namespace SolusManifestApp.Core.Interfaces;

/// <summary>
/// Logger service interface for application logging
/// </summary>
public interface ILoggerService
{
    void Log(string level, string message);
    void Info(string message);
    void Warning(string message);
    void Error(string message);
    void Debug(string message);
    List<string> GetRecentLogs(int count = 100);
    void ClearLogs();
}
