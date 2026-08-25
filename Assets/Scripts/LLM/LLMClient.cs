using System;
using System.Threading.Tasks;

namespace creator_ui.LLM
{
    public class LLMClient
    {
        private readonly BarrosBackend _barros;
        private readonly LMStudioBackend _lmstudio;
        private readonly OpenAIBackend _openai;

        public enum Source { Barros, LMStudio, OpenAI }

        public LLMClient(BarrosBackend barros, LMStudioBackend lmstudio, OpenAIBackend openai)
        {
            _barros = barros;
            _lmstudio = lmstudio;
            _openai = openai;
        }

        public static string MaskKey(string key)
        {
            if (string.IsNullOrEmpty(key) || key.Length < 8) return "****";
            return key.Substring(0, 4) + "..." + key.Substring(key.Length - 4);
        }

        public async Task<string> CompleteAsync(string systemPrompt, string userPrompt)
        {
            // Preferred path: Barros sidecar (handles orchestration + provider routing)
            try
            {
                return await _barros.ComposeAsync(systemPrompt, userPrompt);
            }
            catch (Exception barrosEx)
            {
                UnityEngine.Debug.LogWarning($"[LLMClient] Barros sidecar failed: {barrosEx.Message}. Falling back to LMStudio.");
                try
                {
                    return await _lmstudio.CompleteAsync(systemPrompt, userPrompt);
                }
                catch (Exception lmEx)
                {
                    UnityEngine.Debug.LogWarning($"[LLMClient] LMStudio failed: {lmEx.Message}. Falling back to OpenAI.");
                    try
                    {
                        return await _openai.CompleteAsync(systemPrompt, userPrompt);
                    }
                    catch (Exception openaiEx)
                    {
                        throw new Exception(
                            $"No LLM backend available. Barros: {barrosEx.Message}. LMStudio: {lmEx.Message}. OpenAI: {openaiEx.Message}");
                    }
                }
            }
        }

        // Chat-mode entry point — uses Barros /chat (preserves conversation history)
        public async Task<string> ChatAsync(string systemPrompt, string userPrompt, System.Collections.Generic.List<(string role, string content)> history)
        {
            try
            {
                return await _barros.ChatAsync(systemPrompt, userPrompt, history);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[LLMClient] Barros /chat failed: {ex.Message}. Falling back to LMStudio.");
                return await _lmstudio.CompleteAsync(systemPrompt, userPrompt);
            }
        }
    }
}
