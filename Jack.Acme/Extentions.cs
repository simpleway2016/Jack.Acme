using Jack.Acme;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class Extentions
    {
        /// <summary>
        /// 添加acme证书生成器
        /// </summary>
        /// <returns></returns>
        public static IServiceCollection AddCertificateGenerator(this IServiceCollection services)
        {
            services.AddSingleton<ICertificateGenerator,DefaultCertificateGenerator>();
            return services;
        }
    }
}
