using System;

namespace creator_ui.Recipe
{
    // Barros sidecar returns this shape (see backend/barros_ai/models.py)
    // Different from LLM-direct RecipeData — Barros handles solver/positions internally.
    [Serializable]
    public class BarrosRecipeData
    {
        public string name;
        public string summary;
        public string shape;
        public float profit_factor;
        public BarrosIngredientData[] ingredients;
        public BarrosScoresData scores;
        public string rationale;
        public string[] warnings;
    }

    [Serializable]
    public class BarrosIngredientData
    {
        public string id;
        public string size;
        public float target_grams;
        public string distribution;
        public string note;
    }

    [Serializable]
    public class BarrosScoresData
    {
        public float taste;
        public float cost;
        public float profit;
        public float popularity;
        public float novelty;
        public float originality;
        public string source;
    }

    // Full /compose response envelope
    [Serializable]
    public class BarrosComposeResponse
    {
        public bool ok;
        public string message;
        public string provider;
        public BarrosRecipeData[] recipes;
        public string[] warnings;
    }

    // Convert Barros recipe to my RecipeData (for JsonExporter).
    // Position/rotation are filled with neutral defaults; the PC3 game will re-place ingredients.
    public static class BarrosRecipeAdapter
    {
        public static RecipeData ToRecipeData(BarrosRecipeData br)
        {
            var rd = new RecipeData
            {
                name = br.name,
                summary = br.summary,
                dough = new DoughSelectionData { size = "Large", shape = string.IsNullOrEmpty(br.shape) ? "Round" : br.shape },
                ingredients = new IngredientSelectionData[br.ingredients.Length]
            };
            for (int i = 0; i < br.ingredients.Length; i++)
            {
                rd.ingredients[i] = new IngredientSelectionData
                {
                    id = br.ingredients[i].id,
                    amount_g = br.ingredients[i].target_grams,
                    size = br.ingredients[i].size,
                    position = new[] { 0f, 0f, 0.95f + i * 0.01f },
                    rotation = new[] { 0f, 0f, 0f }
                };
            }
            if (br.scores != null)
            {
                rd.scores = new ScoresData
                {
                    taste = br.scores.taste,
                    cost_dollars = br.scores.cost,
                    profit_percent = br.scores.profit,
                    novelty = br.scores.novelty
                };
            }
            rd._meta = new MetaData { unknown_ingredient_count = 0 };
            return rd;
        }
    }
}
