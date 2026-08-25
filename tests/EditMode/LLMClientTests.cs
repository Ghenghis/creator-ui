using NUnit.Framework;
using creator_ui.LLM;

namespace creator_ui.tests.EditMode
{
    public class LLMClientTests
    {
        [Test]
        public void MaskKey_OpenAIKey_ReturnsMasked()
        {
            var masked = LLMClient.MaskKey("sk-1234567890abcdef");
            Assert.AreEqual("sk-1...cdef", masked);
        }

        [Test]
        public void MaskKey_ShortKey_ReturnsMasked()
        {
            var masked = LLMClient.MaskKey("sk-1234");
            Assert.AreEqual("****", masked);
        }

        [Test]
        public void MaskKey_Empty_ReturnsMasked()
        {
            var masked = LLMClient.MaskKey("");
            Assert.AreEqual("****", masked);
        }

        [Test]
        public void MaskKey_Null_ReturnsMasked()
        {
            var masked = LLMClient.MaskKey(null);
            Assert.AreEqual("****", masked);
        }

        [Test]
        public void BarrosBackend_Constructs()
        {
            var backend = new BarrosBackend("http://127.0.0.1:48173");
            Assert.AreEqual("http://127.0.0.1:48173", backend.BaseUrl);
        }

        [Test]
        public void BarrosBackend_TrimsTrailingSlash()
        {
            var backend = new BarrosBackend("http://127.0.0.1:48173/");
            Assert.AreEqual("http://127.0.0.1:48173", backend.BaseUrl);
        }

        [Test]
        public void LMStudioBackend_Constructs()
        {
            var backend = new LMStudioBackend("http://localhost:1234", "test-model");
            Assert.IsNotNull(backend);
        }

        [Test]
        public void OpenAIBackend_Constructs()
        {
            var backend = new OpenAIBackend("sk-test-key-1234");
            Assert.IsNotNull(backend);
        }

        [Test]
        public void LLMClient_Constructs()
        {
            var barros = new BarrosBackend();
            var lm = new LMStudioBackend("http://localhost:1234", "m");
            var oa = new OpenAIBackend("sk-test");
            var client = new LLMClient(barros, lm, oa);
            Assert.IsNotNull(client);
        }
    }
}
