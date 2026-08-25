using creator_ui.LLM;
using creator_ui.Recipe;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace creator_ui.Chat
{
    public class LabPanel : MonoBehaviour
    {
        public LLMClient llmClient;
        public BarrosBackend barros;
        public string[] catalogJsonArray;
        public NameDialog nameDialog;

        private readonly List<RecipeData> _recipes = new();
        private RecipeData _selected;
        private const string MODE_KEY = "lab";

        public async Task GenerateBatchAsync(string[] tags)
        {
            var tagStr = string.Join(", ", tags);
            var tasks = new List<Task<RecipeData>>();
            for (int i = 0; i < 3; i++)
            {
                tasks.Add(GenerateOneAsync($"Tags: {tagStr}. Variant {i + 1}."));
            }
            var results = await Task.WhenAll(tasks);
            _recipes.Clear();
            _recipes.AddRange(results);
            _recipes.Sort((a, b) => b.scores.taste.CompareTo(a.scores.taste));
            HistoryStore.SaveMessage(MODE_KEY, "user", $"Tags: {tagStr}");
            HistoryStore.SaveMessage(MODE_KEY, "assistant", $"Generated {_recipes.Count} recipes");
            RenderRecipeCards();
        }

        private async Task<RecipeData> GenerateOneAsync(string prompt)
        {
            // Prefer Barros with catalog
            if (barros != null && catalogJsonArray != null && catalogJsonArray.Length > 0)
            {
                try
                {
                    var respJson = await barros.ComposeWithCatalogAsync(prompt, catalogJsonArray, "Medium");
                    var response = JsonUtility.FromJson<BarrosComposeResponse>(LLMJson.StripMarkdownCodeBlock(respJson));
                    if (response != null && response.recipes != null && response.recipes.Length > 0)
                    {
                        return BarrosRecipeAdapter.ToRecipeData(response.recipes[0]);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[LabPanel] Barros failed: {ex.Message}, falling back to RecipeComposer");
                }
            }
            var composer = new RecipeComposer(llmClient, barros);
            return await composer.ComposeAsync(
                "Experimental pizza designer. Return Barro's Pizza JSON with 5-8 ingredients.",
                prompt);
        }

        private void RenderRecipeCards()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            var scroll = root.Q<ScrollView>("lab__recipes");
            if (scroll == null) return;
            scroll.Clear();
            foreach (var recipe in _recipes)
            {
                var card = new VisualElement();
                card.AddToClassList("card-recipe-card");
                var thumb = new VisualElement();
                thumb.AddToClassList("card-recipe-card__thumb");
                card.Add(thumb);
                var body = new VisualElement();
                body.AddToClassList("card-recipe-card__body");
                var name = new Label(string.IsNullOrEmpty(recipe.name) ? "Recipe" : recipe.name);
                name.AddToClassList("card-recipe-card__name");
                body.Add(name);
                if (recipe.scores != null)
                {
                    AddScoreRow(body, "Taste", recipe.scores.taste);
                    AddScoreRow(body, "Cost", recipe.scores.cost_dollars);
                    AddScoreRow(body, "Profit", recipe.scores.profit_percent);
                    AddScoreRow(body, "Novelty", recipe.scores.novelty);
                }
                card.Add(body);
                var actions = new VisualElement();
                actions.AddToClassList("card-recipe-card__actions");
                var previewBtn = new Button { text = "Preview" };
                previewBtn.AddToClassList("btn");
                previewBtn.AddToClassList("btn-secondary");
                var useBtn = new Button { text = "Use" };
                useBtn.AddToClassList("btn");
                useBtn.AddToClassList("btn-primary");
                var capturedRecipe = recipe;
                useBtn.clicked += () => { _selected = capturedRecipe; nameDialog?.Show(capturedRecipe); };
                actions.Add(previewBtn);
                actions.Add(useBtn);
                card.Add(actions);
                scroll.Add(card);
            }
        }

        private void AddScoreRow(VisualElement parent, string label, float value)
        {
            var row = new VisualElement();
            row.AddToClassList("bar-row");
            var lab = new Label(label);
            lab.AddToClassList("bar-row__label");
            row.Add(lab);
            var track = new VisualElement();
            track.AddToClassList("bar-row__track");
            var fill = new VisualElement();
            fill.AddToClassList("bar__fill");
            fill.style.width = new Length(Mathf.Min(100, value), LengthUnit.Percent);
            track.Add(fill);
            row.Add(track);
            var val = new Label(((int)value).ToString());
            val.AddToClassList("bar-row__value");
            row.Add(val);
            parent.Add(row);
        }

        public void ClearHistory() => HistoryStore.Clear(MODE_KEY);
    }
}
