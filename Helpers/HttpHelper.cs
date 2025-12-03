using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using TermiusCN_Tool.Models;

namespace TermiusCN_Tool.Helpers;

/// <summary>
/// HTTP 辅助类，支持代理配置
/// </summary>
public static class HttpHelper
{
    /// <summary>
    /// 创建配置了代理的 HttpClient
    /// </summary>
    public static HttpClient CreateClient(AppConfig? config = null)
    {
        var handler = new HttpClientHandler();

        // 配置代理
        if (config?.IsProxyEnabled == true)
        {
            handler.Proxy = new WebProxy($"http://{config.ProxyAddress}:{config.ProxyPort}");
            handler.UseProxy = true;
        }

        var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        // 设置 User-Agent 避免被 GitHub 拦截
        client.DefaultRequestHeaders.Add("User-Agent", "TermiusCN-Tool/1.0");
        client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

        return client;
    }

    /// <summary>
    /// 测试网络连接和代理配置
    /// </summary>
    public static async Task<(bool Success, string Message)> TestConnectionAsync(AppConfig? config = null)
    {
        try
        {
            using var client = CreateClient(config);
            var response = await client.GetAsync("https://api.github.com");

            if (response.IsSuccessStatusCode)
            {
                return (true, "连接成功");
            }

            return (false, $"连接失败: HTTP {(int)response.StatusCode}");
        }
        catch (TaskCanceledException)
        {
            return (false, "连接超时，请检查网络或代理配置");
        }
        catch (HttpRequestException ex)
        {
            return (false, $"网络错误: {ex.Message}");
        }
        catch (Exception ex)
        {
            return (false, $"未知错误: {ex.Message}");
        }
    }
}
