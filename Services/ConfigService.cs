using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TermiusCN_Tool.Models;

namespace TermiusCN_Tool.Services;

/// <summary>
/// 配置服务实现
/// </summary>
public class ConfigService : IConfigService
{
    private readonly string _configDirectory;
    private readonly string _configFilePath;

    public ConfigService()
    {
        _configDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TermiusCN-Tool"
        );
        _configFilePath = Path.Combine(_configDirectory, "config.json");
    }

    public async Task<AppConfig> LoadConfigAsync()
    {
        try
        {
            if (!File.Exists(_configFilePath))
            {
                // 首次运行，创建默认配置
                var defaultConfig = new AppConfig();
                await SaveConfigAsync(defaultConfig);
                return defaultConfig;
            }

            var json = await File.ReadAllTextAsync(_configFilePath);
            return JsonConvert.DeserializeObject<AppConfig>(json) ?? new AppConfig();
        }
        catch
        {
            // 配置文件损坏，返回默认配置
            return new AppConfig();
        }
    }

    public async Task SaveConfigAsync(AppConfig config)
    {
        try
        {
            // 确保目录存在
            if (!Directory.Exists(_configDirectory))
            {
                Directory.CreateDirectory(_configDirectory);
            }

            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            await File.WriteAllTextAsync(_configFilePath, json);
        }
        catch (Exception ex)
        {
            throw new Exception($"保存配置失败: {ex.Message}", ex);
        }
    }

    public string GetConfigPath() => _configFilePath;
}
