using System;
using System.IO;
using System.Threading.Tasks;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            string configPath = args.Length > 0
                ? args[0]
                : Path.Combine(AppContext.BaseDirectory, "appsettings.json");

            if (!File.Exists(configPath))
            {
                configPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
            }

            if (!File.Exists(configPath))
            {
                throw new FileNotFoundException("appsettings.json not found. Put it next to the exe or pass the path as the first argument.");
            }

            var config = MirrorConfig.Load(configPath);
            LogToFile(config.LogPath, $"Starting mirror: {config.SourceFolder} -> {config.TargetFolder}");

            var engine = new MirrorEngine(config);
            await engine.RunAsync();

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return 1;
        }
    }

    private static void LogToFile(string logPath, string message)
    {
        try
        {
            string? dir = Path.GetDirectoryName(logPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
        }
        catch
        {
            // ignore logging failure
        }
    }
}
