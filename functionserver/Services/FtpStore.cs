using System.IO.Compression;
using FluentFTP;
using Core.Helpers;
using Core.Interfaces;
using Core;

namespace functionserver.Services;

public class FtpStore : IFunctionStore
{
    public FtpClient Client { get; }
    private ILogger _logger;

    public FtpStore(Configuration configuration,string localStore, ILogger logger)
    {
        _logger = logger;
        RemoteStorePath = configuration.FtpFolder;
        LocalStorePath = localStore;
        Client = new FtpClient(configuration.FtpServer, configuration.FtpUser, configuration.FtpPassword);
        Client.ReadTimeout = 60000;
        Client.ConnectTimeout = 60000;
        Client.AutoConnect();
    }

    public async Task<Function> PushFunctionAsync(Function function,bool newVersion, Stream zipArchive)
    {

        var listing = Client.GetListing(Path.Combine(RemoteStorePath,function.RemoteDirectory).ToFtpPath());

        if (listing.Length > 0)
        {
            function.Version = listing.Select(l => Path.GetFileNameWithoutExtension(l.Name).ToVersion())
                .OrderByDescending(l => l).First();
        }
        
        if (newVersion || function.Version.Major == 0)
            function.Version = new Version(function.Version.Major + 1, 0);

        function.Version = new Version(function.Version.Major, function.Version.Minor + 1);
        
        var status = await Client.UploadStreamAsync(zipArchive, Path.Combine(RemoteStorePath,function.Zip).ToFtpPath());
        
        if(status != FtpStatus.Success)
            throw new Exception($"Pushing of function failed: {status.ToString()}");
        
        return function;
    }

    public async Task<string> GetFunctionAsync(Function function)
    {
        if (!File.Exists(Path.Combine(LocalStorePath,function.File)))
        {
            if (function.Version.Major == 0 || function.Version.Minor == 0)
            {
                var listing = Client.GetListing(Path.Combine(RemoteStorePath,function.RemoteDirectory).ToFtpPath());

                if (function.Version.Major > 0)
                    listing = listing.Where(l => l.Name.StartsWith(function.Version.Major.ToString())).ToArray();

                if (listing.Length > 0)
                {
                    function.Version = listing.Select(l => Path.GetFileNameWithoutExtension(l.Name).ToVersion())
                        .OrderByDescending(l => l).First();
                }
            }
        
            if(!File.Exists(Path.Combine(LocalStorePath,function.File)))
                using (var stream = new MemoryStream())
                {
                    if (await Client.DownloadStreamAsync(stream,
                            Path.Combine(RemoteStorePath, function.Zip).ToFtpPath()))
                    {
                        using (var zip = new ZipArchive(stream))
                        {
                            zip.ExtractToDirectory(Path.Combine(LocalStorePath,function.LocalDirectory));
                        }
                    }
                }
        }

        return Path.Combine(LocalStorePath,function.File);
    }

    public string LocalStorePath { get; }
    public string RemoteStorePath { get; }
}