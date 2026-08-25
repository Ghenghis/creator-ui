using NUnit.Framework;
using creator_ui.Recipe;
using System.IO;

namespace creator_ui.tests.EditMode
{
    public class JsonExporterTests
    {
        [Test]
        public void WriteFinal_ProducesValidPC3DataContractShape()
        {
            var recipe = new RecipeData
            {
                name = "Test Pizza",
                dough = new DoughSelectionData { size = "Large", shape = "Round" },
                ingredients = new[]
                {
                    new IngredientSelectionData
                    {
                        id = "PizzaSauce",
                        amount_g = 100f,
                        position = new[] { 0f, 0f, 0.95f },
                        rotation = new[] { 0f, 0f, 0f },
                        size = "Medium"
                    }
                }
            };
            var tmpPath = Path.GetTempFileName();
            try
            {
                JsonExporter.WriteFinal(recipe, tmpPath);
                var written = File.ReadAllText(tmpPath);
                Assert.IsTrue(written.Contains("\"ID\""));
                Assert.IsTrue(written.Contains("\"IngredientID\":\"PizzaSauce\""));
                Assert.IsTrue(written.Contains("\"Size\":1"));  // PC3 IngredientSize: Medium=1
                Assert.IsTrue(written.Contains("\"DoughPositions\""));
                Assert.IsTrue(written.Contains("\"ProfitFactor\":1.5"));
            }
            finally { File.Delete(tmpPath); }
        }

        [Test]
        public void WriteFinal_SizeEnum_LargeMapsToZero()
        {
            var recipe = new RecipeData
            {
                ingredients = new[]
                {
                    new IngredientSelectionData
                    {
                        id = "Mozzarella",
                        amount_g = 50f,
                        position = new[] { 0f, 0f, 0.95f },
                        rotation = new[] { 0f, 0f, 0f },
                        size = "Large"
                    }
                }
            };
            var tmpPath = Path.GetTempFileName();
            try
            {
                JsonExporter.WriteFinal(recipe, tmpPath);
                var written = File.ReadAllText(tmpPath);
                Assert.IsTrue(written.Contains("\"Size\":0"));  // Large=0
            }
            finally { File.Delete(tmpPath); }
        }

        [Test]
        public void WriteFinal_SizeEnum_SmallMapsToTwo()
        {
            var recipe = new RecipeData
            {
                ingredients = new[]
                {
                    new IngredientSelectionData
                    {
                        id = "Jalapeno",
                        amount_g = 25f,
                        position = new[] { 0f, 0f, 0.95f },
                        rotation = new[] { 0f, 0f, 0f },
                        size = "Small"
                    }
                }
            };
            var tmpPath = Path.GetTempFileName();
            try
            {
                JsonExporter.WriteFinal(recipe, tmpPath);
                var written = File.ReadAllText(tmpPath);
                Assert.IsTrue(written.Contains("\"Size\":2"));  // Small=2
            }
            finally { File.Delete(tmpPath); }
        }

        [Test]
        public void WriteRecipe_StoresRecipeJson()
        {
            var recipe = new RecipeData { name = "Test" };
            var tmpPath = Path.GetTempFileName();
            try
            {
                JsonExporter.WriteRecipe(recipe, tmpPath);
                Assert.IsTrue(File.Exists(tmpPath));
                var written = File.ReadAllText(tmpPath);
                Assert.IsTrue(written.Contains("\"name\":\"Test\""));
            }
            finally { File.Delete(tmpPath); }
        }

        [Test]
        public void WriteFinal_EmptyIngredients_ProducesEmptyArray()
        {
            var recipe = new RecipeData { name = "Empty", ingredients = new IngredientSelectionData[0] };
            var tmpPath = Path.GetTempFileName();
            try
            {
                JsonExporter.WriteFinal(recipe, tmpPath);
                var written = File.ReadAllText(tmpPath);
                Assert.IsTrue(written.Contains("\"Ingredients\":[]"));
            }
            finally { File.Delete(tmpPath); }
        }
    }
}
