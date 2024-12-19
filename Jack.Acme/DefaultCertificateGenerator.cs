using Certes;
using Certes.Acme;
using Certes.Pkcs;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Jack.Acme
{
    public class DefaultCertificateGenerator : ICertificateGenerator
    {
        private readonly IAcmeDomainRecoredWriter _acmeDomainRecoredWriter;
        ConcurrentDictionary<string, DomainCertificateManagement> _domainCertManagements = new ConcurrentDictionary<string, DomainCertificateManagement>();

        public DefaultCertificateGenerator(IAcmeDomainRecoredWriter acmeDomainRecoredWriter)
        {
            this._acmeDomainRecoredWriter = acmeDomainRecoredWriter;
        }

        /// <summary>
        /// 生成crt证书
        /// </summary>
        /// <param name="domain">域名</param>
        /// <param name="crtPath">生成的证书路径</param>
        /// <param name="privateKeyPath">生成的私钥路径</param>
        /// <returns></returns>
        public async Task GenerateCrtAsync(string domain, CsrInformation csrInformation, string crtPath, string privateKeyPath)
        {
            var management = _domainCertManagements.GetOrAdd(domain, s => new DomainCertificateManagement(domain, _acmeDomainRecoredWriter));
            var certificateChain = await management.GetLastCertificateChain();
            if(certificateChain != null)
            {
                var pfxBuilder = certificateChain.ToPfx(management.PrivateKey);
                var pfx = pfxBuilder.Build("cert", "123456");
                var x509 = new X509Certificate2(pfx, "123456");
                if( x509.NotAfter.ToUniversalTime() > DateTime.UtcNow.AddMonths(1))
                {
                    generateCrt(management, certificateChain, crtPath, privateKeyPath);
                    return;
                }
            }

            certificateChain = await management.GenerateCertificateChain(csrInformation);
            generateCrt(management, certificateChain, crtPath, privateKeyPath);
        }

        void generateCrt(DomainCertificateManagement management,CertificateChain certificateChain, string crtPath, string privateKeyPath)
        {
            File.WriteAllText(crtPath, certificateChain.ToPem(), Encoding.UTF8);
            File.WriteAllText(privateKeyPath, management.PrivateKey.ToPem(), Encoding.UTF8);
        }

        /// <summary>
        /// 生成pfx证书
        /// </summary>
        /// <param name="domain">域名</param>
        /// <param name="pfxPath">生成的证书路径</param>
        /// <param name="password">设置证书密码</param>
        /// <returns></returns>
        public async Task GeneratePfxAsync(string domain, CsrInformation csrInformation, string pfxPath, string password)
        {
            var management = _domainCertManagements.GetOrAdd(domain, s => new DomainCertificateManagement(domain, _acmeDomainRecoredWriter));
            byte[] pfxData;
            PfxBuilder pfxBuilder;

            var certificateChain = await management.GetLastCertificateChain();
            if (certificateChain != null)
            {
                pfxBuilder = certificateChain.ToPfx(management.PrivateKey);
                pfxData = pfxBuilder.Build("cert", password);
                var x509 = new X509Certificate2(pfxData, password);
                if (x509.NotAfter.ToUniversalTime() > DateTime.UtcNow.AddMonths(1))
                {
                    File.WriteAllBytes(pfxPath, pfxData);
                    return;
                }
            }

            certificateChain = await management.GenerateCertificateChain(csrInformation);
            pfxBuilder = certificateChain.ToPfx(management.PrivateKey);
            pfxData = pfxBuilder.Build("cert", password);
            File.WriteAllBytes(pfxPath, pfxData);
        }
    }
}
