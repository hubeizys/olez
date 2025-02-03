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
            Log.Information($"[ChatService] ��ʼ����ɣ�BaseUrl: {BaseUrl}");
        }

        public async Task<string> CreateNewSession(string title = "�»Ự")
        {
            var session = await _chatDbService.CreateSessionAsync(title);
            if (session == null)
            {
                throw new InvalidOperationException("�����Ựʧ��");
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
            Log.Information($"[ChatService] ��ʼ������Ϣ: Model={model}, Message={message}");
            
            // �����û���Ϣ�����ݿ�
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
            Log.Information($"[ChatService] ����JSON: {jsonRequest}");

            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/chat")
            {
                Content = content
            };

            try
            {
                Log.Information("[ChatService] ����HTTP����Ollama API");
                var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
                Log.Information($"[ChatService] HTTP����ɹ���״̬��: {response.StatusCode}");
                return ReadResponseStream(response);
            }
            catch (Exception ex)
            {
                Log.Error($"[ChatService] HTTP����ʧ��: {ex.Message}");
                throw;
            }
        }

        private async IAsyncEnumerable<string> ReadResponseStream(HttpResponseMessage response)
        {
            Log.Information("[ChatService] ��ʼ��ȡ��Ӧ��");
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
                    Log.Information($"[ChatService] ��ȡ�� {lineCount} ��ԭʼ����: {line}");
                    string? content = null;
                    bool isDone = false;
                    
                    try
                    {
                        using var doc = JsonDocument.Parse(line);
                        var messageElement = doc.RootElement.GetProperty("message");
                        content = messageElement.GetProperty("content").GetString();
                        Log.Information($"[ChatService] ����JSON�������: {content}");
                        
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
                        Log.Error($"[ChatService] ������Ӧ����ʧ��: {ex.Message}, ԭʼ����: {line}");
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
                            // ��������ʱ�������������ֻظ�
                            await _chatDbService.AddMessageAsync(_currentSessionId, assistantResponse, false);
                        }
                    }
                }
            }
            Log.Information("[ChatService] ��Ӧ����ȡ���");
        }

        public async Task<string> SendMessageAsync(string message, string model)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentNullException(nameof(message));
            }

            // �����û���Ϣ�����ݿ�
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
                throw new InvalidOperationException("���յ�����Ϣ����Ϊ��");
            }

            // �������ֻظ������ݿ�
            if (!string.IsNullOrEmpty(_currentSessionId))
            {
                await _chatDbService.AddMessageAsync(_currentSessionId, messageContent, false);
            }

            return messageContent;
        }
    }
}