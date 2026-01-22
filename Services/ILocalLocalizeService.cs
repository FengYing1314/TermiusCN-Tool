using System.Collections.Generic;
using System.Threading.Tasks;
using TermiusCN_Tool.Models;

namespace TermiusCN_Tool.Services;

public interface ILocalLocalizeService
{
    /// <summary>
    /// 检查系统环境中是否可用 asar 命令
    /// </summary>
    Task<bool> IsAsarAvailableAsync();

    /// <summary>
    /// 下载最新的规则文件
    /// </summary>
    Task DownloadRulesAsync();

    /// <summary>
    /// 执行本地汉化流程
    /// </summary>
    /// <param name="asarPath">原版 app.asar 路径</param>
    /// <param name="type">汉化类型 (支持 Trial, Localize, SkipLogin, Style)</param>
    /// <returns>生成的 app.asar 路径</returns>
    Task<string> ExecuteLocalizeAsync(string asarPath, LocalizeType type);
}
