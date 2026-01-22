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
    private readonly string _metadataFilePath;
    public string BackupDirectory => GetRealBackupDirectory();

    public FileManager()
    {
        // 构造函数中不再硬编码路径，而是通过 GetRealBackupDirectory 动态获取
        // _backupDirectory 字段也不再作为主要存储，而是通过属性访问
        _metadataFilePath = Path.Combine(BackupDirectory, "metadata.json");

        EnsureBackupDirectoryExists();
    }

    private string GetRealBackupDirectory()
    {
        string baseDir;
        
        // 检测是否在 MSIX 容器中运行
        if (IsRunningInPackage())
        {
            // 如果是 MSIX 包，Roaming 文件夹会被虚拟化
            // 我们尝试获取 LocalCache 下的真实路径，以便用户可以通过资源管理器直接访问
            try
            {
                // %LocalAppData%\Packages\{PackageFamilyName}\LocalCache\Roaming
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var packageFamilyName = Windows.ApplicationModel.Package.Current.Id.FamilyName;
                var virtualRoamingPath = Path.Combine(localAppData, "Packages", packageFamilyName, "LocalCache", "Roaming");
                
                if (Directory.Exists(virtualRoamingPath))
                {
                    baseDir = Path.Combine(virtualRoamingPath, "TermiusCN-Tool");
                }
                else
                {
                    // 如果 LocalCache 不存在（未发生重定向），则回退到标准的 Roaming 路径
                    // 注意：这可能是一个虚拟路径，但在资源管理器中会自动映射
                    baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TermiusCN-Tool");
                }
            }
            catch
            {
                // 获取包信息失败（可能不是打包运行），回退到标准路径
                baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TermiusCN-Tool");
            }
        }
        else
        {
            // 非打包运行（便携版/调试模式）
            baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TermiusCN-Tool");
        }

        return Path.Combine(baseDir, "Backups");
    }

    private bool IsRunningInPackage()
    {
        try
        {
            return Windows.ApplicationModel.Package.Current != null;
        }
        catch
        {
            return false;
        }
    }

    private void EnsureBackupDirectoryExists()
    {
        var dir = BackupDirectory;
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }

    public async Task<BackupInfo> BackupAsync(string asarPath, string version)
    {
        EnsureBackupDirectoryExists();

        if (!File.Exists(asarPath))
        {
            throw new FileNotFoundException("未找到 app.asar 文件", asarPath);
        }

        // 生成备份文件名: app.asar.backup.20231204_153000
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var backupFileName = $"app.asar.backup.{timestamp}";
        var backupFilePath = Path.Combine(BackupDirectory, backupFileName);

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

    // ReplaceAsync, RestoreAsync 保持不变... (省略以减少 Token，但需注意在实现中保留)
    // 为确保完整性，我将在 new_string 中包含所有方法的完整实现

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

        var targetDir = Path.GetDirectoryName(targetPath);
        if (targetDir == null) throw new DirectoryNotFoundException("目标目录不存在");

        if (!Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        var tempTargetFile = Path.Combine(targetDir, $"app.asar.new.{Guid.NewGuid()}");
        
        try 
        {
            File.Copy(sourceFile, tempTargetFile, overwrite: true);
            
            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }
            
            File.Move(tempTargetFile, targetPath);
        }
        catch
        {
            if (File.Exists(tempTargetFile))
            {
                try { File.Delete(tempTargetFile); } catch { }
            }
            throw;
        }

        try
        {
            File.Delete(sourceFile);
        }
        catch
        {
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

            // 检查文件实际存在性，如果有丢失的文件，更新 metadata
            var validBackups = new List<BackupInfo>();
            bool needsUpdate = false;

            foreach (var b in backups)
            {
                if (File.Exists(b.FilePath))
                {
                    validBackups.Add(b);
                }
                else
                {
                    // 标记需要更新元数据
                    needsUpdate = true;
                }
            }

            if (needsUpdate)
            {
                // 异步更新元数据，移除不存在的记录
                // 注意：这里可能会有并发问题，但在单用户场景下通常可接受
                var newJson = JsonConvert.SerializeObject(validBackups, Formatting.Indented);
                await File.WriteAllTextAsync(_metadataFilePath, newJson);
            }

            return validBackups.OrderByDescending(b => b.BackupTime).ToList();
        }
        catch
        {
            return new List<BackupInfo>();
        }
    }

    public async Task DeleteBackupAsync(BackupInfo backupInfo)
    {
        if (File.Exists(backupInfo.FilePath))
        {
            File.Delete(backupInfo.FilePath);
        }

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
