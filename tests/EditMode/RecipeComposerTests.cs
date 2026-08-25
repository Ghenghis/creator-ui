using NUnit.Framework;
using creator_ui.LLM;
using creator_ui.Recipe;
using System.Threading.Tasks;

namespace creator_ui.tests.EditMode
{
    public class RecipeComposerTests
    {
        [Test]
        public async Task ComposeAsync_ParsesLLMJson_ReturnsValidRecipe()
        {
            var stubClient = new StubLLMClient(@"{
                ""name"": ""Test Pizza"",
                ""dough"": {""size"": ""Large"", ""shape"": ""Round""},
                ""ingredients"": [
                    {""id"": ""PizzaSauce"", ""amount_g"": 100, ""position"": [0,0,0.95], ""rotation"": [0,0,0], ""size"": ""Medium""},
                    {""id"": ""Mozzarella"", ""amount_g"": 50, ""position"": [0.1,0.05,0.95], ""rotation"": [0,0,0], ""size"": ""Large""}
                ]
            }");
            var composer = new RecipeComposer(stubClient);
            var recipe = await composer.ComposeAsync("system", "user");
            Assert.AreEqual("Test Pizza", recipe.name);
            Assert.IsNotNull(recipe.scores);
            Assert.That(recipe.scores.taste, Is.GreaterThan(0));
        }

        [Test]
        public void ComposeAsync_InvalidJson_Throws()
        {
            var stubClient = new StubLLMClient("not valid json");
            var composer = new RecipeComposer(stubClient);
            Assert.ThrowsAsync<System.Exception>(async () =>
                await composer.ComposeAsync("system", "user"));
        }

        [Test]
        public async Task ComposeAsync_UnknownIngredient_FlaggedInMeta()
        {
            var stubClient = new StubLLMClient(@"{
                ""name"": ""Mystery"",
                ""ingredients"": [
                    {""id"": ""DefinitelyNotInCatalog"", ""amount_g"": 100, ""position"": [0,0,0.95], ""rotation"": [0,0,0], ""size"": ""Medium""}
                ]
            }");
            var composer = new RecipeComposer(stubClient);
            var recipe = await composer.ComposeAsync("system", "user");
            Assert.AreEqual(1, recipe._meta.unknown_ingredient_count);
        }
    }

    public class StubLLMClient : LLMClient
    {
        private readonly string _response;
        public StubLLMClient(string response) : base(
            new BarrosBackend("http://localhost:1"),
            new LMStudioBackend("http://localhost:1", "stub"),
            new OpenAIBackend("stub-key"))
        {
            _response = response;
        }
        public new Task<string> CompleteAsync(string sys, string usr) =>
            Task.FromResult(_response);
    }
}
