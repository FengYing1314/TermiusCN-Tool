using System.Threading.Tasks;
using TermiusCN_Tool.Models;

namespace TermiusCN_Tool.Services;

/// <summary>
/// GitHub 服务接口
/// </summary>
public interface IGitHubService
{
    /// <summary>
    /// 获取最新 Release 版本号
    /// </summary>
    Task<string> GetLatestVersionAsync();

    /// <summary>
    /// 下载指定版本的汉化文件
    /// </summary>
    /// <param name="version">版本号</param>
    /// <param name="localizeType">汉化类型</param>
    /// <returns>下载到的临时文件路径</returns>
    Task<string> DownloadLocalizeFileAsync(string version, LocalizeType localizeType);

    /// <summary>
    /// 检查指定版本是否有可用的汉化文件
    /// </summary>
    Task<bool> IsVersionAvailableAsync(string version);
}
