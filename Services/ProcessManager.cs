using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace TermiusCN_Tool.Services;

/// <summary>
/// 进程管理服务实现
/// </summary>
public class ProcessManager : IProcessManager
{
    private const string ProcessName = "Termius";

    public bool IsTermiusRunning()
    {
        var processes = Process.GetProcessesByName(ProcessName);
        return processes.Length > 0;
    }

    public async Task<bool> CloseTermiusAsync(int timeoutSeconds = 5)
    {
        var processes = Process.GetProcessesByName(ProcessName);
        if (processes.Length == 0)
        {
            return true; // 没有进程需要关闭
        }

        // 尝试友好关闭每个进程
        foreach (var process in processes)
        {
            try
            {
                process.CloseMainWindow();
            }
            catch
            {
                // 忽略关闭失败
            }
        }

        // 等待进程退出
        var endTime = DateTime.Now.AddSeconds(timeoutSeconds);
        while (DateTime.Now < endTime)
        {
            await Task.Delay(500);

            if (!IsTermiusRunning())
            {
                return true; // 所有进程已关闭
            }
        }

        return false; // 超时，进程未关闭
    }

    public void KillTermius()
    {
        var processes = Process.GetProcessesByName(ProcessName);
        foreach (var process in processes)
        {
            try
            {
                process.Kill();
                process.WaitForExit(2000);
            }
            catch
            {
                // 忽略强制结束失败
            }
        }
    }

    public string? GetTermiusInstallPathFromProcess()
    {
        try
        {
            var processes = Process.GetProcessesByName(ProcessName);
            if (processes.Length == 0)
            {
                return null;
            }

            // 获取第一个进程的可执行文件路径
            var process = processes[0];
            var exePath = process.MainModule?.FileName;

            if (string.IsNullOrEmpty(exePath))
            {
                return null;
            }

            // 返回可执行文件所在的目录
            return System.IO.Path.GetDirectoryName(exePath);
        }
        catch
        {
            return null;
        }
    }
}
