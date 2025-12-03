using System.Threading.Tasks;
using TermiusCN_Tool.Models;

namespace TermiusCN_Tool.Services;

/// <summary>
/// 配置服务接口
/// </summary>
public interface IConfigService
{
    /// <summary>
    /// 加载配置
    /// </summary>
    Task<AppConfig> LoadConfigAsync();

    /// <summary>
    /// 保存配置
    /// </summary>
    Task SaveConfigAsync(AppConfig config);

    /// <summary>
    /// 获取配置文件路径
    /// </summary>
    string GetConfigPath();
}
