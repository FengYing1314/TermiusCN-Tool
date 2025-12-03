using System.Collections.Generic;
using System.Threading.Tasks;
using TermiusCN_Tool.Models;

namespace TermiusCN_Tool.Services;

/// <summary>
/// 文件管理服务接口
/// </summary>
public interface IFileManager
{
    /// <summary>
    /// 备份 app.asar 文件
    /// </summary>
    /// <param name="asarPath">app.asar 文件路径</param>
    /// <param name="version">Termius 版本号</param>
    /// <returns>备份信息</returns>
    Task<BackupInfo> BackupAsync(string asarPath, string version);

    /// <summary>
    /// 替换 app.asar 文件
    /// </summary>
    /// <param name="sourceFile">源文件路径</param>
    /// <param name="targetPath">目标路径</param>
    Task ReplaceAsync(string sourceFile, string targetPath);

    /// <summary>
    /// 还原备份
    /// </summary>
    /// <param name="backupInfo">备份信息</param>
    /// <param name="targetPath">还原目标路径</param>
    Task RestoreAsync(BackupInfo backupInfo, string targetPath);

    /// <summary>
    /// 获取所有备份列表
    /// </summary>
    Task<List<BackupInfo>> GetBackupsAsync();

    /// <summary>
    /// 删除备份
    /// </summary>
    Task DeleteBackupAsync(BackupInfo backupInfo);
}
