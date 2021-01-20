using BlitzCommon.Resources;
using Serilog;
using Serilog.Core;
using System;
using System.IO;

namespace SnifferResourceEncryptor
{
    class Program
    {
        private static readonly string LOG_FORMAT = "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}";

        static void Main(string targetFile)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(outputTemplate: LOG_FORMAT)
                .CreateLogger();

            ILogger logContext = Log.ForContext(Constants.SourceContextPropertyName, "Program");

            logContext.Information("SnifferResourceEncryptor {Version}", ThisAssembly.AssemblyFileVersion);

            byte[] data = File.ReadAllBytes(targetFile);

            logContext.Information("Read file");

            SnifferResource resource = new SnifferResource(data);
            byte[] file = resource.Serialize();

            logContext.Information("Encryption successful");

            logContext.Information("Key: {Key}", BitConverter.ToString(resource.Key).Replace("-", "").ToLower());
            logContext.Information("Key (base64): {Key}", Convert.ToBase64String(resource.Key));

            File.WriteAllBytes(targetFile + "-enc", file);

            logContext.Information("Done!");
        }
    }
}
