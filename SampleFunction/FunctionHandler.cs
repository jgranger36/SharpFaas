using Core.Interfaces;
using Dapper;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SampleFunction;

public class FunctionHandler : IFunction
{
    public Guid FunctionId { get; } = new Guid("332c15af-5820-44a3-90d6-bf9a8965933e");
    public Guid InstanceId { get; }
    public string Label { get; } = "SampleFunction";
    public string Description { get; } = "sample function used for testing sharpfaas";
    public DateTime StartTime { get; } = DateTime.UtcNow;
    public StringWriter FunctionConsole { get; set; }
    public async Task ExecuteAsync(string jsonPayload, StringWriter writer = null)
    {
        FunctionConsole = writer;

        var jsonObject = JsonConvert.DeserializeObject<JObject>(jsonPayload);
        
        using(var conn = new SqlConnection((string)jsonObject["connectionString"]))
        {
            for (int i = 1; i != 0 ;i++)
            {
                var update = await conn.QuerySingleAsync("select * from dbo.[update] order by rowupdated offset @i rows fetch next 1 rows only ", new {i});

                if (update is null)
                    i = 0;
                
                await conn.ExecuteAsync((string)jsonObject["insertQuery"],new {i,payload = update.File});

                await Task.Delay(5000);
            }
        }

        if ((bool) jsonObject["throwError"])
            throw new Exception("ERROR WAS THROWN.");

    }
}