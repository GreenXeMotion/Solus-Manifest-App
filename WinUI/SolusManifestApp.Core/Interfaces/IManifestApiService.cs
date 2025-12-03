using SolusManifestApp.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SolusManifestApp.Core.Interfaces;

public interface IManifestApiService
{
    Task<Manifest?> GetManifestAsync(string appId, string apiKey);
    Task<List<Manifest>?> SearchGamesAsync(string query, string apiKey);
    Task<List<Manifest>?> GetAllGamesAsync(string apiKey);
    bool ValidateApiKey(string apiKey);
    Task<bool> TestApiKeyAsync(string apiKey);
    Task<GameStatus?> GetGameStatusAsync(string appId, string apiKey);
}
