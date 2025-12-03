namespace TermiusCN_Tool.Models;

/// <summary>
/// 应用配置
/// </summary>
public class AppConfig
{
    /// <summary>
    /// 自定义 Termius 安装路径（为空则自动检测）
    /// </summary>
    public string CustomTermiusPath { get; set; } = string.Empty;

    /// <summary>
    /// 默认汉化类型
    /// </summary>
    public LocalizeType DefaultLocalizeType { get; set; } = LocalizeType.Trial;

    /// <summary>
    /// 启动时自动检查更新
    /// </summary>
    public bool AutoCheckUpdate { get; set; } = true;

    /// <summary>
    /// 显示警告对话框
    /// </summary>
    public bool ShowWarnings { get; set; } = true;

    /// <summary>
    /// HTTP 代理地址（可选）
    /// </summary>
    public string ProxyAddress { get; set; } = string.Empty;

    /// <summary>
    /// HTTP 代理端口（0 表示不使用代理）
    /// </summary>
    public int ProxyPort { get; set; } = 0;

    /// <summary>
    /// 是否配置了代理
    /// </summary>
    public bool IsProxyEnabled => !string.IsNullOrWhiteSpace(ProxyAddress) && ProxyPort > 0;
}
