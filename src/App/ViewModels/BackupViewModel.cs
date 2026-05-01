using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Sakura.Core.Backup;
using System.Collections.ObjectModel;
using System.IO;

namespace Sakura.App.ViewModels;

public sealed partial class BackupViewModel : ObservableObject
{
    private readonly ILogger<BackupViewModel> _logger;
    private readonly string _backupRoot;

    public ObservableCollection<BackupEntry> Backups { get; } = [];

    [ObservableProperty] private BackupEntry? _selectedBackup;
    [ObservableProperty] private bool         _isBusy        = false;
    [ObservableProperty] private string       _statusMessage = "";
    [ObservableProperty] private bool         _statusSuccess = false;
    [ObservableProperty] private bool         _hasLatestBackup = false;

    public BackupViewModel(ILogger<BackupViewModel> logger, string? backupRootOverride = null)
    {
        _logger     = logger;
        _backupRoot = backupRootOverride
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                            "Sakura", "backup");
        Refresh();
    }

    // ── Commands ───────────────────────────────────────────────────────────

    [RelayCommand]
    public void Refresh()
    {
        Backups.Clear();
        SelectedBackup = null;

        if (!Directory.Exists(_backupRoot))
        {
            StatusMessage = "No backups found";
            return;
        }

        foreach (string dir in Directory.GetDirectories(_backupRoot)
                     .OrderByDescending(d => Directory.GetCreationTimeUtc(d)))
        {
            string manifestPath = Path.Combine(dir, "manifest.json");
            if (!File.Exists(manifestPath)) continue;

            try
            {
                var manifest = BackupManifest.Load(dir);
                Backups.Add(new BackupEntry(manifest, dir));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load manifest from {Dir}", dir);
            }
        }

        StatusMessage    = Backups.Count == 0 ? "No backups found" : $"{Backups.Count} backup(s) found";
        HasLatestBackup  = Backups.Count > 0;

        if (Backups.Count > 0)
            SelectedBackup = Backups[0];
    }

    [RelayCommand]
    public async Task RestoreSelectedAsync()
    {
        if (IsBusy || SelectedBackup is null) return;

        IsBusy        = true;
        StatusSuccess = false;
        StatusMessage = $"Restoring '{SelectedBackup.Description}'...";

        try
        {
            var manifest = SelectedBackup.Manifest;
            int restored = 0;
            int failed   = 0;

            await Task.Run(() =>
            {
                foreach (var artifact in manifest.Artifacts.Reverse())
                {
                    try
                    {
                        switch (artifact.Kind)
                        {
                            case ArtifactKind.Registry:
                                RegistryBackup.Restore(artifact);
                                break;
                            case ArtifactKind.File:
                                RestoreFile(artifact);
                                break;
                        }
                        restored++;
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        _logger.LogError(ex, "Restore failed for {Path}", artifact.Path);
                    }
                }
            });

            StatusSuccess = failed == 0;
            StatusMessage = failed == 0
                ? $"Restored {restored} artifact(s) — restart to apply"
                : $"Restored {restored}, failed {failed} — check logs";

            _logger.LogInformation("Restore complete: {Ok} ok, {Fail} failed", restored, failed);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Restore error: {ex.Message}";
            _logger.LogError(ex, "Unexpected restore error");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Selects the most recent backup and restores it — one-click undo.
    /// </summary>
    [RelayCommand]
    public async Task UndoLastApplyAsync()
    {
        if (IsBusy || Backups.Count == 0) return;
        SelectedBackup = Backups[0]; // list is ordered newest-first
        await RestoreSelectedAsync();
    }

    [RelayCommand]
    public void OpenFolder()
    {
        if (SelectedBackup is null) return;
        try
        {
            System.Diagnostics.Process.Start("explorer.exe", SelectedBackup.Directory);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Cannot open folder: {ex.Message}";
        }
    }

    [RelayCommand]
    public void DeleteSelected()
    {
        if (SelectedBackup is null) return;

        string desc = SelectedBackup.Description;
        try
        {
            Directory.Delete(SelectedBackup.Directory, recursive: true);
            Backups.Remove(SelectedBackup);
            SelectedBackup = Backups.Count > 0 ? Backups[0] : null;
            StatusMessage  = $"Deleted '{desc}'";
            StatusSuccess  = false;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Delete failed: {ex.Message}";
            _logger.LogError(ex, "Failed to delete backup {Dir}", SelectedBackup?.Directory);
        }
    }

    // ── Internals ──────────────────────────────────────────────────────────

    private static void RestoreFile(BackupArtifact artifact)
    {
        string destName = Path.GetFileNameWithoutExtension(artifact.Path);
        string dest = destName
            .Replace("__SYSTEM32__", @"C:\Windows\System32")
            .Replace("__WINDIR__", @"C:\Windows")
            .Replace('_', Path.DirectorySeparatorChar);

        string? dir = Path.GetDirectoryName(dest);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.Copy(artifact.Path, dest, overwrite: true);
    }
}

public sealed class BackupEntry
{
    public BackupManifest Manifest   { get; }
    public string         Directory  { get; }

    public string   Id           => Manifest.Id;
    public string   Description  => Manifest.Description;
    public DateTime CreatedUtc   => Manifest.CreatedUtc;
    public uint     OsBuild      => Manifest.OsBuild;
    public int      ArtifactCount => Manifest.Artifacts.Length;

    public string ArtifactSummary
    {
        get
        {
            int reg  = Manifest.Artifacts.Count(a => a.Kind == ArtifactKind.Registry);
            int file = Manifest.Artifacts.Count(a => a.Kind == ArtifactKind.File);
            var parts = new List<string>();
            if (reg  > 0) parts.Add($"{reg} registry");
            if (file > 0) parts.Add($"{file} file(s)");
            return parts.Count > 0 ? string.Join(", ", parts) : "empty";
        }
    }

    public BackupEntry(BackupManifest manifest, string directory)
    {
        Manifest  = manifest;
        Directory = directory;
    }
}
