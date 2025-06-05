using Jack.Acme;
using System;
using System.Threading.Tasks;
using Tea;

namespace Jack.Acme.AlibabaCloudApi
{
   public  class AcmeDomainRecoredWriter : IAcmeDomainRecoredWriter
    {
        private readonly string _accessKeyId;
        private readonly string _accessKeySecret;
        private readonly string _endPoint;

        public AcmeDomainRecoredWriter(string accessKeyId, string accessKeySecret,string endPoint)
        {
            this._accessKeyId = accessKeyId;
            this._accessKeySecret = accessKeySecret;
            this._endPoint = endPoint;
        }

        AlibabaCloud.SDK.Alidns20150109.Client CreateClient()
        {
            AlibabaCloud.OpenApiClient.Models.Config config = new AlibabaCloud.OpenApiClient.Models.Config
            {
                AccessKeyId = _accessKeyId,
                AccessKeySecret = _accessKeySecret,
            };

            config.Endpoint = _endPoint;
            return new AlibabaCloud.SDK.Alidns20150109.Client(config);
        }

        /// <summary>
        /// 获取主域名的所有解析记录列表
        /// </summary>
        /// <param name="client"></param>
        /// <param name="domainName"></param>
        /// <param name="RR"></param>
        /// <param name="recordType"></param>
        /// <returns></returns>
        static async Task<AlibabaCloud.SDK.Alidns20150109.Models.DescribeDomainRecordsResponse> DescribeDomainRecordsAsync(AlibabaCloud.SDK.Alidns20150109.Client client, string domainName, string RR, string recordType)
        {
            AlibabaCloud.SDK.Alidns20150109.Models.DescribeDomainRecordsRequest req = new AlibabaCloud.SDK.Alidns20150109.Models.DescribeDomainRecordsRequest();
            // 主域名
            req.DomainName = domainName;
            // 主机记录
            req.RRKeyWord = RR;
            // 解析记录类型
            req.Type = recordType;
            AlibabaCloud.SDK.Alidns20150109.Models.DescribeDomainRecordsResponse resp = await client.DescribeDomainRecordsAsync(req);

            return resp;
        }

 
        public async Task WriteAsync(string domainName, string value)
        {
            AlibabaCloud.SDK.Alidns20150109.Client client = CreateClient();
            var domainRecords = await DescribeDomainRecordsAsync(client, domainName, "_acme-challenge", "TXT");

            if (domainRecords.Body.DomainRecords.Record.Count > 0)
            {
                var request = new AlibabaCloud.SDK.Alidns20150109.Models.UpdateDomainRecordRequest
                {
                    RR = "_acme-challenge",
                    Type = "TXT",
                    Value = value,
                    RecordId = domainRecords.Body.DomainRecords.Record[0].RecordId,
                };
                try
                {
                    var ret = await client.UpdateDomainRecordAsync(request);
                }
                catch (TeaException ex)
                {
                    if (ex.Code == "DomainRecordDuplicate")
                    {
                        return;
                    }
                    else
                        throw ex;
                }
            }
            else
            {
                var request = new AlibabaCloud.SDK.Alidns20150109.Models.AddDomainRecordRequest
                {
                    Lang = "zh",
                    DomainName = domainName,
                    RR = "_acme-challenge",
                    Type = "TXT",
                    Value = value
                };

                var response = await client.AddDomainRecordAsync(request);

            }
        }
    }
}
