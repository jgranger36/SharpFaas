namespace functionserver;

public class Configuration
{
    public string EncryptionKey { get; set; }
    public string FunctionDirectory { get; set; }
    public string FtpServer { get; set; }
    public string FtpPort { get; set; }
    public string FtpUser { get; set; }
    public string FtpPassword { get; set; }
    public string FtpFolder { get; set; }
    public string BucketName { get; set; }
    public string BucketAccessKey { get; set; }
    public string BucketAccessSecret { get; set; }
    public string BucketRegion { get; set; }
}