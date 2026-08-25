using NUnit.Framework;
using creator_ui.Recipe;

namespace creator_ui.tests.EditMode
{
    public class HistoryStoreTests
    {
        [Test]
        public void SaveMessage_WritesFile()
        {
            var mode = "test-mode-" + System.Guid.NewGuid().ToString("N").Substring(0, 8);
            try
            {
                HistoryStore.SaveMessage(mode, "user", "Hello world");
                Assert.IsTrue(System.IO.File.Exists(HistoryStore.PathForTest(mode)));
            }
            finally
            {
                HistoryStore.Clear(mode);
            }
        }

        [Test]
        public void LoadRecent_ReturnsLatestEntries()
        {
            var mode = "test-mode-" + System.Guid.NewGuid().ToString("N").Substring(0, 8);
            try
            {
                HistoryStore.SaveMessage(mode, "user", "msg1");
                HistoryStore.SaveMessage(mode, "assistant", "msg2");
                HistoryStore.SaveMessage(mode, "user", "msg3");
                var recent = HistoryStore.LoadRecent(mode, 10);
                Assert.AreEqual(3, recent.Count);
                Assert.AreEqual("msg1", recent[0].content);
                Assert.AreEqual("msg3", recent[2].content);
            }
            finally
            {
                HistoryStore.Clear(mode);
            }
        }

        [Test]
        public void LoadRecent_TruncatesToMaxTurns()
        {
            var mode = "test-mode-" + System.Guid.NewGuid().ToString("N").Substring(0, 8);
            try
            {
                for (int i = 0; i < 5; i++) HistoryStore.SaveMessage(mode, "user", $"msg{i}");
                var recent = HistoryStore.LoadRecent(mode, 3);
                Assert.AreEqual(3, recent.Count);
                Assert.AreEqual("msg2", recent[0].content);
                Assert.AreEqual("msg4", recent[2].content);
            }
            finally
            {
                HistoryStore.Clear(mode);
            }
        }

        [Test]
        public void Clear_RemovesFile()
        {
            var mode = "test-mode-" + System.Guid.NewGuid().ToString("N").Substring(0, 8);
            HistoryStore.SaveMessage(mode, "user", "test");
            HistoryStore.Clear(mode);
            Assert.IsFalse(System.IO.File.Exists(HistoryStore.PathForTest(mode)));
        }

        [Test]
        public void LoadRecent_MissingFile_ReturnsEmpty()
        {
            var mode = "nonexistent-mode-" + System.Guid.NewGuid().ToString("N").Substring(0, 8);
            var recent = HistoryStore.LoadRecent(mode);
            Assert.AreEqual(0, recent.Count);
        }
    }
}
