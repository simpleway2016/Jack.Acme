using Jack.Acme;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using TencentCloud.Common;
using TencentCloud.Common.Profile;
using TencentCloud.Dnspod.V20210323;
using TencentCloud.Dnspod.V20210323.Models;

namespace Jack.Acme.TencentCloud
{
    public class AcmeDomainRecoredWriter : IAcmeDomainRecoredWriter
    {
        private readonly string _secretId;
        private readonly string _secretKey;
        private readonly string _endPoint;

        public AcmeDomainRecoredWriter(string secretId, string secretKey, string endPoint)
        {
            _secretId = secretId;
            _secretKey = secretKey;
            _endPoint = endPoint;
        }

        DnspodClient CreateClient()
        {
            Credential cred = new Credential
            {
                SecretId = _secretId,
                SecretKey = _secretKey
            };
            ClientProfile clientProfile = new ClientProfile();
            HttpProfile httpProfile = new HttpProfile();
            httpProfile.Endpoint = _endPoint;
            clientProfile.HttpProfile = httpProfile;
            return new DnspodClient(cred, "", clientProfile);
        }

        public async Task WriteAsync(string mainDomain, string value)
        {
            var client = CreateClient();

            string subDomain = "_acme-challenge";

            // 查询现有记录
            var listReq = new DescribeRecordListRequest
            {
                Domain = mainDomain,
                Subdomain = subDomain,
                RecordType = "TXT"
            };
            var listResp = await client.DescribeRecordList(listReq);

            var record = listResp.RecordList?.FirstOrDefault(r => r.Type == "TXT" && r.Name == subDomain);

            if (record != null)
            {
                // 更新记录
                var updateReq = new ModifyRecordRequest
                {
                    Domain = mainDomain,
                    RecordId = record.RecordId,
                    SubDomain = subDomain,
                    RecordType = "TXT",
                    RecordLine = "默认",
                    Value = value
                };
                await client.ModifyRecord(updateReq);
            }
            else
            {
                // 新增记录
                var addReq = new CreateRecordRequest
                {
                    Domain = mainDomain,
                    SubDomain = subDomain,
                    RecordType = "TXT",
                    RecordLine = "默认",
                    Value = value
                };
                await client.CreateRecord(addReq);
            }
        }
    }
}