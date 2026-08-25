using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace creator_ui.Recipe
{
    public static class JsonExporter
    {
        // PC3 IngredientSize enum: Large=0, Medium=1, Small=2 (IngredientModel.cs:12-17)
        private static int SizeToInt(string size)
        {
            if (size == "Large") return 0;
            if (size == "Small") return 2;
            return 1;  // Medium default
        }

        public static void WriteFinal(RecipeData recipe, string outputPath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"ID\": \"{EscapeString(recipe?.name ?? "recipe")}-{DateTime.UtcNow.Ticks}\",");
            sb.AppendLine($"  \"Name\": \"{EscapeString(recipe?.name ?? "Pizza")}\",");
            if (recipe?.dough != null)
            {
                sb.AppendLine($"  \"DoughSize\": \"{EscapeString(recipe.dough.size ?? "Large")}\",");
                sb.AppendLine($"  \"DoughShape\": \"{EscapeString(recipe.dough.shape ?? "Round")}\",");
            }
            sb.AppendLine("  \"Ingredients\": [");
            if (recipe?.ingredients != null)
            {
                for (int i = 0; i < recipe.ingredients.Length; i++)
                {
                    var ing = recipe.ingredients[i];
                    float px = ing.position != null && ing.position.Length > 0 ? ing.position[0] : 0;
                    float py = ing.position != null && ing.position.Length > 1 ? ing.position[1] : 0;
                    float pz = ing.position != null && ing.position.Length > 2 ? ing.position[2] : 0.95f;
                    float rx = ing.rotation != null && ing.rotation.Length > 0 ? ing.rotation[0] : 0;
                    float ry = ing.rotation != null && ing.rotation.Length > 1 ? ing.rotation[1] : 0;
                    float rz = ing.rotation != null && ing.rotation.Length > 2 ? ing.rotation[2] : 0;
                    sb.AppendLine($"    {{\"IngredientID\":\"{ing.id}\",\"Rotation\":{{\"x\":{rx.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"y\":{ry.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"z\":{rz.ToString(System.Globalization.CultureInfo.InvariantCulture)}}},\"Position\":{{\"x\":{px.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"y\":{py.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"z\":{pz.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}");
                    sb.AppendLine($",\"Size\":{SizeToInt(ing.size)}");
                    sb.AppendLine($",\"AmountG\":{ing.amount_g.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
                    sb.AppendLine($",\"DisplaySize\":\"{EscapeString(ing.size ?? "Medium")}\"");
                    sb.AppendLine("}");
                }
            }
            sb.AppendLine("  ],");
            sb.AppendLine("  \"DoughPositions\": [{\"x\":0,\"y\":0,\"z\":0}],");
            sb.AppendLine($"  \"ProfitFactor\": {(recipe != null && recipe.profit_factor > 0 ? recipe.profit_factor : 1.5f).ToString(System.Globalization.CultureInfo.InvariantCulture)},");
            if (recipe?.scores != null)
            {
                sb.AppendLine("  \"Scores\": {");
                sb.AppendLine($"    \"taste\": {recipe.scores.taste.ToString(System.Globalization.CultureInfo.InvariantCulture)},");
                sb.AppendLine($"    \"cost_dollars\": {recipe.scores.cost_dollars.ToString(System.Globalization.CultureInfo.InvariantCulture)},");
                sb.AppendLine($"    \"profit_percent\": {recipe.scores.profit_percent.ToString(System.Globalization.CultureInfo.InvariantCulture)},");
                sb.AppendLine($"    \"novelty\": {recipe.scores.novelty.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
                sb.AppendLine("  },");
            }
            sb.AppendLine($"  \"Summary\": \"{EscapeString(recipe?.summary ?? "")}\",");
            sb.AppendLine("  \"Owner\": null,");
            sb.AppendLine("  \"Texture\": \"\"");
            sb.AppendLine("}");
            File.WriteAllText(outputPath, sb.ToString());
            Debug.Log($"[JsonExporter] Wrote {outputPath}");
        }

        private static string EscapeString(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
        }

        public static void WriteRecipe(RecipeData recipe, string outputPath)
        {
            // Strip _meta before writing (it's internal annotation)
            var meta = recipe._meta;
            recipe._meta = null;
            var json = JsonUtility.ToJson(recipe, true);
            recipe._meta = meta;
            File.WriteAllText(outputPath, json);
            Debug.Log($"[JsonExporter] Wrote recipe {outputPath}");
        }
    }
}
