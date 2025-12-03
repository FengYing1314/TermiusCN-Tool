using System.Threading.Tasks;

namespace TermiusCN_Tool.Services;

/// <summary>
/// 进程管理服务接口
/// </summary>
public interface IProcessManager
{
    /// <summary>
    /// 检查 Termius 进程是否正在运行
    /// </summary>
    bool IsTermiusRunning();

    /// <summary>
    /// 友好关闭 Termius 进程
    /// </summary>
    /// <param name="timeoutSeconds">超时时间（秒）</param>
    /// <returns>是否成功关闭</returns>
    Task<bool> CloseTermiusAsync(int timeoutSeconds = 5);

    /// <summary>
    /// 强制结束 Termius 进程
    /// </summary>
    void KillTermius();

    /// <summary>
    /// 从运行中的 Termius 进程获取安装路径
    /// </summary>
    /// <returns>Termius 安装路径，如果未找到则返回 null</returns>
    string? GetTermiusInstallPathFromProcess();
}
