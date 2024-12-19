using Jack.Acme;
using Microsoft.Extensions.DependencyInjection;

namespace UnitTest
{
    public class UnitTest1
    {
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
