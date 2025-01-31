using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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