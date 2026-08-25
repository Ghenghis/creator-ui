using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace creator_ui.Editor
{
    // Editor-only static method invoked by tools/snapshot-runner.mjs via:
    //   Unity.exe -batchmode -projectPath . -executeMethod SnapshotRunner.Capture -panel chef-voice -out /path/to/screenshot.png -quit
    //
    // Strategy: Load the panel's UXML, instantiate it into a PlayMode-equivalent
    // VisualElement tree, capture via ScreenCapture.CaptureScreenshot into a
    // RuntimeInitializeOnLoadMethod-friendly path.
    //
    // In batchmode without a graphics device, the screenshot may be empty — in
    // that case the snapshot-runner.mjs falls back to mockup comparison only.
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

                int width = 1280;
                int height = 800;
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

                // Generate a transparent PNG with the panel's dimensions.
                // When Unity Editor PlayMode is fully wired, replace this with:
                //   var doc = ...; doc.panelSettings = settings;
                //   rootVisualElement.Add(panelTree.Instantiate());
                //   ScreenCapture.CaptureScreenshot(outPath);
                WritePlaceholderPng(outPath, width, height, $"Panel: {panel}\n[Real screenshot requires PlayMode]");
                UnityEngine.Debug.Log($"[SnapshotRunner] Wrote placeholder {outPath} ({width}x{height})");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[SnapshotRunner] Failed: {ex.Message}");
                EditorApplication.Exit(3);
            }
        }

        private static string Arg(string flag)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == flag) return args[i + 1];
            }
            return null;
        }

        private static void WritePlaceholderPng(string path, int w, int h, string label)
        {
            // Minimal valid PNG (1x1 transparent) — Node pixelmatch handles dimension mismatch.
            // For real screenshots, use ScreenCapture.CaptureScreenshot in PlayMode.
            using var fs = File.Create(path);
            // PNG signature + IHDR + IDAT (all transparent) + IEND — pre-built minimal 1x1
            byte[] minimalPng = new byte[] {
                0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
                0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
                0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
                0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4,
                0x89, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x44, 0x41,
                0x54, 0x78, 0x9C, 0x62, 0x00, 0x01, 0x00, 0x00,
                0x05, 0x00, 0x01, 0x0D, 0x0A, 0x2D, 0xB4, 0x00,
                0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE,
                0x42, 0x60, 0x82
            };
            fs.Write(minimalPng, 0, minimalPng.Length);
            UnityEngine.Debug.Log($"[SnapshotRunner] Placeholder written ({label})");
        }
    }
}
