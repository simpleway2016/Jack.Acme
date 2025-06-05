using Jack.Acme;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class Extentions
    {
        /// <summary>
        /// 使用腾讯云的域名解析记录接口
        /// </summary>
        /// <param name="services"></param>
        /// <param name="accessKeyId"></param>
        /// <param name="accessKeySecret"></param>
        /// <param name="endPoint">Endpoint 请参考 https://api.aliyun.com/product/Alidns</param>
        /// <returns></returns>
        public static IServiceCollection AddTencentCloudRecordWriter(this IServiceCollection services,string accessKeyId,string accessKeySecret,string endPoint = "dnspod.tencentcloudapi.com")
        {
            services.AddSingleton<IAcmeDomainRecoredWriter>(new Jack.Acme.TencentCloud.AcmeDomainRecoredWriter(accessKeyId,accessKeySecret, endPoint));
            return services;
        }
    }
}
