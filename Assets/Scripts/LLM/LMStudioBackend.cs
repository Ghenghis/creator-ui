using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace creator_ui.LLM
{
    public class LMStudioBackend
    {
        private readonly HttpClient _http;
        private readonly string _baseUrl;
        private readonly string _model;

        public LMStudioBackend(string baseUrl, string model)
        {
            _baseUrl = baseUrl;
            _model = model;
            // LMStudio cold-load can take 30-60s on first request; warm cache ~5s.
            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(180) };
        }

        public async Task<string> CompleteAsync(string systemPrompt, string userPrompt)
        {
            var messages = new[]
            {
                new LLMMessage("system", systemPrompt),
                new LLMMessage("user", userPrompt)
            };
            // LMStudio does NOT support response_format.type=json_object.
            // It only supports json_schema or text. We rely on prompt engineering
            // + post-stripping ```json``` blocks.
            var payload = "{\"model\":\"" + _model + "\",\"messages\":" +
                          LLMJson.ArrayOf(messages) +
                          ",\"temperature\":0.3,\"max_tokens\":1500}";
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var resp = await _http.PostAsync($"{_baseUrl}/v1/chat/completions", content);
            resp.EnsureSuccessStatusCode();
            var body = await resp.Content.ReadAsStringAsync();
            var parsed = JsonUtility.FromJson<LLMResponse>(body);
            var raw = parsed.choices[0].message.content;
            // Strip ```json ... ``` markdown blocks that uncensored models often emit
            return LLMJson.StripMarkdownCodeBlock(raw);
        }
    }

    public static class LLMJson
    {
        public static string ArrayOf(LLMMessage[] msgs)
        {
            var sb = new StringBuilder("[");
            for (int i = 0; i < msgs.Length; i++)
            {
                if (i > 0) sb.Append(",");
                sb.Append("{\"role\":\"").Append(Escape(msgs[i].role)).Append("\",\"content\":\"").Append(Escape(msgs[i].content)).Append("\"}");
            }
            sb.Append("]");
            return sb.ToString();
        }

        public static string Escape(string s)
        {
            if (s == null) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
        }

        // Strip ```json ... ``` markdown blocks (some models emit JSON inside fences)
        public static string StripMarkdownCodeBlock(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return raw;
            var trimmed = raw.Trim();
            if (trimmed.StartsWith("```"))
            {
                int firstNewline = trimmed.IndexOf('\n');
                if (firstNewline > 0)
                {
                    int lastFence = trimmed.LastIndexOf("```");
                    if (lastFence > firstNewline)
                    {
                        return trimmed.Substring(firstNewline + 1, lastFence - firstNewline - 1).Trim();
                    }
                }
            }
            return trimmed;
        }
    }
}
