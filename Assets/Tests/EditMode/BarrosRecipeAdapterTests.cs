using NUnit.Framework;
using creator_ui.Recipe;
using UnityEngine;

namespace creator_ui.tests.EditMode
{
    public class BarrosRecipeAdapterTests
    {
        [Test]
        public void ToRecipeData_BasicConversion_ProducesValidRecipeData()
        {
            var br = new BarrosRecipeData
            {
                name = "Margherita",
                summary = "Classic",
                shape = "Round",
                profit_factor = 1.5f,
                ingredients = new[]
                {
                    new BarrosIngredientData { id = "Mozzarella", size = "Medium", target_grams = 162.5f, distribution = "even" },
                    new BarrosIngredientData { id = "Tomato", size = "Large", target_grams = 100f, distribution = "ring" }
                },
                scores = new BarrosScoresData { taste = 77f, cost = 1.03f, profit = 42.9f, novelty = 75f }
            };
            var rd = BarrosRecipeAdapter.ToRecipeData(br);
            Assert.AreEqual("Margherita", rd.name);
            Assert.AreEqual("Round", rd.dough.shape);
            Assert.AreEqual(2, rd.ingredients.Length);
            Assert.AreEqual("Mozzarella", rd.ingredients[0].id);
            Assert.AreEqual("Medium", rd.ingredients[0].size);
            Assert.AreEqual(162.5f, rd.ingredients[0].amount_g);
            Assert.AreEqual(77f, rd.scores.taste);
        }

        [Test]
        public void ToRecipeData_EmptyIngredients_ProducesEmptyArray()
        {
            var br = new BarrosRecipeData
            {
                name = "Empty",
                shape = "Round",
                ingredients = new BarrosIngredientData[0],
                scores = new BarrosScoresData()
            };
            var rd = BarrosRecipeAdapter.ToRecipeData(br);
            Assert.AreEqual("Empty", rd.name);
            Assert.AreEqual(0, rd.ingredients.Length);
        }

        [Test]
        public void ToRecipeData_NullShape_DefaultsToRound()
        {
            var br = new BarrosRecipeData
            {
                name = "X",
                shape = "",
                ingredients = new BarrosIngredientData[0],
                scores = new BarrosScoresData()
            };
            var rd = BarrosRecipeAdapter.ToRecipeData(br);
            Assert.AreEqual("Round", rd.dough.shape);
        }

        [Test]
        public void ToRecipeData_IncrementsYPositionPerIngredient()
        {
            var br = new BarrosRecipeData
            {
                name = "Layered",
                shape = "Round",
                ingredients = new[]
                {
                    new BarrosIngredientData { id = "A", size = "Large", target_grams = 100f },
                    new BarrosIngredientData { id = "B", size = "Medium", target_grams = 50f },
                    new BarrosIngredientData { id = "C", size = "Small", target_grams = 10f }
                },
                scores = new BarrosScoresData()
            };
            var rd = BarrosRecipeAdapter.ToRecipeData(br);
            Assert.AreEqual(0.95f, rd.ingredients[0].position[2], 0.001f);
            Assert.AreEqual(0.96f, rd.ingredients[1].position[2], 0.001f);
            Assert.AreEqual(0.97f, rd.ingredients[2].position[2], 0.001f);
        }

        [Test]
        public void BarrosComposeResponse_ParsesCorrectly()
        {
            var json = @"{
                ""ok"": true,
                ""message"": ""OK"",
                ""provider"": ""lmstudio"",
                ""recipes"": [{""name"":""Test"", ""shape"":""Round"", ""ingredients"":[], ""scores"":{}}],
                ""warnings"": []
            }";
            var resp = JsonUtility.FromJson<BarrosComposeResponse>(json);
            Assert.IsTrue(resp.ok);
            Assert.AreEqual("lmstudio", resp.provider);
            Assert.AreEqual(1, resp.recipes.Length);
            Assert.AreEqual("Test", resp.recipes[0].name);
        }

        [Test]
        public void BarrosIngredientData_RoundTripSerialization()
        {
            var orig = new BarrosIngredientData { id = "X", size = "Large", target_grams = 200f, distribution = "spiral", note = "extra" };
            var json = JsonUtility.ToJson(orig);
            var copy = JsonUtility.FromJson<BarrosIngredientData>(json);
            Assert.AreEqual(orig.id, copy.id);
            Assert.AreEqual(orig.size, copy.size);
            Assert.AreEqual(orig.target_grams, copy.target_grams);
            Assert.AreEqual(orig.distribution, copy.distribution);
            Assert.AreEqual(orig.note, copy.note);
        }
    }
}
