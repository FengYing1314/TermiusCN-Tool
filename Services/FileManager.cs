using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TermiusCN_Tool.Models;

namespace TermiusCN_Tool.Services;

/// <summary>
/// 文件管理服务实现
/// </summary>
public class FileManager : IFileManager
{
    private readonly string _backupDirectory;
    private readonly string _metadataFilePath;

    public FileManager()
    {
        _backupDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TermiusCN-Tool",
            "Backups"
        );
        _metadataFilePath = Path.Combine(_backupDirectory, "metadata.json");

        // 确保备份目录存在
        if (!Directory.Exists(_backupDirectory))
        {
            Directory.CreateDirectory(_backupDirectory);
        }
    }

    public async Task<BackupInfo> BackupAsync(string asarPath, string version)
    {
        if (!File.Exists(asarPath))
        {
            throw new FileNotFoundException("未找到 app.asar 文件", asarPath);
        }

        // 生成备份文件名: app.asar.backup.20231204_153000
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var backupFileName = $"app.asar.backup.{timestamp}";
        var backupFilePath = Path.Combine(_backupDirectory, backupFileName);

        // 复制文件
        File.Copy(asarPath, backupFilePath, overwrite: false);

        // 创建备份信息
        var fileInfo = new FileInfo(backupFilePath);
        var backup = new BackupInfo
        {
            FilePath = backupFilePath,
            BackupTime = DateTime.Now,
            Version = version,
            FileSize = fileInfo.Length
        };

        // 保存元数据
        await SaveMetadataAsync(backup);

        return backup;
    }

    public async Task ReplaceAsync(string sourceFile, string targetPath)
    {
        if (!File.Exists(sourceFile))
        {
            throw new FileNotFoundException("源文件不存在", sourceFile);
        }

        // 验证文件完整性
        var fileInfo = new FileInfo(sourceFile);
        if (fileInfo.Length == 0)
        {
            throw new InvalidOperationException("源文件为空，无法替换");
        }

        // 原子替换
        File.Copy(sourceFile, targetPath, overwrite: true);

        // 删除临时文件
        try
        {
            File.Delete(sourceFile);
        }
        catch
        {
            // 忽略删除临时文件失败
        }

        await Task.CompletedTask;
    }

    public async Task RestoreAsync(BackupInfo backupInfo, string targetPath)
    {
        if (!File.Exists(backupInfo.FilePath))
        {
            throw new FileNotFoundException("备份文件不存在", backupInfo.FilePath);
        }

        File.Copy(backupInfo.FilePath, targetPath, overwrite: true);
        await Task.CompletedTask;
    }

    public async Task<List<BackupInfo>> GetBackupsAsync()
    {
        if (!File.Exists(_metadataFilePath))
        {
            return new List<BackupInfo>();
        }

        try
        {
            var json = await File.ReadAllTextAsync(_metadataFilePath);
            var backups = JsonConvert.DeserializeObject<List<BackupInfo>>(json) ?? new List<BackupInfo>();

            // 过滤掉已删除的备份文件
            return backups.Where(b => File.Exists(b.FilePath))
                         .OrderByDescending(b => b.BackupTime)
                         .ToList();
        }
        catch
        {
            return new List<BackupInfo>();
        }
    }

    public async Task DeleteBackupAsync(BackupInfo backupInfo)
    {
        // 删除文件
        if (File.Exists(backupInfo.FilePath))
        {
            File.Delete(backupInfo.FilePath);
        }

        // 更新元数据
        var backups = await GetBackupsAsync();
        backups.RemoveAll(b => b.FilePath == backupInfo.FilePath);

        var json = JsonConvert.SerializeObject(backups, Formatting.Indented);
        await File.WriteAllTextAsync(_metadataFilePath, json);
    }

    private async Task SaveMetadataAsync(BackupInfo newBackup)
    {
        var backups = await GetBackupsAsync();
        backups.Add(newBackup);

        var json = JsonConvert.SerializeObject(backups, Formatting.Indented);
        await File.WriteAllTextAsync(_metadataFilePath, json);
    }
}
