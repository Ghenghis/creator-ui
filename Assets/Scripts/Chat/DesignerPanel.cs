using creator_ui.LLM;
using creator_ui.Recipe;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace creator_ui.Chat
{
    public class DesignerPanel : MonoBehaviour
    {
        public LLMClient llmClient;
        public BarrosBackend barros;
        public string[] catalogJsonArray;
        public NameDialog nameDialog;

        private RecipeData _currentRecipe;
        private string _mode = "build";
        private const string MODE_KEY = "designer";

        public void SetMode(string mode)
        {
            _mode = mode;
            HistoryStore.SaveMessage(MODE_KEY, "system", $"Mode: {mode}");
        }

        public async Task SendAsync(string userText)
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            var userLabel = root.Q<Label>("designer__msg-user");
            if (userLabel != null) userLabel.text = userText;

            string sysPrompt = _mode switch
            {
                "build" => "You are Barro's AI Pizza Designer. Help the user build a pizza step by step. Return Barro's Pizza JSON.",
                "surprise" => "Invent a surprising but balanced Barro's Pizza. Return Barro's Pizza JSON.",
                "improve" => "Improve the existing recipe by tweaking ingredients/amounts. Return Barro's Pizza JSON.",
                _ => ""
            };

            // Use Barros sidecar with history if available, fallback to direct LLM
            RecipeData recipe = null;
            if (barros != null && catalogJsonArray != null && catalogJsonArray.Length > 0)
            {
                try
                {
                    var history = HistoryStore.LoadRecent(MODE_KEY, 10);
                    var fullPrompt = $"{sysPrompt}\nUser: {userText}";
                    var respJson = await barros.ComposeWithCatalogAsync(fullPrompt, catalogJsonArray, "Medium");
                    var response = JsonUtility.FromJson<BarrosComposeResponse>(LLMJson.StripMarkdownCodeBlock(respJson));
                    if (response != null && response.recipes != null && response.recipes.Length > 0)
                    {
                        recipe = BarrosRecipeAdapter.ToRecipeData(response.recipes[0]);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[DesignerPanel] Barros failed: {ex.Message}, falling back to LLMClient");
                }
            }

            if (recipe == null)
            {
                var composer = new RecipeComposer(llmClient, barros);
                recipe = await composer.ComposeAsync(sysPrompt, userText);
            }

            HistoryStore.SaveMessage(MODE_KEY, "user", userText);
            HistoryStore.SaveMessage(MODE_KEY, "assistant", JsonUtility.ToJson(recipe));

            _currentRecipe = recipe;
            UpdateRecipeCard(_currentRecipe);
        }

        private void UpdateRecipeCard(RecipeData recipe)
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            var nameLabel = root.Q<Label>("designer__recipe-name");
            if (nameLabel != null) nameLabel.text = string.IsNullOrEmpty(recipe.name) ? "Recipe" : recipe.name;
            if (recipe.scores != null)
            {
                var tasteLabel = root.Q<Label>("designer__taste");
                var costLabel = root.Q<Label>("designer__cost");
                var popLabel = root.Q<Label>("designer__pop");
                if (tasteLabel != null) tasteLabel.text = ((int)recipe.scores.taste).ToString();
                if (costLabel != null) costLabel.text = ((int)(recipe.scores.cost_dollars * 100)).ToString();
                if (popLabel != null) popLabel.text = ((int)recipe.scores.novelty).ToString();
            }
        }

        public void OnApplyClicked()
        {
            if (_currentRecipe == null) return;
            nameDialog?.Show(_currentRecipe);
        }

        public void ClearHistory() => HistoryStore.Clear(MODE_KEY);
    }
}
