using NUnit.Framework;
using creator_ui.LLM;

namespace creator_ui.tests.EditMode
{
    public class LMStudioBackendTests
    {
        [Test]
        public void StripMarkdownCodeBlock_PlainJSON_ReturnsUnchanged()
        {
            var raw = "{\"name\":\"Margherita\"}";
            var stripped = LLMJson.StripMarkdownCodeBlock(raw);
            Assert.AreEqual("{\"name\":\"Margherita\"}", stripped);
        }

        [Test]
        public void StripMarkdownCodeBlock_JsonFence_StripsFence()
        {
            var raw = "```json\n{\"name\":\"M\"}\n```";
            var stripped = LLMJson.StripMarkdownCodeBlock(raw);
            Assert.AreEqual("{\"name\":\"M\"}", stripped);
        }

        [Test]
        public void StripMarkdownCodeBlock_PlainFence_StripsFence()
        {
            var raw = "```\n{\"name\":\"X\"}\n```";
            var stripped = LLMJson.StripMarkdownCodeBlock(raw);
            Assert.AreEqual("{\"name\":\"X\"}", stripped);
        }

        [Test]
        public void StripMarkdownCodeBlock_NullOrEmpty_ReturnsInput()
        {
            Assert.AreEqual("", LLMJson.StripMarkdownCodeBlock(""));
            Assert.IsNull(LLMJson.StripMarkdownCodeBlock(null));
        }

        [Test]
        public void StripMarkdownCodeBlock_HandlesSurroundingWhitespace()
        {
            var raw = "  \n```json\n{\"name\":\"W\"}\n```\n  ";
            var stripped = LLMJson.StripMarkdownCodeBlock(raw);
            Assert.AreEqual("{\"name\":\"W\"}", stripped);
        }

        [Test]
        public void Escape_HandlesSpecialChars()
        {
            var escaped = LLMJson.Escape("He said \"hello\"\nWorld\t!");
            Assert.AreEqual("He said \\\"hello\\\"\\nWorld\\t!", escaped);
        }

        [Test]
        public void Escape_Null_ReturnsEmpty()
        {
            Assert.AreEqual("", LLMJson.Escape(null));
        }

        [Test]
        public void ArrayOf_BuildsValidJsonArray()
        {
            var msgs = new[]
            {
                new LLMMessage("system", "you are helpful"),
                new LLMMessage("user", "hi")
            };
            var json = LLMJson.ArrayOf(msgs);
            Assert.IsTrue(json.StartsWith("["));
            Assert.IsTrue(json.EndsWith("]"));
            Assert.IsTrue(json.Contains("\"role\":\"system\""));
            Assert.IsTrue(json.Contains("\"role\":\"user\""));
            Assert.IsTrue(json.Contains("\\\"hi\\\""));  // escaped quotes
        }
    }
}
