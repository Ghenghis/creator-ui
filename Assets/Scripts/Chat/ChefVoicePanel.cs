using creator_ui.LLM;
using creator_ui.Recipe;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace creator_ui.Chat
{
    public class ChefVoicePanel : MonoBehaviour
    {
        public LLMClient llmClient;
        public BarrosBackend barros;
        public string[] catalogJsonArray;
        public NameDialog nameDialog;

        private const string SYSTEM_PROMPT =
            @"You are Chef AI for Barro's Pizza Creator. Help the user design a pizza. Return Barro's Pizza JSON with fields: name, dough:{size,shape}, ingredients:[{id, amount_g, position:[x,y,z], rotation:[x,y,z], size}]. Ingredient IDs MUST be from the catalog.";
        private const string MODE_KEY = "chef-voice";

        private RecipeData _currentRecipe;
        private bool _isComposing;

        private void OnEnable()
        {
            var doc = GetComponent<UIDocument>();
            if (doc == null) return;
            var root = doc.rootVisualElement;
            var applyBtn = root.Q<Button>("chef-voice__apply");
            if (applyBtn != null) applyBtn.clicked += OnApplyClicked;
            var mildBtn = root.Q<Button>("heat-mild");
            var medBtn = root.Q<Button>("heat-medium");
            var hotBtn = root.Q<Button>("heat-hot");
            if (mildBtn != null) mildBtn.clicked += () => SetHeat("Mild");
            if (medBtn != null) medBtn.clicked += () => SetHeat("Medium");
            if (hotBtn != null) hotBtn.clicked += () => SetHeat("Hot");
        }

        public async Task ComposeAsync(string userText)
        {
            if (_isComposing) return;
            _isComposing = true;
            try
            {
                HistoryStore.SaveMessage(MODE_KEY, "user", userText);
                var root = GetComponent<UIDocument>().rootVisualElement;
                var userLabel = root.Q<Label>("chef-voice__msg-user-text");
                if (userLabel != null) userLabel.text = userText;

                RecipeData recipe = null;
                if (barros != null && catalogJsonArray != null && catalogJsonArray.Length > 0)
                {
                    try
                    {
                        var respJson = await barros.ComposeWithCatalogAsync(userText, catalogJsonArray, "Medium");
                        var response = JsonUtility.FromJson<BarrosComposeResponse>(LLMJson.StripMarkdownCodeBlock(respJson));
                        if (response != null && response.recipes != null && response.recipes.Length > 0)
                        {
                            recipe = BarrosRecipeAdapter.ToRecipeData(response.recipes[0]);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[ChefVoicePanel] Barros failed: {ex.Message}, falling back to LLMClient");
                    }
                }
                if (recipe == null)
                {
                    var composer = new RecipeComposer(llmClient, barros);
                    recipe = await composer.ComposeAsync(SYSTEM_PROMPT, userText);
                }
                HistoryStore.SaveMessage(MODE_KEY, "assistant", JsonUtility.ToJson(recipe));
                _currentRecipe = recipe;
                int ingCount = _currentRecipe.ingredients?.Length ?? 0;
                var aiLabel = root.Q<Label>("chef-voice__msg-ai-text");
                if (aiLabel != null) aiLabel.text = $"I can build that. Medium heat or hot? ({ingCount} ingredients)";
                UpdateRecipeCard(_currentRecipe);
            }
            finally { _isComposing = false; }
        }

        private void UpdateRecipeCard(RecipeData recipe)
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            var nameLabel = root.Q<Label>("chef-voice__recipe-name");
            if (nameLabel != null) nameLabel.text = string.IsNullOrEmpty(recipe.name) ? "Recipe" : recipe.name;
            var ingContainer = root.Q<VisualElement>("chef-voice__recipe-ingredients");
            if (ingContainer != null)
            {
                ingContainer.Clear();
                if (recipe.ingredients != null)
                {
                    foreach (var ing in recipe.ingredients)
                    {
                        var row = new Label($"{ing.id} -- {ing.amount_g:0.#}g");
                        row.style.fontSize = 13;
                        ingContainer.Add(row);
                    }
                }
            }
            if (recipe.scores != null)
            {
                var costLabel = root.Q<Label>("stat-cost");
                var priceLabel = root.Q<Label>("stat-price");
                var profitLabel = root.Q<Label>("stat-profit");
                float cost = recipe.scores.cost_dollars;
                if (costLabel != null) costLabel.text = $"Cost ${cost:0.00}";
                if (priceLabel != null) priceLabel.text = $"Price ${cost * 1.5f:0.00}";
                if (profitLabel != null) profitLabel.text = $"Profit {recipe.scores.profit_percent:0.#}%";
            }
        }

        private void SetHeat(string heat)
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            var mild = root.Q<Button>("heat-mild");
            var med = root.Q<Button>("heat-medium");
            var hot = root.Q<Button>("heat-hot");
            if (mild != null) mild.EnableInClassList("btn-chip--active", heat == "Mild");
            if (med != null) med.EnableInClassList("btn-chip--active", heat == "Medium");
            if (hot != null) hot.EnableInClassList("btn-chip--active", heat == "Hot");
            HistoryStore.SaveMessage(MODE_KEY, "user", $"[heat] {heat}");
        }

        private void OnApplyClicked()
        {
            if (_currentRecipe == null) return;
            nameDialog?.Show(_currentRecipe);
        }

        public void ClearHistory() => HistoryStore.Clear(MODE_KEY);
    }
}
