using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Jack.Acme
{
    public interface ICertificateGenerator
    {
        /// <summary>
        /// 生成crt证书
        /// </summary>
        /// <param name="domain">域名</param>
        /// <param name="crtPath">生成的证书路径</param>
        /// <param name="privateKeyPath">生成的私钥路径</param>
        /// <returns></returns>
        Task GenerateCrtAsync(string domain, CsrInformation csrInformation, string crtPath, string privateKeyPath);
        /// <summary>
        /// 生成pfx证书
        /// </summary>
        /// <param name="domain">域名</param>
        /// <param name="pfxPath">生成的证书路径</param>
        /// <param name="password">设置证书密码</param>
        /// <returns></returns>
        Task GeneratePfxAsync(string domain, CsrInformation csrInformation, string pfxPath, string password);
    }
}
