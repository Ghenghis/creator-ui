using creator_ui.LLM;
using creator_ui.Recipe;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace creator_ui.Chat
{
    // Crew panel: 4-persona design crew (Flavor Chef, Cost Manager, Customer Scout, Creative Director).
    // Each persona contributes via Barros /chat. Then Lead synthesizes final recipe via /compose.
    public class CrewPanel : MonoBehaviour
    {
        public LLMClient llmClient;
        public BarrosBackend barros;
        public NameDialog nameDialog;
        public string[] catalogJsonArray;

        private RecipeData _currentRecipe;
        private readonly List<(string agent, string message, bool warning)> _discussion = new();

        private const string FLAVOR_SYS = "You are Flavor Chef for Barro's Pizza. Suggest ONE bold, craveable ingredient combination. Reply in 1 short sentence.";
        private const string COST_SYS = "You are Cost Manager. Flag ONE cost concern about a proposed pizza. Reply in 1 short sentence.";
        private const string CUSTOMER_SYS = "You are Customer Scout. Note ONE current trend relevant to the pizza. Reply in 1 short sentence.";
        private const string CREATIVE_SYS = "You are Creative Director. Suggest a memorable pizza NAME and one signature element. Reply in 1 short sentence.";
        private const string LEAD_SYS = "You are Crew Lead for Barro's Pizza. Combine the 4 agent ideas into one final pizza recipe. Return PC3 PizzaModel-shaped JSON with: {name, dough:{size,shape}, ingredients:[{id, amount_g, size}]}. Sizes: Small/Medium/Large. Ingredient IDs MUST be from the catalog.";

        public async Task ComposeAsync(string theme)
        {
            _discussion.Clear();

            // Step 1: Gather 4 perspectives in parallel via Barros /chat
            var tasks = new List<Task<string>>
            {
                barros.ChatAsync(FLAVOR_SYS, $"Theme: {theme}", null),
                barros.ChatAsync(COST_SYS, $"Theme: {theme}", null),
                barros.ChatAsync(CUSTOMER_SYS, $"Theme: {theme}", null),
                barros.ChatAsync(CREATIVE_SYS, $"Theme: {theme}", null)
            };
            var results = await Task.WhenAll(tasks);
            _discussion.Add(("Flavor Chef", results[0], false));
            _discussion.Add(("Cost Manager", results[1], true));
            _discussion.Add(("Customer Scout", results[2], false));
            _discussion.Add(("Creative Director", results[3], false));
            UpdateDiscussionLog();

            // Step 2: Lead synthesizes final recipe via Barros /compose (with catalog)
            var leadPrompt = $"Theme: {theme}\nFlavor Chef: {results[0]}\nCost Manager: {results[1]}\nCustomer Scout: {results[2]}\nCreative Director: {results[3]}";
            string respJson;
            try
            {
                respJson = await barros.ComposeWithCatalogAsync(leadPrompt, catalogJsonArray ?? new string[0], "Medium");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[CrewPanel] Barros /compose failed: {ex.Message}, falling back to LLMClient");
                respJson = await llmClient.CompleteAsync(LEAD_SYS, leadPrompt);
            }

            // Step 3: Parse Barros response, convert to RecipeData
            try
            {
                var response = JsonUtility.FromJson<BarrosComposeResponse>(LLMJson.StripMarkdownCodeBlock(respJson));
                if (response != null && response.recipes != null && response.recipes.Length > 0)
                {
                    _currentRecipe = BarrosRecipeAdapter.ToRecipeData(response.recipes[0]);
                    UpdateConsensus(_currentRecipe);
                    return;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[CrewPanel] Barros parse failed: {ex.Message}, trying LLMClient recipe shape");
            }
            // Fallback: LLM direct
            var composer = new RecipeComposer(llmClient, barros);
            _currentRecipe = await composer.ComposeAsync(LEAD_SYS, leadPrompt);
            UpdateConsensus(_currentRecipe);
        }

        private void UpdateDiscussionLog()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            var log = root.Q<ScrollView>("crew__discussion-log");
            if (log == null) return;
            log.Clear();
            foreach (var (agent, msg, warn) in _discussion)
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.marginBottom = 4;
                var name = new Label(agent);
                name.style.width = 120;
                name.style.unityFontStyleAndWeight = FontStyle.Bold;
                if (warn) name.style.color = Color.red;
                var text = new Label(msg);
                text.style.flexGrow = 1;
                text.style.whiteSpace = WhiteSpace.Normal;
                row.Add(name);
                row.Add(text);
                log.Add(row);
            }
        }

        private void UpdateConsensus(RecipeData recipe)
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            var nameLabel = root.Q<Label>("crew__pizza-name");
            if (nameLabel != null) nameLabel.text = string.IsNullOrEmpty(recipe.name) ? "Proposed" : recipe.name;
            if (recipe.scores == null) return;
            SetBar(root, "bar-flavor", "bar-flavor-val", recipe.scores.taste);
            SetBar(root, "bar-profit", "bar-profit-val", recipe.scores.profit_percent);
            SetBar(root, "bar-popularity", "bar-popularity-val", 75);
            SetBar(root, "bar-originality", "bar-originality-val", recipe.scores.novelty);
        }

        private void SetBar(VisualElement root, string barName, string valName, float value)
        {
            var bar = root.Q<VisualElement>(barName);
            if (bar != null) bar.style.width = new Length(Mathf.Min(100, value), LengthUnit.Percent);
            var valLabel = root.Q<Label>(valName);
            if (valLabel != null) valLabel.text = ((int)value).ToString();
        }
    }
}
