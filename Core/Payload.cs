using System.Text.Json.Serialization;
using Core.Interfaces;

namespace Core;

public class Payload : IPayload
{
    public string Url { get; set; }
    public Dictionary<string, string> Headers { get; set; }
    public Dictionary<string, string> Parameters { get; set; }
    public string Body { get; set; }
}