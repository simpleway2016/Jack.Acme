Jack.Acme用于自动生成泛域名证书

```cs
            var accessKeyId = "阿里云accessKeyId";
            var accessKeySecret = "阿里云accessKeySecret";

            var services = new ServiceCollection();
            services.AddCertificateGenerator();
            services.AddAlibabaCloudRecordWriter(accessKeyId, accessKeySecret);

            var serviceProvider = services.BuildServiceProvider();

            var certificateGenerator = serviceProvider.GetRequiredService<ICertificateGenerator>();

            await certificateGenerator.GeneratePfxAsync("jacktan.cn" , new CsrInformation
            {
                CountryName = "CA",
                State = "Ontario",
                Locality = "Toronto",
                Organization = "Certes",
                OrganizationUnit = "Dev",
            } , "cert.pfx" , "123456");
```