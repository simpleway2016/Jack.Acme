using System;
using System.Threading.Tasks;

namespace Jack.Acme
{
    /// <summary>
    /// 写入Acme验证时_acme-challenge主机记录的接口
    /// </summary>
    public interface IAcmeDomainRecoredWriter
    {
        /// <summary>
        /// 写入主机记录
        /// </summary>
        /// <param name="domainName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        Task WriteAsync(string domainName,string value);
    }
}
