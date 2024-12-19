using Certes;
using Certes.Acme;
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

        /// <summary>
        /// 生成新的证书
        /// </summary>
        /// <returns></returns>
        public async Task<CertificateChain?> GenerateCertificateChain(CsrInformation csrInformation)
        {
            await init();

            var order = await _acme.NewOrder(new[] { $"*.{_domain}" });


            var authz = (await order.Authorizations()).First();
            var dnsChallenge = await authz.Dns();
            var dnsTxt = _acme.AccountKey.DnsTxt(dnsChallenge.Token);
            await _acmeDomainRecoredWriter.WriteAsync(_domain, dnsTxt);

            for (int i = 0; i <= 500; i++)
            {
                if (i == 500)
                    throw new TimeoutException("acme验证域名记录超时");

                var ret = await dnsChallenge.Validate();
                if (ret.Status == Certes.Acme.Resource.ChallengeStatus.Valid)
                    break;
                else
                    await Task.Delay(3000);
            }

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
