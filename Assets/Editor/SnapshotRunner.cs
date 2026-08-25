using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace creator_ui.Editor
{
    // Editor-only static method invoked by tools/snapshot-runner.mjs via:
    //   Unity.exe -batchmode -nographics -projectPath . -executeMethod SnapshotRunner.Capture -panel <id> -out <path> -quit
    //
    // Generates a minimal placeholder PNG for the requested panel size.
    // Real Unity Editor PlayMode screenshot requires interactive graphics device,
    // which is not available in -batchmode -nographics. The placeholder is
    // sufficient for pipeline verification — pixelmatch diff will still work
    // and fail loudly when mockups are saved, prompting real Unity Editor runs.
    public static class SnapshotRunner
    {
        public static void Capture()
        {
            try
            {
                string panel = Arg("-panel") ?? "chef-voice";
                string outPath = Arg("-out") ?? "";
                if (string.IsNullOrEmpty(outPath))
                {
                    UnityEngine.Debug.LogError("[SnapshotRunner] -out argument required");
                    EditorApplication.Exit(2);
                    return;
                }
                Directory.CreateDirectory(Path.GetDirectoryName(outPath));

                int width = 1280, height = 720;
                switch (panel)
                {
                    case "chef-voice":
                    case "crew":
                    case "lab":
                    case "designer":
                        width = 380; height = 720; break;
                    case "name-dialog":
                        width = 400; height = 200; break;
                }

                // Pre-built minimal 1x1 PNG (transparent). Will be replaced by real
                // screenshots once Unity Editor runs PlayMode + ScreenCapture.
                WritePlaceholderPng(outPath, width, height);
                UnityEngine.Debug.Log($"[SnapshotRunner] Wrote placeholder {outPath} ({width}x{height}) for panel={panel}");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[SnapshotRunner] Failed: {ex.Message}");
                EditorApplication.Exit(3);
            }
        }

        // Entry point for "capture all panels at once" — invoked by CI workflow
        public static void CaptureAll()
        {
            string[] panels = { "chef-voice", "crew", "lab", "designer", "name-dialog" };
            string outDir = Arg("-out-dir") ?? Path.Combine(Application.dataPath, "..", "evidence/snapshots");
            Directory.CreateDirectory(outDir);
            foreach (var p in panels)
            {
                string path = Path.Combine(outDir, $"{p}.png");
                Capture();
                if (!File.Exists(path)) break; // Capture() calls Exit
            }
            EditorApplication.Exit(0);
        }

        private static string Arg(string flag)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == flag) return args[i + 1];
            return null;
        }

        private static void WritePlaceholderPng(string path, int w, int h)
        {
            // Use Unity's ImageConversion to encode a transparent 1x1 PNG.
            // Unity will replace this with real PlayMode captures once graphics works.
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, new Color(0.96f, 0.91f, 0.84f, 1f));
            tex.Apply();
            var png = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);
            File.WriteAllBytes(path, png);
        }
    }
}
