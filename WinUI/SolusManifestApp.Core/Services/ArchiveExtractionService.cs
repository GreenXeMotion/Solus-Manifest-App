using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace SolusManifestApp.Core.Services;

public class ArchiveExtractionService
{
    public static bool IsValidLuaFilename(string filename)
    {
        if (!filename.ToLower().EndsWith(".lua"))
            return false;

        var namePart = filename.Substring(0, filename.Length - 4);
        return namePart.All(char.IsDigit);
    }

    public (List<string> luaFiles, string? tempDir) ExtractLuaFromArchive(string archivePath)
    {
        var luaFiles = new List<string>();
        var tempDir = Path.Combine(Path.GetTempPath(), $"solus_extract_{Guid.NewGuid()}");

        try
        {
            Directory.CreateDirectory(tempDir);

            var archiveLower = archivePath.ToLower();

            if (archiveLower.EndsWith(".zip"))
            {
                ZipFile.ExtractToDirectory(archivePath, tempDir);
            }
            else
            {
                throw new NotSupportedException($"Unsupported archive format: {Path.GetExtension(archivePath)}. Only ZIP is supported.");
            }

            // Find all .lua files recursively
            foreach (var file in Directory.GetFiles(tempDir, "*.lua", SearchOption.AllDirectories))
            {
                var filename = Path.GetFileName(file);
                if (IsValidLuaFilename(filename))
                {
                    luaFiles.Add(file);
                }
            }

            return (luaFiles, tempDir);
        }
        catch (Exception ex)
        {
            if (Directory.Exists(tempDir))
            {
                try
                {
                    Directory.Delete(tempDir, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }

            throw new Exception($"Error extracting archive: {ex.Message}", ex);
        }
    }

    public (List<string> manifestFiles, string? tempDir) ExtractManifestsFromArchive(string archivePath)
    {
        var manifestFiles = new List<string>();
        var tempDir = Path.Combine(Path.GetTempPath(), $"solus_extract_{Guid.NewGuid()}");

        try
        {
            Directory.CreateDirectory(tempDir);

            if (archivePath.ToLower().EndsWith(".zip"))
            {
                ZipFile.ExtractToDirectory(archivePath, tempDir);
            }
            else
            {
                throw new NotSupportedException($"Unsupported archive format. Only ZIP is supported.");
            }

            // Find all .manifest files
            manifestFiles = Directory.GetFiles(tempDir, "*.manifest", SearchOption.AllDirectories).ToList();

            return (manifestFiles, tempDir);
        }
        catch (Exception ex)
        {
            CleanupTempDirectory(tempDir);
            throw new Exception($"Error extracting manifests: {ex.Message}", ex);
        }
    }

    public void CleanupTempDirectory(string? tempDir)
    {
        if (!string.IsNullOrEmpty(tempDir) && Directory.Exists(tempDir))
        {
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
