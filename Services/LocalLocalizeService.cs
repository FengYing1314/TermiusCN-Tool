using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TermiusCN_Tool.Helpers;
using TermiusCN_Tool.Models;

namespace TermiusCN_Tool.Services;

public class LocalLocalizeService : ILocalLocalizeService
{
    private readonly IConfigService _configService;
    private readonly string _workDir;
    private readonly string _rulesDir;
    private readonly string _extractDir;
    private readonly string _asarCmd;

    // Rules URL
    private const string BaseRuleUrl = "https://raw.githubusercontent.com/ArcSurge/Termius-Pro-zh_CN/main/rules/";
    
    public LocalLocalizeService(IConfigService configService)
    {
        _configService = configService;
        
        var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TermiusCN-Tool");
        _workDir = Path.Combine(appData, "LocalBuild");
        _rulesDir = Path.Combine(_workDir, "rules");
        _extractDir = Path.Combine(_workDir, "extract");

        // Determine asar command
        _asarCmd = OperatingSystem.IsWindows() ? "asar.cmd" : "asar";
    }

    public async Task<bool> IsAsarAvailableAsync()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _asarCmd,
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null) return false;
            
            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public async Task DownloadRulesAsync()
    {
        EnsureDirectoryExists(_rulesDir);
        
        var files = new[] { "localize.txt", "trial.txt", "skip_login.txt", "style.txt" };
        var config = await _configService.LoadConfigAsync();

        using var client = HttpHelper.CreateClient(config);

        foreach (var file in files)
        {
            try
            {
                var url = $"{BaseRuleUrl}{file}";
                var content = await client.GetStringAsync(url);
                await File.WriteAllTextAsync(Path.Combine(_rulesDir, file), content);
            }
            catch (Exception ex)
            {
                throw new Exception($"无法下载规则文件 {file}: {ex.Message}");
            }
        }
    }

    public async Task<string> ExecuteLocalizeAsync(string asarPath, LocalizeType type)
    {
        // 1. 准备工作区
        if (Directory.Exists(_extractDir))
            Directory.Delete(_extractDir, true);
        EnsureDirectoryExists(_extractDir);

        // 2. 解包
        await RunAsarCommandAsync("extract", $"\"{asarPath}\"", $"\"{_extractDir}\"");

        // 3. 加载规则
        var rules = new List<string>();
        
        // 基础汉化规则 (所有类型都需要)
        if (File.Exists(Path.Combine(_rulesDir, "localize.txt")))
            rules.AddRange(await File.ReadAllLinesAsync(Path.Combine(_rulesDir, "localize.txt")));

        // 试用版规则
        if (type == LocalizeType.Trial && File.Exists(Path.Combine(_rulesDir, "trial.txt")))
            rules.AddRange(await File.ReadAllLinesAsync(Path.Combine(_rulesDir, "trial.txt")));

        // 离线/跳过登录规则
        if (type == LocalizeType.SkipLogin && File.Exists(Path.Combine(_rulesDir, "skip_login.txt")))
            rules.AddRange(await File.ReadAllLinesAsync(Path.Combine(_rulesDir, "skip_login.txt")));
            
        // 样式修改 (默认启用)
        if (File.Exists(Path.Combine(_rulesDir, "style.txt")))
             rules.AddRange(await File.ReadAllLinesAsync(Path.Combine(_rulesDir, "style.txt")));

        // 4. 应用规则
        await ApplyRulesAsync(_extractDir, rules);

        // 5. 打包
        var outputAsar = Path.Combine(_workDir, "app.asar");
        if (File.Exists(outputAsar)) File.Delete(outputAsar);

        // asar pack app.asar.unpack app.asar --unpack-dir "{node_modules/@termius,out}"
        await RunAsarCommandAsync("pack", $"\"{_extractDir}\"", $"\"{outputAsar}\"", "--unpack-dir", "\"{node_modules/@termius,out}\"");

        return outputAsar;
    }

    private async Task ApplyRulesAsync(string rootDir, List<string> rules)
    {
        // 限制扫描目录，与 lang.py 保持一致，避免误伤 node_modules 等目录
        var targetSubDirs = new[] 
        { 
            Path.Combine(rootDir, "background-process", "assets"),
            Path.Combine(rootDir, "ui-process", "assets"),
            Path.Combine(rootDir, "main-process")
        };

        var targetFiles = new List<string>();

        foreach (var dir in targetSubDirs)
        {
            if (Directory.Exists(dir))
            {
                // 扫描 .js 和 .css 文件
                targetFiles.AddRange(Directory.GetFiles(dir, "*.js", SearchOption.AllDirectories));
                targetFiles.AddRange(Directory.GetFiles(dir, "*.css", SearchOption.AllDirectories));
            }
        }

        foreach (var file in targetFiles)
        {
            var content = await File.ReadAllTextAsync(file);
            // 规范化换行符，避免因换行符差异导致规则匹配失败
            // content = content.Replace("\r\n", "\n"); 
            
            var originalContent = content;
            bool changed = false;

            foreach (var rule in rules)
            {
                if (string.IsNullOrWhiteSpace(rule) || rule.TrimStart().StartsWith("#")) continue;

                var parts = rule.Split('|', 2);
                if (parts.Length != 2) continue;

                var oldVal = parts[0];
                var newVal = parts[1];

                try 
                {
                    if (IsRegex(oldVal))
                    {
                        // 移除首尾的 /
                        var pattern = oldVal.Substring(1, oldVal.Length - 2);
                        
                        // --- Python Regex Pattern 转换 ---
                        // 1. (?P<name>...) -> (?<name>...)
                        pattern = Regex.Replace(pattern, @"\(\?P<([^>]+)>", "(?<$1>");
                        // 2. (?P=name) -> \k<name>
                        pattern = Regex.Replace(pattern, @"\(\?P=([^>]+)\)", @"\k<$1>");

                        // --- Python Regex Replacement 转换 ---
                        var csharpNewVal = newVal;

                        // 0. 预处理: 将所有字面量 $ 转义为 $$ (防止 C# 将其误认为替换符)
                        // 注意: 必须在处理 backreferences 之前做
                        csharpNewVal = csharpNewVal.Replace("$", "$$");

                        // 1. \g<digits> -> $digits (Python 明确编号引用 -> C# 编号引用)
                        // 替换为 "$$$1" -> 结果字符串 "$1" (其中 $ 为替换符)
                        csharpNewVal = Regex.Replace(csharpNewVal, @"\\g<(\d+)>", "$$$1");

                        // 2. \g<name> -> ${name} (Python 命名引用 -> C# 命名引用)
                        // 替换为 "$${$1}" -> 结果字符串 "${name}"
                        csharpNewVal = Regex.Replace(csharpNewVal, @"\\g<([a-zA-Z_]\w*)>", "$${$1}");

                        // 3. \1, \2 -> $1, $2 (标准编号引用)
                        // 排除已被转义的 \\1
                        csharpNewVal = Regex.Replace(csharpNewVal, @"(?<!\\)\\(\d+)", "$$$1");

                        if (Regex.IsMatch(content, pattern))
                        {
                            content = Regex.Replace(content, pattern, csharpNewVal);
                            changed = true;
                        }
                    }
                    else
                    {
                        if (content.Contains(oldVal))
                        {
                            content = content.Replace(oldVal, newVal);
                            changed = true;
                        }
                    }
                }
                catch (ArgumentException) 
                {
                    // 忽略无效的正则规则
                }
                catch (Exception)
                {
                    // 忽略其他错误
                }
            }

            if (changed)
            {
                await File.WriteAllTextAsync(file, content);
            }
        }
    }

    private bool IsRegex(string s)
    {
        // 必须以 / 开头和结尾，且中间不能包含 // (排除URL等)
        // 长度至少为 3 (e.g. /a/)
        return s.Length > 2 && s.StartsWith("/") && s.EndsWith("/") && !s.Contains("//");
    }

    private async Task RunAsarCommandAsync(params string[] args)
    {
        EnsureDirectoryExists(_workDir);

        var startInfo = new ProcessStartInfo
        {
            FileName = _asarCmd,
            Arguments = string.Join(" ", args),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            // WorkingDirectory = _workDir // 移除工作目录设置，避免目录不存在导致启动失败
        };

        using var process = Process.Start(startInfo);
        if (process == null) throw new Exception("无法启动 asar 进程");

        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new Exception($"asar 命令执行失败: {stderr}");
        }
    }

    private void EnsureDirectoryExists(string dir)
    {
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }
}
