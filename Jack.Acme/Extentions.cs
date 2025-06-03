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
        public static IServiceCollection AddCertificateGenerator(this IServiceCollection services, GenerateOption generateOption = null)
        {
            if (generateOption != null)
                services.AddSingleton(generateOption);
            else
            {
                services.AddSingleton(new GenerateOption());
            }
                services.AddSingleton<ICertificateGenerator, DefaultCertificateGenerator>();
            return services;
        }
    }
}
