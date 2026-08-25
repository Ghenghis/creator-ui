using System;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;

namespace creator_ui.LLM
{
    public class OpenAIBackend
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;
        private readonly string _model;

        public OpenAIBackend(string apiKey, string model = "gpt-4o-mini")
        {
            _apiKey = apiKey;
            _model = model;
            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        }

        public async Task<string> CompleteAsync(string systemPrompt, string userPrompt)
        {
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
            var messages = new[]
            {
                new LLMMessage("system", systemPrompt),
                new LLMMessage("user", userPrompt)
            };
            var payload = "{\"model\":\"" + _model + "\",\"messages\":" +
                          LLMJson.ArrayOf(messages) +
                          ",\"temperature\":0.3,\"response_format\":{\"type\":\"json_object\"}}";
            var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
            var resp = await _http.PostAsync("https://api.openai.com/v1/chat/completions", content);
            resp.EnsureSuccessStatusCode();
            var body = await resp.Content.ReadAsStringAsync();
            var parsed = JsonUtility.FromJson<LLMResponse>(body);
            return parsed.choices[0].message.content;
        }
    }
}
