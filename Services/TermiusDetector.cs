using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using TermiusCN_Tool.Models;

namespace TermiusCN_Tool.Services;

/// <summary>
/// Termius 检测服务实现
/// </summary>
public class TermiusDetector : ITermiusDetector
{
    private readonly IProcessManager _processManager;
    private const string DefaultRelativePath = @"Programs\Termius";
    private const string TermiusExe = "Termius.exe";
    private const string ResourcesFolder = "resources";
    private const string AsarFileName = "app.asar";
    private const string PackageJsonPath = @"app.asar.unpacked\package.json";

    public TermiusDetector(IProcessManager processManager)
    {
        _processManager = processManager;
    }

    public async Task<TermiusInfo> DetectAsync(string? customPath = null)
    {
        var info = new TermiusInfo();

        try
        {
            // 1. 确定搜索路径
            var searchPath = GetSearchPath(customPath);
            
            // 如果路径不存在，尝试从运行中的进程获取
            if (string.IsNullOrEmpty(searchPath) || !Directory.Exists(searchPath))
            {
                searchPath = _processManager.GetTermiusInstallPathFromProcess();
                if (string.IsNullOrEmpty(searchPath) || !Directory.Exists(searchPath))
                {
                    return info; // IsFound = false
                }
            }

            // 2. 验证 Termius.exe 存在
            var exePath = Path.Combine(searchPath, TermiusExe);
            if (!File.Exists(exePath))
            {
                return info;
            }

            // 3. 验证 resources 文件夹存在
            var resourcesPath = Path.Combine(searchPath, ResourcesFolder);
            if (!Directory.Exists(resourcesPath))
            {
                return info;
            }

            // 4. 查找 app.asar 文件（在 resources 子目录中）
            var asarPath = Path.Combine(resourcesPath, AsarFileName);
            if (!File.Exists(asarPath))
            {
                return info;
            }

            // 5. 获取版本号（尝试多种方法）
            var version = await GetVersionAsync(exePath, resourcesPath);
            if (string.IsNullOrEmpty(version))
            {
                // 如果无法获取版本号，使用默认值
                version = "未知版本";
            }

            // 6. 成功检测
            info.IsFound = true;
            info.InstallPath = searchPath;
            info.AsarPath = asarPath;
            info.Version = version;
        }
        catch
        {
            // 检测失败，返回空信息
        }

        return info;
    }

    private string? GetSearchPath(string? customPath)
    {
        // 1. 优先使用自定义路径
        if (!string.IsNullOrWhiteSpace(customPath))
        {
            return customPath;
        }

        // 2. 尝试默认路径: %LOCALAPPDATA%\Programs\Termius
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var defaultPath = Path.Combine(localAppData, DefaultRelativePath);
        
        if (Directory.Exists(defaultPath))
        {
            return defaultPath;
        }

        // 3. 尝试从运行中的进程获取路径
        var processPath = _processManager.GetTermiusInstallPathFromProcess();
        if (!string.IsNullOrEmpty(processPath))
        {
            return processPath;
        }

        return defaultPath; // 返回默认路径，即使不存在
    }

    private async Task<string> GetVersionAsync(string exePath, string resourcesPath)
    {
        // 方法 1: 从 package.json 读取（如果存在）
        var packageJsonPath = Path.Combine(resourcesPath, PackageJsonPath);
        if (File.Exists(packageJsonPath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(packageJsonPath);
                var obj = JObject.Parse(json);
                var version = obj["version"]?.ToString();
                if (!string.IsNullOrEmpty(version))
                    return NormalizeVersion(version);
            }
            catch { }
        }

        // 方法 2: 从 exe 文件属性读取
        try
        {
            var versionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(exePath);
            if (!string.IsNullOrEmpty(versionInfo.ProductVersion))
            {
                return NormalizeVersion(versionInfo.ProductVersion);
            }
            if (!string.IsNullOrEmpty(versionInfo.FileVersion))
            {
                return NormalizeVersion(versionInfo.FileVersion);
            }
        }
        catch { }

        // 方法 3: 返回默认值
        return "已安装";
    }

    /// <summary>
    /// 标准化版本号，移除末尾多余的 .0
    /// 例如: "9.34.5.0" -> "9.34.5"
    /// </summary>
    private string NormalizeVersion(string version)
    {
        if (string.IsNullOrEmpty(version))
            return version;

        // 移除前缀 'v' 和可能的空格
        version = version.Trim().TrimStart('v');

        // 分割版本号
        var parts = version.Split('.');
        
        // 移除末尾的 0 (但至少保留 3 段)
        var significantParts = new System.Collections.Generic.List<string>();
        for (int i = 0; i < parts.Length; i++)
        {
            // 始终添加前3段
            if (i < 3)
            {
                significantParts.Add(parts[i]);
            }
            // 第4段及以后，只有非0才添加
            else if (parts[i] != "0")
            {
                significantParts.Add(parts[i]);
            }
        }

        return string.Join(".", significantParts);
    }
}
