using Certes;
using Certes.Acme;
using DnsClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Jack.Acme
{
    internal class DomainCertificateManagement
    {
        private readonly string _domain;
        private readonly IAcmeDomainRecoredWriter _acmeDomainRecoredWriter;
        private readonly GenerateOption _generateOption;
        Uri _serverUri = WellKnownServers.LetsEncryptV2;


        public IKey PrivateKey => _privateKey;

        IAccountContext _accountContext;
        AcmeContext _acme;
        IKey _privateKey;
        public DomainCertificateManagement(string domain, IAcmeDomainRecoredWriter acmeDomainRecoredWriter, GenerateOption generateOption)
        {
            while (domain.StartsWith("*."))
                domain = domain.Substring(2);

            this._domain = domain;
            this._acmeDomainRecoredWriter = acmeDomainRecoredWriter;
            _generateOption = generateOption;

            var filepath = $"{_generateOption.GetSaveFolderPath()}$Jack.Acme.{_domain}.privateKey.pem";
            if (File.Exists(filepath))
            {
                _privateKey = KeyFactory.FromPem(File.ReadAllText(filepath, Encoding.UTF8));
            }
            else
            {
                _privateKey = KeyFactory.NewKey(KeyAlgorithm.RS256);
                File.WriteAllText(filepath, _privateKey.ToPem(), Encoding.UTF8);
            }
        }

        async ValueTask init()
        {
            if (_accountContext != null)
                return;

            string filepath = $"{_generateOption.GetSaveFolderPath()}$Jack.Acme.acccount.pem";
            string pemKey;
            if (File.Exists(filepath))
            {
                pemKey = File.ReadAllText(filepath, Encoding.UTF8);

                var accountKey = KeyFactory.FromPem(pemKey);
                _acme = new AcmeContext(_serverUri, accountKey);
                _accountContext = await _acme.Account();
            }
            else
            {
                _acme = new AcmeContext(_serverUri);
                var account = await _acme.NewAccount($"{DateTime.Now.Ticks}@qq.com", true);

                // Save the account key for later use
                pemKey = _acme.AccountKey.ToPem();
                File.WriteAllText(filepath, pemKey, Encoding.UTF8);
                _accountContext = account;
            }


        }

        /// <summary>
        /// 获取最后一次生成的证书
        /// </summary>
        /// <returns></returns>
        public async Task<CertificateChain?> GetLastCertificateChain()
        {
            await init();

            if (File.Exists($"{_generateOption.GetSaveFolderPath()}$Jack.Acme.{_domain}.order.txt"))
            {
                try
                {
                    var oldorder = _acme.Order(new Uri(File.ReadAllText($"{_generateOption.GetSaveFolderPath()}$Jack.Acme.{_domain}.order.txt", Encoding.UTF8)));
                    if (oldorder != null)
                    {
                        return await oldorder.Download();
                    }
                }
                catch (AcmeRequestException ex)
                {
                    if (ex.Error != null)
                    {
                        if (ex.Error.Status != System.Net.HttpStatusCode.NotFound)
                        {
                            throw ex;
                        }
                    }
                    else
                        throw ex;
                }
            }
            return null;
        }

        void log(string content)
        {
            try
            {
                Console.WriteLine(content);
            }
            catch
            {

            }
        }

        /// <summary>
        /// 生成新的证书
        /// </summary>
        /// <returns></returns>
        public async Task<CertificateChain?> GenerateCertificateChain(CsrInformation csrInformation)
        {
            await init();
 

            var order = await _acme.NewOrder(new[] { _domain, $"*.{_domain}" });

            using var httpclient = new HttpClient();

            // 指定要查询的域名
            var domainStr = $"_acme-challenge.{_domain}"; // 替换为实际域名

            var authList = await order.Authorizations();
            foreach (var authz in authList)
            {
             
                var contextStr = await httpclient.GetStringAsync(authz.Location.AbsoluteUri);
                var authContext = System.Text.Json.JsonSerializer.Deserialize<Dtos.AuthorizationContextDto>(contextStr);
                if(authContext?.challenges?.FirstOrDefault()?.status == "valid")
                {
                    continue;
                }
                var dnsChallenge = await authz.Dns();
                var dnsTxt = _acme.AccountKey.DnsTxt(dnsChallenge.Token);
                await _acmeDomainRecoredWriter.WriteAsync(_domain, dnsTxt);
                log($"写入域名记录：{dnsTxt}");

                for (int j = 0; j < 10; j++)
                {
                    await Task.Delay(10000);

                    log($"读取{domainStr}记录值");

                    // 实例化 LookupClient 对象
                    var lookup = new LookupClient();

                    // 查询域名的TXT记录
                    var result = await lookup.QueryAsync(domainStr, QueryType.TXT);

                    // 遍历查询结果
                    foreach (var txtRecord in result.Answers.TxtRecords())
                    {

                        log($"当前值: {string.Join("", txtRecord.Text)}");
                        if (txtRecord.Text.Any(m => string.Equals(m, dnsTxt, StringComparison.OrdinalIgnoreCase)))
                        {
                            log($"记录值已经成功生效");
                            j = 1000;
                            break;
                        }
                    }
                }


                for (int j = 0; j < 10; j++)
                {
                    var ret = await dnsChallenge.Validate();
                    if (ret.Status != Certes.Acme.Resource.ChallengeStatus.Invalid && ret.Status != Certes.Acme.Resource.ChallengeStatus.Valid)
                    {
                        log($"域名验证当前状态：{ret.Status}");
                        await Task.Delay(3000);
                        continue;
                    }
                    if (ret.Status != Certes.Acme.Resource.ChallengeStatus.Valid)
                        throw new Exception($"域名验证失败，Status={ret.Status} Err={ret.Error}");

                    break;
                }
            }

            log($"域名验证通过");

            var cert = await order.Generate(new CsrInfo
            {
                CountryName = csrInformation.CountryName,
                State = csrInformation.State,
                Locality = csrInformation.Locality,
                Organization = csrInformation.Organization,
                OrganizationUnit = csrInformation.OrganizationUnit,
                CommonName = $"*.{_domain}",
            }, _privateKey, null, 1000);

            File.WriteAllText($"{_generateOption.GetSaveFolderPath()}$Jack.Acme.{_domain}.order.txt", order.Location.ToString(), Encoding.UTF8);
            return cert;
        }
    }
}
