using NUnit.Framework;
using creator_ui.Recipe;
using UnityEngine;

namespace creator_ui.tests.EditMode
{
    public class ScoringEngineTests
    {
        [Test]
        public void Taste_WeightedAverage_ReturnsCorrectValue()
        {
            var recipe = new RecipeData
            {
                ingredients = new[]
                {
                    new IngredientSelectionData { id = "PizzaSauce", amount_g = 100f },
                    new IngredientSelectionData { id = "Mozzarella", amount_g = 50f }
                }
            };
            var catalog = new CatalogData
            {
                ingredients = new[]
                {
                    new IngredientData { id = "PizzaSauce", taste_rating = 60, base_price = 0.12f },
                    new IngredientData { id = "Mozzarella", taste_rating = 80, base_price = 0.15f }
                }
            };
            var scores = ScoringEngine.Compute(recipe, catalog);
            // weighted avg: (60*100 + 80*50) / 150 = 66.67
            Assert.That(scores.taste, Is.EqualTo(66.7f).Within(0.1f));
        }

        [Test]
        public void Cost_PC3Formula_MatchesIngredientModelLine402()
        {
            var recipe = new RecipeData
            {
                ingredients = new[]
                {
                    new IngredientSelectionData { id = "PizzaSauce", amount_g = 100f }
                }
            };
            var catalog = new CatalogData
            {
                ingredients = new[]
                {
                    new IngredientData { id = "PizzaSauce", taste_rating = 60, base_price = 0.12f }
                }
            };
            var scores = ScoringEngine.Compute(recipe, catalog);
            // PC3: Price = Amount / 100 * BasePrice = 100/100 * 0.12 = 0.12
            Assert.That(scores.cost_dollars, Is.EqualTo(0.12f).Within(0.001f));
        }

        [Test]
        public void Cost_UnknownIngredient_Skipped()
        {
            var recipe = new RecipeData
            {
                ingredients = new[]
                {
                    new IngredientSelectionData { id = "UnknownIngredient", amount_g = 100f }
                }
            };
            var catalog = new CatalogData { ingredients = new IngredientData[0] };
            var scores = ScoringEngine.Compute(recipe, catalog);
            Assert.That(scores.cost_dollars, Is.EqualTo(0).Within(0.001f));
        }

        [Test]
        public void Cost_PC3Formula_At200g()
        {
            // PC3: 200g * (1/100) * base_price = 2 * base_price
            var recipe = new RecipeData
            {
                ingredients = new[]
                {
                    new IngredientSelectionData { id = "Mozzarella", amount_g = 200f }
                }
            };
            var catalog = new CatalogData
            {
                ingredients = new[]
                {
                    new IngredientData { id = "Mozzarella", taste_rating = 70, base_price = 0.10f }
                }
            };
            var scores = ScoringEngine.Compute(recipe, catalog);
            // 200/100 * 0.10 = 0.20
            Assert.That(scores.cost_dollars, Is.EqualTo(0.20f).Within(0.001f));
        }
    }
}
