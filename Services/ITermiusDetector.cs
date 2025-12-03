using System.Threading.Tasks;
using TermiusCN_Tool.Models;

namespace TermiusCN_Tool.Services;

/// <summary>
/// Termius 检测服务接口
/// </summary>
public interface ITermiusDetector
{
    /// <summary>
    /// 检测 Termius 安装信息
    /// </summary>
    Task<TermiusInfo> DetectAsync(string? customPath = null);
}
