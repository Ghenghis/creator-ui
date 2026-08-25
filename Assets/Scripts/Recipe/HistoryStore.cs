using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace creator_ui.Recipe
{
    // Persistent chat history per chat mode. Saved as JSON in Application.persistentDataPath/history/
    public static class HistoryStore
    {
        private static string HistoryDir => Path.Combine(Application.persistentDataPath, "history");

        public static void SaveMessage(string mode, string role, string content)
        {
            if (string.IsNullOrEmpty(mode)) return;
            Directory.CreateDirectory(HistoryDir);
            var path = PathFor(mode);
            var entries = LoadRaw(mode);
            entries.Add(new HistoryEntry { role = role, content = content, ts = System.DateTime.UtcNow.ToString("o") });
            File.WriteAllText(path, JsonUtility.ToJson(new HistoryFile { entries = entries.ToArray() }, true));
        }

        public static List<(string role, string content)> LoadRecent(string mode, int maxTurns = 10)
        {
            var entries = LoadRaw(mode);
            var result = new List<(string, string)>();
            int start = System.Math.Max(0, entries.Count - maxTurns);
            for (int i = start; i < entries.Count; i++)
                result.Add((entries[i].role, entries[i].content));
            return result;
        }

        public static void Clear(string mode)
        {
            var path = PathFor(mode);
            if (File.Exists(path)) File.Delete(path);
        }

        private static List<HistoryEntry> LoadRaw(string mode)
        {
            var path = PathFor(mode);
            if (!File.Exists(path)) return new List<HistoryEntry>();
            try
            {
                var file = JsonUtility.FromJson<HistoryFile>(File.ReadAllText(path));
                if (file?.entries == null) return new List<HistoryEntry>();
                return new List<HistoryEntry>(file.entries);
            }
            catch { return new List<HistoryEntry>(); }
        }

        private static string PathFor(string mode) => Path.Combine(HistoryDir, $"{mode}.json");

        // Internal accessor for tests (exposes the private path resolver)
        public static string PathForTest(string mode) => PathFor(mode);

        [System.Serializable]
        private class HistoryFile { public HistoryEntry[] entries; }

        [System.Serializable]
        private class HistoryEntry { public string role; public string content; public string ts; }
    }
}
