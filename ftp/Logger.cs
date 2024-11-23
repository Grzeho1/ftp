using System;
using System.IO;
using System.Threading.Tasks;

public class Logger : IDisposable
{
    private readonly StreamWriter logWriter;
    private static Logger? _instance;
    private static readonly object _lock = new object();

    public static Logger Instance
    {
        get
        {
            lock (_lock)
            {
                return _instance ??= new Logger("Logs");
            }
        }
    }

    private Logger(string logFolderPath)
    {
        if (!Directory.Exists(logFolderPath))
        {
            Directory.CreateDirectory(logFolderPath);
        }

        
        string logFilePath = Path.Combine(logFolderPath, $"Log_{DateTime.Now:yyyy-MM-dd}.txt");

        
        logWriter = new StreamWriter(logFilePath, append: true) { AutoFlush = true };
    }

    public async Task WriteLineAsync(string message)
    {
        Console.WriteLine(message);

        await logWriter.WriteLineAsync($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
    }


    public void Dispose()
    {
        logWriter?.Dispose();
    }
}

