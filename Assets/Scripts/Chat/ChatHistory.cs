using creator_ui.LLM;
using creator_ui.Recipe;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace creator_ui.Chat
{
    // Static helpers for chat panels — wraps HistoryStore + LLMClient calls
    public static class ChatHistory
    {
        public static async Task<string> ComposeWithHistoryAsync(
            LLMClient client,
            BarrosBackend barros,
            string mode,
            string systemPrompt,
            string userPrompt,
            string[] catalogJsonArray = null,
            string heat = "Medium")
        {
            // Save user turn
            HistoryStore.SaveMessage(mode, "user", userPrompt);

            // Load recent history (last 10 turns)
            var history = HistoryStore.LoadRecent(mode, 10);

            // Call Barros /compose with history + catalog
            string response;
            try
            {
                response = await ComposeViaBarros(barros, systemPrompt, userPrompt, history, catalogJsonArray, heat);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[ChatHistory] Barros failed: {ex.Message}, falling back to LLMClient");
                response = await client.ChatAsync(systemPrompt, userPrompt, history);
            }

            // Save assistant turn
            HistoryStore.SaveMessage(mode, "assistant", response);
            return response;
        }

        private static async Task<string> ComposeViaBarros(
            BarrosBackend barros,
            string systemPrompt,
            string userPrompt,
            List<(string role, string content)> history,
            string[] catalogJsonArray,
            string heat)
        {
            // Barros doesn't currently take history in /compose; fall back to plain call.
            // (Future: extend BarrosBackend to forward history.)
            if (catalogJsonArray != null && catalogJsonArray.Length > 0)
            {
                return await barros.ComposeWithCatalogAsync(userPrompt, catalogJsonArray, heat);
            }
            return await barros.ChatAsync(systemPrompt, userPrompt, history);
        }
    }
}
