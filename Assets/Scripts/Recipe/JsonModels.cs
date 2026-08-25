using System;

namespace creator_ui.Recipe
{
    // Plain serializable models for catalog.json (PC3 data model)
    [Serializable]
    public class CatalogData
    {
        public IngredientData[] ingredients;
        public DoughData dough;
    }

    [Serializable]
    public class IngredientData
    {
        public string id;
        public string type;
        public string name;
        public float min_g;
        public float max_g;
        public float base_price;
        public string[] allowed_sizes;
        public float taste_rating;
    }

    [Serializable]
    public class DoughData
    {
        public string[] shapes;
        public string[] sizes;
        public float default_radius_units;
    }

    // RecipeData for LLM output + intermediate recipe
    [Serializable]
    public class RecipeData
    {
        public string name;
        public string summary;
        public string prompt;
        public DoughSelectionData dough;
        public IngredientSelectionData[] ingredients;
        public ScoresData scores;
        public MetaData _meta;
        public float profit_factor = 1.5f;
    }

    [Serializable]
    public class DoughSelectionData
    {
        public string size;
        public string shape;
    }

    [Serializable]
    public class IngredientSelectionData
    {
        public string id;
        public float amount_g;
        public float[] position;
        public float[] rotation;
        public string size;
    }

    [Serializable]
    public class ScoresData
    {
        public float taste;
        public float cost_dollars;
        public float profit_percent;
        public float novelty;
    }

    [Serializable]
    public class MetaData
    {
        public int unknown_ingredient_count;
    }
}
