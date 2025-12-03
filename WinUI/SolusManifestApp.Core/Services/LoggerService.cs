using SolusManifestApp.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SolusManifestApp.Core.Services;

public class LoggerService : ILoggerService
{
    private static readonly object _lock = new object();
    private readonly string _logFilePath;
    private const long MAX_LOG_SIZE = 8 * 1024 * 1024; // 8MB
    private const long TRIM_TO_SIZE = 6 * 1024 * 1024; // Trim to 6MB when rotating

    public LoggerService(string logName = "SolusManifestApp")
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SolusManifestApp"
        );
        Directory.CreateDirectory(appDataPath);

        _logFilePath = Path.Combine(appDataPath, $"{logName}.log");

        Log("INFO", "Logger initialized");
        Log("INFO", $"Log file: {_logFilePath}");
    }

    public void Log(string level, string message)
    {
        lock (_lock)
        {
            try
            {
                if (File.Exists(_logFilePath))
                {
                    var fileInfo = new FileInfo(_logFilePath);
                    if (fileInfo.Length >= MAX_LOG_SIZE)
                    {
                        TrimLogFile();
                    }
                }

                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var logEntry = $"[{timestamp}] [{level}] {message}";

                File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
                System.Diagnostics.Debug.WriteLine(logEntry);
            }
            catch
            {
                // Silently fail if logging fails
            }
        }
    }

    private void TrimLogFile()
    {
        try
        {
            var allLines = File.ReadAllLines(_logFilePath);
            var currentSize = new FileInfo(_logFilePath).Length;
            var averageLineSize = currentSize / allLines.Length;
            var linesToKeep = (int)(TRIM_TO_SIZE / averageLineSize);
            var linesToWrite = allLines.Skip(Math.Max(0, allLines.Length - linesToKeep)).ToArray();
            File.WriteAllLines(_logFilePath, linesToWrite);
        }
        catch
        {
            // Silent fail
        }
    }

    public void Info(string message) => Log("INFO", message);
    public void Warning(string message) => Log("WARNING", message);
    public void Error(string message) => Log("ERROR", message);
    public void Debug(string message) => Log("DEBUG", message);

    public List<string> GetRecentLogs(int count = 100)
    {
        lock (_lock)
        {
            try
            {
                if (!File.Exists(_logFilePath))
                    return new List<string>();

                var lines = File.ReadAllLines(_logFilePath);
                return lines.TakeLast(count).ToList();
            }
            catch
            {
                return new List<string>();
            }
        }
    }

    public void ClearLogs()
    {
        lock (_lock)
        {
            try
            {
                if (File.Exists(_logFilePath))
                    File.Delete(_logFilePath);
            }
            catch
            {
                // Silent fail
            }
        }
    }
}
