using Certes;
using Certes.Acme;
using DnsClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jack.Acme
{
    internal class DomainCertificateManagement
    {
        private readonly string _domain;
        private readonly IAcmeDomainRecoredWriter _acmeDomainRecoredWriter;


        Uri _serverUri = WellKnownServers.LetsEncryptV2;


        public IKey PrivateKey => _privateKey;

        IAccountContext _accountContext;
        AcmeContext _acme;
        IKey _privateKey;
        public DomainCertificateManagement(string domain, IAcmeDomainRecoredWriter acmeDomainRecoredWriter)
        {
            while (domain.StartsWith("*."))
                domain = domain.Substring(2);

            this._domain = domain;
            this._acmeDomainRecoredWriter = acmeDomainRecoredWriter;
            var filepath = $"$Jack.Acme.{_domain}.privateKey.pem";
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

            string filepath = "$Jack.Acme.acccount.pem";
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

            if (File.Exists($"$Jack.Acme.{_domain}.order.txt"))
            {
                try
                {
                    var oldorder = _acme.Order(new Uri(File.ReadAllText($"$Jack.Acme.{_domain}.order.txt", Encoding.UTF8)));
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

            IChallengeContext dnsChallenge = null;
            var order = await _acme.NewOrder(new[] { $"*.{_domain}" });
            string dnsTxt = null;
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    var authz = (await order.Authorizations()).First();
                    dnsChallenge = await authz.Dns();
                    dnsTxt = _acme.AccountKey.DnsTxt(dnsChallenge.Token);
                    await _acmeDomainRecoredWriter.WriteAsync(_domain, dnsTxt);
                    log($"写入域名记录：{dnsTxt}");
                    break;
                }
                catch (Certes.AcmeRequestException ex)
                {
                    log($"order.Authorizations期间发生错误，{ex.ToString()}");
                    if (i == 9)
                        throw new TimeoutException("order.Authorizations超时");

                    await Task.Delay(3000);
                }
            }

            // 实例化 LookupClient 对象
            var lookup = new LookupClient();

            // 指定要查询的域名
            var domainStr = $"_acme-challenge.{_domain}"; // 替换为实际域名

            for (int i = 0; i < 10; i ++)
            {
                await Task.Delay(10000);

                log($"读取{domainStr}记录值");
                // 查询域名的TXT记录
                var result = await lookup.QueryAsync(domainStr, QueryType.TXT);

                // 遍历查询结果
                foreach (var txtRecord in result.Answers.TxtRecords())
                {

                    log($"TXT Record: {string.Join("", txtRecord.Text)}");
                    if( txtRecord.Text.Any(m=>string.Equals(m , dnsTxt , StringComparison.OrdinalIgnoreCase)) )
                    {
                        log($"记录值已经成功生效");
                        i = int.MaxValue;
                        break;
                    }
                }
            }

            log($"再等待1分钟，让域名记录生效");
            await Task.Delay(60000);

            var ret = await dnsChallenge.Validate();
            if (ret.Status != Certes.Acme.Resource.ChallengeStatus.Valid)
                throw new Exception($"域名验证失败，Status={ret.Status} Err={ret.Error}");


            var cert = await order.Generate(new CsrInfo
            {
                CountryName = csrInformation.CountryName,
                State = csrInformation.State,
                Locality = csrInformation.Locality,
                Organization = csrInformation.Organization,
                OrganizationUnit = csrInformation.OrganizationUnit,
                CommonName = $"*.{_domain}",
            }, _privateKey, null, 1000);

            File.WriteAllText($"$Jack.Acme.{_domain}.order.txt", order.Location.ToString(), Encoding.UTF8);
            return cert;
        }
    }
}
