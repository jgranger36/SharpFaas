using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Core;
using Core.Helpers;
using Core.Interfaces;

namespace functionserver.Services;

public class S3Store : IFunctionStore
{
    private ILogger _logger;
    private AmazonS3Client _s3Client;
    private string _bucketName;

    public S3Store(string bucketName,string secret,string key,string region, ILogger logger)
    {
        _logger = logger;

        _bucketName = bucketName;
        var awsCredentials = new BasicAWSCredentials(key, secret);
        RegionEndpoint regionEndpoint = RegionEndpoint.EnumerableAllRegions.First(r => r.SystemName == region);
        _s3Client = new AmazonS3Client(awsCredentials,regionEndpoint);
    }
    
    private IFunctionStore _functionStoreImplementation;
    public string LocalStorePath => _functionStoreImplementation.LocalStorePath;

    public string RemoteStorePath => _functionStoreImplementation.RemoteStorePath;

    public async Task<FunctionFile> PushFunctionAsync(FunctionFile function,bool newVersion, Stream zipArchive)
    {
        var request = new ListObjectsRequest()
        {
            BucketName = Path.Combine(_bucketName, function.RemoteDirectory).ToFtpPath(),
        };
        
        var response = await _s3Client.ListObjectsAsync(request);

        if (response.S3Objects.Count > 0)
        {
            function.Version = response.S3Objects.Select(l =>
                
                    Path.GetFileNameWithoutExtension(l.Key).ToVersion()
                )
                .OrderByDescending(l => l).First();
        }
        
        if (newVersion || function.Version.Major == 0)
            function.Version = new Version(function.Version.Major + 1, 0);

        function.Version = new Version(function.Version.Major, function.Version.Minor + 1);
        
        var putRequest = new Amazon.S3.Model.PutObjectRequest
        {
            BucketName = _bucketName,
            Key = function.RemoteDirectory.ToFtpPath(),
            InputStream = zipArchive,
        };
        
        var putResponse = await _s3Client.PutObjectAsync(putRequest);
        
        if(!putResponse.HttpStatusCode.ToString().StartsWith("2"))
            throw new Exception($"Pushing of function failed: {putResponse.HttpStatusCode.ToString()}");
        
        return function;
    }

    public async Task<string> GetFunctionAsync(FunctionFile function)
    {
        return await _functionStoreImplementation.GetFunctionAsync(function);
    }
}