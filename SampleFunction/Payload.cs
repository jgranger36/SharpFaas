using Core.Interfaces;

namespace SampleFunction;

public class Payload
{
    public string connectionString { get; set; }
    public string insertQuery { get; set; }
    public Dictionary<string,string> settings { get; set; }
}