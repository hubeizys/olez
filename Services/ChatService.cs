using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Serilog;

namespace ollez.Services
{
    public class ChatService : IChatService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "http://localhost:11434"; // 注意：BaseUrl 不需要包含 "/api"

        public ChatService()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(BaseUrl)
            };
            Log.Information($"[ChatService] 初始化完成，BaseUrl: {BaseUrl}");
        }

        public async Task<IAsyncEnumerable<string>> SendMessageStreamAsync(string message, string model)
        {
            Log.Information($"[ChatService] 开始发送消息: Model={model}, Message={message}");
            var request = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "user", content = message }
                },
                stream = true // 启用流式响应
            };

            var jsonRequest = JsonSerializer.Serialize(request);
            Log.Information($"[ChatService] 请求JSON: {jsonRequest}");

            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            Log.Information("[ChatService] 发送HTTP请求到Ollama API");
            var response = await _httpClient.PostAsync("/api/chat", content);
            
            try 
            {
                response.EnsureSuccessStatusCode();
                Log.Information($"[ChatService] HTTP请求成功，状态码: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                Log.Error($"[ChatService] HTTP请求失败: {ex.Message}");
                throw;
            }

            return ReadResponseStream(response);
        }

        private async IAsyncEnumerable<string> ReadResponseStream(HttpResponseMessage response)
        {
            Log.Information("[ChatService] 开始读取响应流");
            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new System.IO.StreamReader(stream);
            int lineCount = 0;

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                lineCount++;
                
                if (!string.IsNullOrWhiteSpace(line))
                {
                    Log.Information($"[ChatService] 读取到第 {lineCount} 行原始数据: {line}");
                    string content = null;
                    bool isDone = false;
                    
                    try
                    {
                        using var doc = JsonDocument.Parse(line);
                        var messageElement = doc.RootElement.GetProperty("message");
                        content = messageElement.GetProperty("content").GetString();
                        Log.Information($"[ChatService] 解析JSON后的内容: {content}");
                        
                        // 检查是否是最后一条消息
                        if (doc.RootElement.TryGetProperty("done", out var doneElement))
                        {
                            isDone = doneElement.GetBoolean();
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"[ChatService] 解析响应数据失败: {ex.Message}, 原始数据: {line}");
                        throw;
                    }
                    
                    // 只有在不是最后一条消息时才返回内容
                    if (!isDone)
                    {
                        yield return content ?? string.Empty;
                    }
                }
            }
            Log.Information("[ChatService] 响应流读取完成");
        }

        public async Task<string> SendMessageAsync(string message, string model)
        {
            var request = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "user", content = message }
                },
                stream = false
            };

            var content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            // 修改 API 路径为 "/api/chat"
            var response = await _httpClient.PostAsync("/api/chat", content);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(jsonResponse);

            // 根据 Ollama 的 API 响应格式解析
            var messageContent = doc.RootElement
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return messageContent;
        }
    }
}