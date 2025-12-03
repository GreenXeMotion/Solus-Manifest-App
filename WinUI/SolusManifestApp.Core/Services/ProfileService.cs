using SolusManifestApp.Core.Interfaces;
using SolusManifestApp.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace SolusManifestApp.Core.Services;

public class ProfileService
{
    private readonly string _profilesPath;
    private readonly ISettingsService _settingsService;
    private readonly ILoggerService _logger;
    private ProfileData? _profileData;

    public ProfileService(ISettingsService settingsService, ILoggerService logger)
    {
        _settingsService = settingsService;
        _logger = logger;

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appData, "SolusManifestApp");
        Directory.CreateDirectory(appFolder);
        _profilesPath = Path.Combine(appFolder, "greenluma_profiles.json");
    }

    public ProfileData LoadProfiles()
    {
        if (_profileData != null)
            return _profileData;

        try
        {
            if (File.Exists(_profilesPath))
            {
                var json = File.ReadAllText(_profilesPath);
                _profileData = JsonSerializer.Deserialize<ProfileData>(json) ?? new ProfileData();
            }
            else
            {
                _profileData = new ProfileData();
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to load profiles: {ex.Message}");
            _profileData = new ProfileData();
        }

        if (_profileData.Profiles.Count == 0)
        {
            var defaultProfile = new GreenLumaProfile { Name = "Default" };
            _profileData.Profiles.Add(defaultProfile);
            _profileData.ActiveProfileId = defaultProfile.Id;
            SaveProfiles();
        }

        if (string.IsNullOrEmpty(_profileData.ActiveProfileId) && _profileData.Profiles.Count > 0)
        {
            _profileData.ActiveProfileId = _profileData.Profiles[0].Id;
            SaveProfiles();
        }

        return _profileData;
    }

    public void SaveProfiles()
    {
        try
        {
            if (_profileData == null)
                return;

            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(_profileData, options);

            using var fileStream = new FileStream(_profilesPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough);
            using var writer = new StreamWriter(fileStream);
            writer.Write(json);
            writer.Flush();
            fileStream.Flush(flushToDisk: true);
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to save profiles: {ex.Message}");
        }
    }

    public GreenLumaProfile CreateProfile(string name)
    {
        var data = LoadProfiles();
        var profile = new GreenLumaProfile { Name = name };
        data.Profiles.Add(profile);
        SaveProfiles();
        _logger.Info($"Created profile: {name}");
        return profile;
    }

    public void DeleteProfile(string profileId)
    {
        var data = LoadProfiles();
        var profile = data.Profiles.FirstOrDefault(p => p.Id == profileId);
        if (profile != null)
        {
            data.Profiles.Remove(profile);
            if (data.ActiveProfileId == profileId && data.Profiles.Count > 0)
            {
                data.ActiveProfileId = data.Profiles[0].Id;
            }
            SaveProfiles();
            _logger.Info($"Deleted profile: {profile.Name}");
        }
    }

    public GreenLumaProfile? GetActiveProfile()
    {
        var data = LoadProfiles();
        return data.Profiles.FirstOrDefault(p => p.Id == data.ActiveProfileId);
    }

    public void SetActiveProfile(string profileId)
    {
        var data = LoadProfiles();
        if (data.Profiles.Any(p => p.Id == profileId))
        {
            data.ActiveProfileId = profileId;
            SaveProfiles();
            _logger.Info($"Switched to profile: {profileId}");
        }
    }

    public List<GreenLumaProfile> GetAllProfiles()
    {
        var data = LoadProfiles();
        return data.Profiles;
    }
}
