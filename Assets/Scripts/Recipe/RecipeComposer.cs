using creator_ui.LLM;
using System.Threading.Tasks;
using UnityEngine;

namespace creator_ui.Recipe
{
    public class RecipeComposer
    {
        private readonly LLMClient _client;
        private readonly BarrosBackend _barros;
        private readonly CatalogData _catalog;

        public RecipeComposer(LLMClient client, BarrosBackend barros = null)
        {
            _client = client;
            _barros = barros ?? new BarrosBackend();
            _catalog = IngredientCatalog.Load();
        }

        // Primary path: Barros sidecar (handles solver, scoring, ingredient selection)
        public async Task<RecipeData> ComposeAsync(string systemPrompt, string userPrompt, string heat = "Medium")
        {
            // Try Barros /compose first (with full PC3 catalog) — produces PC3-valid recipe
            try
            {
                var catalogJsonArray = SerializeCatalogForBarros(_catalog);
                var respJson = await _barros.ComposeWithCatalogAsync(userPrompt, catalogJsonArray, heat);
                var response = JsonUtility.FromJson<BarrosComposeResponse>(LLMJson.StripMarkdownCodeBlock(respJson));
                if (response != null && response.recipes != null && response.recipes.Length > 0)
                {
                    return BarrosRecipeAdapter.ToRecipeData(response.recipes[0]);
                }
                throw new System.Exception("Barros returned empty recipes array");
            }
            catch (System.Exception barrosEx)
            {
                Debug.LogWarning($"[RecipeComposer] Barros failed: {barrosEx.Message}. Falling back to LLM direct.");
            }

            // Fallback: LLM direct (LMStudio or OpenAI)
            var llmJson = await _client.CompleteAsync(systemPrompt, userPrompt);
            RecipeData recipe;
            try
            {
                recipe = JsonUtility.FromJson<RecipeData>(LLMJson.StripMarkdownCodeBlock(llmJson));
                if (recipe == null) throw new System.Exception("JsonUtility returned null");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[RecipeComposer] LLM returned invalid JSON: {ex.Message}");
                throw;
            }

            int unknownCount = 0;
            if (recipe.ingredients != null)
            {
                foreach (var ing in recipe.ingredients)
                {
                    if (!IngredientCatalog.ContainsId(_catalog, ing.id))
                    {
                        Debug.LogWarning($"[RecipeComposer] Unknown ingredient '{ing.id}' - keeping but flagging");
                        unknownCount++;
                    }
                }
            }
            recipe.scores = ScoringEngine.Compute(recipe, _catalog);
            recipe._meta = new MetaData { unknown_ingredient_count = unknownCount };
            return recipe;
        }

        // Serialize catalog to JSON array string for Barros /compose payload.
        // Barros expects: [{"id","name","type_id","sizes":[{size,grams,cost}]}]
        private static string[] SerializeCatalogForBarros(CatalogData catalog)
        {
            var arr = new System.Collections.Generic.List<string>();
            if (catalog?.ingredients == null) return arr.ToArray();
            foreach (var ing in catalog.ingredients)
            {
                var sb = new System.Text.StringBuilder();
                sb.Append("{\"id\":\"").Append(EscapeForBarros(ing.id)).Append("\",\"name\":\"").Append(EscapeForBarros(ing.name)).Append("\",\"type_id\":\"").Append(EscapeForBarros(ing.type)).Append("\",\"sizes\":[");
                if (ing.allowed_sizes != null)
                {
                    int idx = 0;
                    foreach (var sz in ing.allowed_sizes)
                    {
                        if (idx > 0) sb.Append(",");
                        float grams = sz == "Large" ? ing.max_g : sz == "Small" ? 2f : (ing.min_g + ing.max_g) / 2f;
                        float cost = (grams / 100f) * ing.base_price;
                        sb.Append("{\"size\":\"").Append(sz).Append("\",\"grams\":").Append(grams.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)).Append(",\"cost\":").Append(cost.ToString("F4", System.Globalization.CultureInfo.InvariantCulture)).Append("}");
                        idx++;
                    }
                }
                sb.Append("]}");
                arr.Add(sb.ToString());
            }
            return arr.ToArray();
        }

        private static string EscapeForBarros(string s)
        {
            if (s == null) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
