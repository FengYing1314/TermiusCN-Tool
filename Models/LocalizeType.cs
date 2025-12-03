namespace TermiusCN_Tool.Models;

/// <summary>
/// 汉化版本类型
/// </summary>
public enum LocalizeType
{
    /// <summary>
    /// 标准版 - 仅汉化，适合有会员资格用户
    /// </summary>
    Standard,

    /// <summary>
    /// 试用版 - 汉化+试用，消除 Upgrade now 按钮
    /// </summary>
    Trial,

    /// <summary>
    /// 离线版 - 汉化+跳过登录，支持离线使用
    /// </summary>
    SkipLogin
}
