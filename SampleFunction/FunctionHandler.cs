using Core.Interfaces;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using S3;

namespace SampleFunction;

public class FunctionHandler : IFunction
{
    public Guid FunctionId { get; } = new Guid("332c15af-5820-44a3-90d6-bf9a8965933e");
    public string Label { get; } = "SampleFunction";
    public string Description { get; } = "sample function used for testing sharpfaas";

    public async Task ExecuteAsync(IPayload payload)
    {
        var jsonObject = JsonConvert.DeserializeObject<JObject>(payload.Body);

        var clientId = payload.Parameters["callerId"];

        await using (var bucket = new ThirdPartySales(NullLogger.Instance,clientId,this.Label ))
        {
            bucket.PutOrder(new ThirdPartySales.TPSPayload
            {
                Key = (string)jsonObject["uniqueId"],
                Payload = payload.Body,
            });
        }

    }
}