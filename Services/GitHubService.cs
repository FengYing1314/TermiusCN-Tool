using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using TermiusCN_Tool.Helpers;
using TermiusCN_Tool.Models;

namespace TermiusCN_Tool.Services;

/// <summary>
/// GitHub 服务实现
/// </summary>
public class GitHubService : IGitHubService
{
    private const string RepoOwner = "ArcSurge";
    private const string RepoName = "Termius-Pro-zh_CN";
    private const string ApiBaseUrl = "https://api.github.com";

    private readonly IConfigService _configService;
    private HttpClient? _httpClient;

    public GitHubService(IConfigService configService)
    {
        _configService = configService;
    }

    public async Task<string> GetLatestVersionAsync()
    {
        try
        {
            var client = await GetHttpClientAsync();
            var url = $"{ApiBaseUrl}/repos/{RepoOwner}/{RepoName}/releases/latest";
            var response = await client.GetStringAsync(url);
            var json = JObject.Parse(response);

            // 解析 tag_name (例如: "v9.34.5" -> "9.34.5")
            var tagName = json["tag_name"]?.ToString() ?? "";
            return tagName.TrimStart('v');
        }
        catch
        {
            return string.Empty;
        }
    }

    public async Task<string> DownloadLocalizeFileAsync(string version, LocalizeType localizeType)
    {
        var client = await GetHttpClientAsync();
        var fileName = GetAssetFileName(localizeType);
        
        try
        {
            // 方法 1: 通过 API 获取 Release 信息
            var releaseInfo = await GetReleaseByVersionAsync(version);
            if (releaseInfo != null)
            {
                var assets = releaseInfo["assets"] as JArray;
                if (assets != null)
                {
                    var asset = assets.FirstOrDefault(a => a["name"]?.ToString() == fileName);
                    if (asset != null)
                    {
                        var downloadUrl = asset["browser_download_url"]?.ToString();
                        if (!string.IsNullOrEmpty(downloadUrl))
                        {
                            return await DownloadFileAsync(client, downloadUrl);
                        }
                    }
                }
            }
        }
        catch
        {
            // API 方法失败，尝试直接构建 URL
        }

        try
        {
            // 方法 2: 直接构建下载 URL
            // https://github.com/ArcSurge/Termius-Pro-zh_CN/releases/download/v9.34.5/app-windows-localize-skip.asar
            var normalizedVersion = NormalizeVersion(version);
            var directUrl = $"https://github.com/{RepoOwner}/{RepoName}/releases/download/v{normalizedVersion}/{fileName}";
            
            return await DownloadFileAsync(client, directUrl);
        }
        catch (Exception ex)
        {
            throw new Exception($"下载汉化文件失败: {ex.Message}\n版本: {version}\n文件: {fileName}", ex);
        }
    }

    private async Task<string> DownloadFileAsync(HttpClient client, string url)
    {
        var tempFilePath = Path.GetTempFileName();
        var fileBytes = await client.GetByteArrayAsync(url);
        await File.WriteAllBytesAsync(tempFilePath, fileBytes);
        return tempFilePath;
    }

    public async Task<bool> IsVersionAvailableAsync(string version)
    {
        try
        {
            var releaseInfo = await GetReleaseByVersionAsync(version);
            return releaseInfo != null;
        }
        catch
        {
            return false;
        }
    }

    private async Task<JObject?> GetReleaseByVersionAsync(string version)
    {
        try
        {
            var client = await GetHttpClientAsync();
            var url = $"{ApiBaseUrl}/repos/{RepoOwner}/{RepoName}/releases";
            var response = await client.GetStringAsync(url);
            var releases = JArray.Parse(response);

            // 标准化版本号 (移除末尾的 .0)
            var normalizedVersion = NormalizeVersion(version);

            // 查找匹配版本 (支持 "v9.34.5" 或 "9.34.5" 或 "9.34.5.0")
            foreach (var release in releases)
            {
                var tagName = release["tag_name"]?.ToString() ?? "";
                var cleanTag = NormalizeVersion(tagName.TrimStart('v'));
                
                if (cleanTag == normalizedVersion)
                {
                    return release as JObject;
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
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

    private string GetAssetFileName(LocalizeType type)
    {
        return type switch
        {
            LocalizeType.Standard => "app-windows-localize.asar",
            LocalizeType.Trial => "app-windows-localize-trial.asar",
            LocalizeType.SkipLogin => "app-windows-localize-skip.asar",
            _ => "app-windows-localize.asar"
        };
    }

    private async Task<HttpClient> GetHttpClientAsync()
    {
        if (_httpClient == null)
        {
            var config = await _configService.LoadConfigAsync();
            _httpClient = HttpHelper.CreateClient(config);
        }

        return _httpClient;
    }
}
