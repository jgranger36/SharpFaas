namespace Core.Interfaces;

public interface IPayload
{
    string Url { get; set; }
    Dictionary<string,string> Headers { get; set; }
    public Guid InstanceId { get; set; }
    public string CallerId { get; set; }
    Dictionary<string,string> Parameters { get; set; }
    string Body { get; set; }
}