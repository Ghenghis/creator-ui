using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace creator_ui.LLM
{
    public class BarrosBackend
    {
        private readonly HttpClient _http;
        private readonly string _baseUrl;

        public string BaseUrl => _baseUrl;

        public BarrosBackend(string baseUrl = "http://127.0.0.1:48173")
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(180) };
        }

        public async Task<bool> HealthAsync()
        {
            try
            {
                var resp = await _http.GetAsync($"{_baseUrl}/health");
                return resp.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        // /compose — accepts full catalog (87 ingredients), returns BarrosComposeResponse
        public async Task<string> ComposeWithCatalogAsync(string userPrompt, string[] catalogJsonArray, string heat = "Medium")
        {
            var sb = new StringBuilder();
            sb.Append("{\"prompt\":\"").Append(Escape(userPrompt)).Append("\",\"heat\":\"").Append(heat).Append("\",\"count\":1,\"catalog\":[");
            for (int i = 0; i < catalogJsonArray.Length; i++)
            {
                if (i > 0) sb.Append(",");
                sb.Append(catalogJsonArray[i]);
            }
            sb.Append("]}");
            var content = new StringContent(sb.ToString(), Encoding.UTF8, "application/json");
            var resp = await _http.PostAsync($"{_baseUrl}/compose", content);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadAsStringAsync();
        }

        // /chat — chat-mode turn with history
        public async Task<string> ChatAsync(string systemPrompt, string userPrompt, System.Collections.Generic.List<(string role, string content)> history = null)
        {
            var sb = new StringBuilder();
            sb.Append("{\"system\":\"").Append(Escape(systemPrompt)).Append("\",\"user\":\"").Append(Escape(userPrompt)).Append("\"");
            if (history != null && history.Count > 0)
            {
                sb.Append(",\"history\":[");
                for (int i = 0; i < history.Count; i++)
                {
                    if (i > 0) sb.Append(",");
                    sb.Append("{\"role\":\"").Append(Escape(history[i].role)).Append("\",\"content\":\"").Append(Escape(history[i].content)).Append("\"}");
                }
                sb.Append("]");
            }
            sb.Append("}");
            var content = new StringContent(sb.ToString(), Encoding.UTF8, "application/json");
            var resp = await _http.PostAsync($"{_baseUrl}/chat", content);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadAsStringAsync();
        }

        // /lab — batch generation
        public async Task<string> LabAsync(string systemPrompt, string[] tags, int count = 3, string[] catalogJsonArray = null)
        {
            var sb = new StringBuilder();
            sb.Append("{\"prompt\":\"").Append(Escape(systemPrompt)).Append("\",\"tags\":[");
            for (int i = 0; i < tags.Length; i++)
            {
                if (i > 0) sb.Append(",");
                sb.Append("\"").Append(Escape(tags[i])).Append("\"");
            }
            sb.Append("],\"count\":").Append(count);
            if (catalogJsonArray != null && catalogJsonArray.Length > 0)
            {
                sb.Append(",\"catalog\":[");
                for (int i = 0; i < catalogJsonArray.Length; i++)
                {
                    if (i > 0) sb.Append(",");
                    sb.Append(catalogJsonArray[i]);
                }
                sb.Append("]");
            }
            sb.Append("}");
            var content = new StringContent(sb.ToString(), Encoding.UTF8, "application/json");
            var resp = await _http.PostAsync($"{_baseUrl}/lab", content);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadAsStringAsync();
        }

        private static string Escape(string s)
        {
            if (s == null) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
        }
    }
}
