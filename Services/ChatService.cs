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
        private const string BaseUrl = "http://localhost:11434"; // ע�⣺BaseUrl ����Ҫ���� "/api"

        public ChatService()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(BaseUrl)
            };
            Log.Information($"[ChatService] ��ʼ����ɣ�BaseUrl: {BaseUrl}");
        }

        public async Task<IAsyncEnumerable<string>> SendMessageStreamAsync(string message, string model)
        {
            Log.Information($"[ChatService] ��ʼ������Ϣ: Model={model}, Message={message}");
            var request = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "user", content = message }
                },
                stream = true // ������ʽ��Ӧ
            };

            var jsonRequest = JsonSerializer.Serialize(request);
            Log.Information($"[ChatService] ����JSON: {jsonRequest}");

            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            Log.Information("[ChatService] ����HTTP����Ollama API");
            var response = await _httpClient.PostAsync("/api/chat", content);
            
            try 
            {
                response.EnsureSuccessStatusCode();
                Log.Information($"[ChatService] HTTP����ɹ���״̬��: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                Log.Error($"[ChatService] HTTP����ʧ��: {ex.Message}");
                throw;
            }

            return ReadResponseStream(response);
        }

        private async IAsyncEnumerable<string> ReadResponseStream(HttpResponseMessage response)
        {
            Log.Information("[ChatService] ��ʼ��ȡ��Ӧ��");
            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new System.IO.StreamReader(stream);
            int lineCount = 0;

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                lineCount++;
                
                if (!string.IsNullOrWhiteSpace(line))
                {
                    Log.Information($"[ChatService] ��ȡ���� {lineCount} ��ԭʼ����: {line}");
                    string content = null;
                    bool isDone = false;
                    
                    try
                    {
                        using var doc = JsonDocument.Parse(line);
                        var messageElement = doc.RootElement.GetProperty("message");
                        content = messageElement.GetProperty("content").GetString();
                        Log.Information($"[ChatService] ����JSON�������: {content}");
                        
                        // ����Ƿ������һ����Ϣ
                        if (doc.RootElement.TryGetProperty("done", out var doneElement))
                        {
                            isDone = doneElement.GetBoolean();
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"[ChatService] ������Ӧ����ʧ��: {ex.Message}, ԭʼ����: {line}");
                        throw;
                    }
                    
                    // ֻ���ڲ������һ����Ϣʱ�ŷ�������
                    if (!isDone)
                    {
                        yield return content ?? string.Empty;
                    }
                }
            }
            Log.Information("[ChatService] ��Ӧ����ȡ���");
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

            // �޸� API ·��Ϊ "/api/chat"
            var response = await _httpClient.PostAsync("/api/chat", content);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(jsonResponse);

            // ���� Ollama �� API ��Ӧ��ʽ����
            var messageContent = doc.RootElement
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return messageContent;
        }
    }
}