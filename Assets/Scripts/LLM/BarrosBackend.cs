using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace creator_ui.LLM
{
    // Client for the Barros sidecar backend (Ghenghis/Barros-Pizza-Creator).
    // Endpoints:
    //   GET  /health
    //   POST /compose -> returns full PC3 PizzaModel JSON (with .final shape)
    //   POST /chat    -> chat-mode turn (returns assistant message + optional recipe)
    //   POST /lab     -> batch of N recipes ranked by taste
    //
    // The Barros sidecar handles all LLM orchestration; this client just calls its REST API.
    public class BarrosBackend
    {
        private readonly HttpClient _http;
        private readonly string _baseUrl;

        public string BaseUrl => _baseUrl;

        public BarrosBackend(string baseUrl = "http://127.0.0.1:48173")
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
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

        // /compose — full PizzaModel recipe from theme prompt
        public async Task<string> ComposeAsync(string systemPrompt, string userPrompt, string heat = "Medium")
        {
            var payload = "{\"system\":\"" + Escape(systemPrompt) +
                          "\",\"user\":\"" + Escape(userPrompt) +
                          "\",\"heat\":\"" + heat + "\"}";
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var resp = await _http.PostAsync($"{_baseUrl}/compose", content);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadAsStringAsync();
        }

        // /chat — chat-mode turn; returns assistant text (recipe JSON embedded)
        public async Task<string> ChatAsync(string systemPrompt, string userPrompt, List<(string role, string content)> history = null)
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
        public async Task<string> LabAsync(string systemPrompt, string[] tags, int count = 3)
        {
            var sb = new StringBuilder();
            sb.Append("{\"system\":\"").Append(Escape(systemPrompt)).Append("\",\"tags\":[");
            for (int i = 0; i < tags.Length; i++)
            {
                if (i > 0) sb.Append(",");
                sb.Append("\"").Append(Escape(tags[i])).Append("\"");
            }
            sb.Append("],\"count\":").Append(count).Append("}");
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
