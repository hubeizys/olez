using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Serilog;
using ollez.Models;

namespace ollez.Services
{
    public class ChatService : IChatService
    {
        private readonly HttpClient _httpClient;
        private readonly IChatDbService _chatDbService;
        private const string BaseUrl = "http://localhost:11434";
        private string _currentSessionId = string.Empty;

        public ChatService(IChatDbService chatDbService)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(BaseUrl)
            };
            _chatDbService = chatDbService ?? throw new ArgumentNullException(nameof(chatDbService));
            Log.Information($"[ChatService] 初始化完成，BaseUrl: {BaseUrl}");
        }

        public async Task<string> CreateNewSession(string title = "新会话")
        {
            var session = await _chatDbService.CreateSessionAsync(title);
            if (session == null)
            {
                throw new InvalidOperationException("创建会话失败");
            }
            _currentSessionId = session.Id;
            return session.Id;
        }

        public string GetCurrentSessionId() => _currentSessionId;

        public void SetCurrentSessionId(string sessionId)
        {
            _currentSessionId = sessionId;
        }

        public async Task<IAsyncEnumerable<string>> SendMessageStreamAsync(string message, string model)
        {
            Log.Information($"[ChatService] 开始发送消息: Model={model}, Message={message}");
            
            // 保存用户消息到数据库
            if (!string.IsNullOrEmpty(_currentSessionId))
            {
                await _chatDbService.AddMessageAsync(_currentSessionId, message, true);
            }

            var request = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "user", content = message }
                },
                stream = true
            };

            var jsonRequest = JsonSerializer.Serialize(request);
            Log.Information($"[ChatService] 请求JSON: {jsonRequest}");

            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/chat")
            {
                Content = content
            };

            try
            {
                Log.Information("[ChatService] 发送HTTP请求到Ollama API");
                var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
                Log.Information($"[ChatService] HTTP请求成功，状态码: {response.StatusCode}");
                return ReadResponseStream(response);
            }
            catch (Exception ex)
            {
                Log.Error($"[ChatService] HTTP请求失败: {ex.Message}");
                throw;
            }
        }

        private async IAsyncEnumerable<string> ReadResponseStream(HttpResponseMessage response)
        {
            Log.Information("[ChatService] 开始读取响应流");
            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new System.IO.StreamReader(stream);
            int lineCount = 0;
            var fullResponse = new StringBuilder();

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (line == null) continue;
                
                lineCount++;
                
                if (!string.IsNullOrWhiteSpace(line))
                {
                    Log.Information($"[ChatService] 读取第 {lineCount} 行原始数据: {line}");
                    string? content = null;
                    bool isDone = false;
                    
                    try
                    {
                        using var doc = JsonDocument.Parse(line);
                        var messageElement = doc.RootElement.GetProperty("message");
                        content = messageElement.GetProperty("content").GetString();
                        Log.Information($"[ChatService] 解析JSON后的内容: {content}");
                        
                        if (doc.RootElement.TryGetProperty("done", out var doneElement))
                        {
                            isDone = doneElement.GetBoolean();
                        }

                        if (!string.IsNullOrEmpty(content))
                        {
                            fullResponse.Append(content);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"[ChatService] 解析响应数据失败: {ex.Message}, 原始数据: {line}");
                        throw;
                    }
                    
                    if (!isDone && !string.IsNullOrEmpty(content))
                    {
                        yield return content;
                    }

                    if (isDone && !string.IsNullOrEmpty(_currentSessionId))
                    {
                        var assistantResponse = fullResponse.ToString();
                        if (!string.IsNullOrEmpty(assistantResponse))
                        {
                            // 在流结束时保存完整的助手回复
                            await _chatDbService.AddMessageAsync(_currentSessionId, assistantResponse, false);
                        }
                    }
                }
            }
            Log.Information("[ChatService] 响应流读取完成");
        }

        public async Task<string> SendMessageAsync(string message, string model)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentNullException(nameof(message));
            }

            // 保存用户消息到数据库
            if (!string.IsNullOrEmpty(_currentSessionId))
            {
                await _chatDbService.AddMessageAsync(_currentSessionId, message, true);
            }

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

            var response = await _httpClient.PostAsync("/api/chat", content);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(jsonResponse);

            var messageContent = doc.RootElement
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrEmpty(messageContent))
            {
                throw new InvalidOperationException("接收到的消息内容为空");
            }

            // 保存助手回复到数据库
            if (!string.IsNullOrEmpty(_currentSessionId))
            {
                await _chatDbService.AddMessageAsync(_currentSessionId, messageContent, false);
            }

            return messageContent;
        }
    }
}