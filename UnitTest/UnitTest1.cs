using DnsClient;
using Jack.Acme;
using Microsoft.Extensions.DependencyInjection;

namespace UnitTest
{
    public class UnitTest1
    {
        [Fact]
        public async Task Dns()
        {
            // 实例化 LookupClient 对象
            var lookup = new LookupClient();

            // 指定要查询的域名
            var domain = "_acme-challenge.jacktan.cn"; // 替换为实际域名

            // 查询域名的TXT记录
            var result = await lookup.QueryAsync(domain, QueryType.TXT);

            // 遍历查询结果
            foreach (var txtRecord in result.Answers.TxtRecords())
            {
             
                Console.WriteLine($"TXT Record: {string.Join("", txtRecord.Text)}");
            }
        }

        [Fact]
        public async Task Test1()
        {
            var accessKeyId = "";
            var accessKeySecret = "";

            var services = new ServiceCollection();
            services.AddCertificateGenerator();
            services.AddAlibabaCloudRecordWriter(accessKeyId, accessKeySecret);

            var serviceProvider = services.BuildServiceProvider();

            var certificateGenerator = serviceProvider.GetRequiredService<ICertificateGenerator>();

            await certificateGenerator.GeneratePfxAsync("yourdomain" , new CsrInformation
            {
                CountryName = "CA",
                State = "Ontario",
                Locality = "Toronto",
                Organization = "Certes",
                OrganizationUnit = "Dev",
            } , "cert.pfx" , "123456");
        }
    }
}
