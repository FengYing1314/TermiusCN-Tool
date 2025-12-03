using System;

namespace TermiusCN_Tool.Models;

/// <summary>
/// 备份文件信息
/// </summary>
public class BackupInfo
{
    /// <summary>
    /// 备份文件路径
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// 备份时间
    /// </summary>
    public DateTime BackupTime { get; set; }

    /// <summary>
    /// 原 Termius 版本号
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// 格式化的文件大小显示
    /// </summary>
    public string FileSizeDisplay
    {
        get
        {
            const long KB = 1024;
            const long MB = KB * 1024;
            const long GB = MB * 1024;

            return FileSize switch
            {
                >= GB => $"{FileSize / (double)GB:F2} GB",
                >= MB => $"{FileSize / (double)MB:F2} MB",
                >= KB => $"{FileSize / (double)KB:F2} KB",
                _ => $"{FileSize} B"
            };
        }
    }

    /// <summary>
    /// 格式化的备份时间显示
    /// </summary>
    public string BackupTimeDisplay => BackupTime.ToString("yyyy-MM-dd HH:mm:ss");
}
