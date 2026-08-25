using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace creator_ui.Chat
{
    // Reads output/*.final.json and renders a thumbnail grid (uses embedded Texture base64).
    // Attach to the Gallery panel UXML container.
    public class GalleryView : MonoBehaviour
    {
        public VisualElement container;

        private void OnEnable()
        {
            if (container == null)
            {
                container = GetComponent<UIDocument>()?.rootVisualElement?.Q<VisualElement>("gallery-grid");
            }
            if (container != null) Refresh();
        }

        public void Refresh()
        {
            if (container == null) return;
            container.Clear();

            string outDir = Path.Combine(Application.dataPath, "..", "output");
            if (!Directory.Exists(outDir)) return;

            var files = Directory.GetFiles(outDir, "*.final.json");
            System.Array.Sort(files); // reverse-chronological

            foreach (var f in files)
            {
                try
                {
                    var json = File.ReadAllText(f);
                    var recipe = JsonUtility.FromJson<RecipeRef>(json);
                    if (recipe == null) continue;

                    var card = new VisualElement();
                    card.style.flexDirection = FlexDirection.Row;
                    card.style.backgroundColor = new StyleColor(new Color(1, 1, 1, 0.95f));
                    card.style.borderTopLeftRadius = 8;
                    card.style.borderTopRightRadius = 8;
                    card.style.borderBottomLeftRadius = 8;
                    card.style.borderBottomRightRadius = 8;
                    card.style.paddingTop = 8;
                    card.style.paddingBottom = 8;
                    card.style.paddingLeft = 8;
                    card.style.paddingRight = 8;
                    card.style.marginTop = 4;
                    card.style.marginBottom = 4;
                    card.style.marginLeft = 4;
                    card.style.marginRight = 4;

                    var thumb = new VisualElement();
                    thumb.style.width = 64;
                    thumb.style.height = 64;
                    thumb.style.backgroundColor = new StyleColor(new Color(0.95f, 0.9f, 0.8f));
                    if (!string.IsNullOrEmpty(recipe.Texture))
                    {
                        var tex = LoadTexture(recipe.Texture);
                        if (tex != null)
                        {
                            thumb.style.backgroundImage = new StyleBackground(tex);
                            thumb.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
                        }
                    }
                    card.Add(thumb);

                    var body = new VisualElement();
                    body.style.flexGrow = 1;
                    body.style.marginLeft = 8;
                    var name = new Label(recipe.ID);
                    name.style.unityFontStyleAndWeight = FontStyle.Bold;
                    name.style.fontSize = 14;
                    body.Add(name);
                    var meta = new Label($"{recipe.Ingredients?.Length ?? 0} ingredients");
                    meta.style.fontSize = 11;
                    meta.style.color = new StyleColor(new Color(0.4f, 0.3f, 0.2f));
                    body.Add(meta);
                    card.Add(body);

                    container.Add(card);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[GalleryView] Failed to load {f}: {ex.Message}");
                }
            }
        }

        private Texture2D LoadTexture(string b64)
        {
            try
            {
                var bytes = System.Convert.FromBase64String(b64);
                var tex = new Texture2D(2, 2);
                tex.LoadImage(bytes);
                return tex;
            }
            catch { return null; }
        }

        // Minimal subset of recipe fields we care about for the gallery
        [System.Serializable]
        private class RecipeRef
        {
            public string ID;
            public string Texture;
            public IngredientRef[] Ingredients;
        }

        [System.Serializable]
        private class IngredientRef
        {
            public string IngredientID;
            public int Size;
        }
    }
}
