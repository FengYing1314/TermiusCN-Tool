namespace TermiusCN_Tool.Models;

/// <summary>
/// Termius 安装信息
/// </summary>
public class TermiusInfo
{
    /// <summary>
    /// 是否找到 Termius 安装
    /// </summary>
    public bool IsFound { get; set; }

    /// <summary>
    /// Termius 安装目录路径
    /// </summary>
    public string InstallPath { get; set; } = string.Empty;

    /// <summary>
    /// app.asar 文件完整路径
    /// </summary>
    public string AsarPath { get; set; } = string.Empty;

    /// <summary>
    /// 当前 Termius 版本号
    /// </summary>
    public string Version { get; set; } = string.Empty;
}
