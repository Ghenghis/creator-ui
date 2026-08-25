using creator_ui.LLM;
using creator_ui.Recipe;
using UnityEngine;
using UnityEngine.UIElements;

namespace creator_ui.Chat
{
    // Runtime bootstrap for CreatorUI scene. Attach to a GameObject with UIDocument.
    // Loads panel UXMLs from Resources, wires TabNavigator + LLMClient + RecipeComposer.
    [RequireComponent(typeof(UIDocument))]
    public class CreatorUIBootstrap : MonoBehaviour
    {
        [Header("Chat backend")]
        public string lmstudioUrl = "http://127.0.0.1:1234";
        public string lmstudioModel = "qwen3.8-9b-uncensored-cyber-exploit-xrpl-v3";
        public string openaiKeyEnv = "OPENAI_API_KEY";

        // Auto-load from Resources if not set in Inspector
        private VisualTreeAsset _sidebar;
        private VisualTreeAsset _chefVoice;
        private VisualTreeAsset _crew;
        private VisualTreeAsset _lab;
        private VisualTreeAsset _designer;
        private VisualTreeAsset _nameDialog;
        private StyleSheet _theme;
        private StyleSheet _buttons;
        private StyleSheet _cards;
        private StyleSheet _bars;
        private StyleSheet _sidebarUSS;
        private StyleSheet _chefVoiceUSS;
        private StyleSheet _crewUSS;
        private StyleSheet _labUSS;
        private StyleSheet _designerUSS;
        private StyleSheet _nameDialogUSS;

        private void Awake()
        {
            _sidebar = CreatorUIResourcesLoader.LoadSidebar();
            _chefVoice = CreatorUIResourcesLoader.LoadPanel("ChefVoice");
            _crew = CreatorUIResourcesLoader.LoadPanel("Crew");
            _lab = CreatorUIResourcesLoader.LoadPanel("Lab");
            _designer = CreatorUIResourcesLoader.LoadPanel("Designer");
            _nameDialog = CreatorUIResourcesLoader.LoadPanel("NameDialog");
            _theme = CreatorUIResourcesLoader.LoadShared("Theme");
            _buttons = CreatorUIResourcesLoader.LoadShared("Buttons");
            _cards = CreatorUIResourcesLoader.LoadShared("Cards");
            _bars = CreatorUIResourcesLoader.LoadShared("Bars");
            _sidebarUSS = CreatorUIResourcesLoader.LoadSidebarUSS();
            _chefVoiceUSS = CreatorUIResourcesLoader.LoadPanelUSS("ChefVoice");
            _crewUSS = CreatorUIResourcesLoader.LoadPanelUSS("Crew");
            _labUSS = CreatorUIResourcesLoader.LoadPanelUSS("Lab");
            _designerUSS = CreatorUIResourcesLoader.LoadPanelUSS("Designer");
            _nameDialogUSS = CreatorUIResourcesLoader.LoadPanelUSS("NameDialog");

            int missing = 0;
            if (_sidebar == null) { Debug.LogError("[Bootstrap] Missing UI/Sidebar/SidebarTabs.uxml"); missing++; }
            if (_chefVoice == null) { Debug.LogError("[Bootstrap] Missing UI/Panels/ChefVoice.uxml"); missing++; }
            if (_crew == null) { Debug.LogError("[Bootstrap] Missing UI/Panels/Crew.uxml"); missing++; }
            if (_lab == null) { Debug.LogError("[Bootstrap] Missing UI/Panels/Lab.uxml"); missing++; }
            if (_designer == null) { Debug.LogError("[Bootstrap] Missing UI/Panels/Designer.uxml"); missing++; }
            if (_nameDialog == null) { Debug.LogError("[Bootstrap] Missing UI/Panels/NameDialog.uxml"); missing++; }
            if (missing > 0)
            {
                Debug.LogError($"[Bootstrap] {missing} required UI asset(s) missing — verify Resources/UI/ exists");
            }
        }

        private void OnEnable()
        {
            var doc = GetComponent<UIDocument>();
            if (doc == null || doc.rootVisualElement == null) return;
            var root = doc.rootVisualElement;

            // Apply theme stylesheets (order matters: theme first, then panels)
            if (_theme != null) root.styleSheets.Add(_theme);
            if (_buttons != null) root.styleSheets.Add(_buttons);
            if (_cards != null) root.styleSheets.Add(_cards);
            if (_bars != null) root.styleSheets.Add(_bars);
            if (_sidebarUSS != null) root.styleSheets.Add(_sidebarUSS);
            if (_chefVoiceUSS != null) root.styleSheets.Add(_chefVoiceUSS);
            if (_crewUSS != null) root.styleSheets.Add(_crewUSS);
            if (_labUSS != null) root.styleSheets.Add(_labUSS);
            if (_designerUSS != null) root.styleSheets.Add(_designerUSS);
            if (_nameDialogUSS != null) root.styleSheets.Add(_nameDialogUSS);

            // Build layout: sidebar | content-root | dialog-layer
            root.Clear();
            var mainRow = new VisualElement();
            mainRow.name = "main-row";
            mainRow.style.flexDirection = FlexDirection.Row;
            mainRow.style.flexGrow = 1;
            mainRow.style.height = new Length(100, LengthUnit.Percent);
            root.Add(mainRow);

            // Sidebar
            if (_sidebar != null)
            {
                var sb = _sidebar.Instantiate();
                sb.style.flexShrink = 0;
                mainRow.Add(sb);
            }

            // Content root (right of sidebar)
            var contentRoot = new VisualElement { name = "content-root" };
            contentRoot.style.flexGrow = 1;
            contentRoot.style.height = new Length(100, LengthUnit.Percent);
            mainRow.Add(contentRoot);

            // Dialog layer (full-screen overlay, hidden until NameDialog shows)
            var dialogLayer = new VisualElement { name = "dialog-layer" };
            dialogLayer.style.position = Position.Absolute;
            dialogLayer.style.left = 0;
            dialogLayer.style.right = 0;
            dialogLayer.style.top = 0;
            dialogLayer.style.bottom = 0;
            dialogLayer.pickingMode = PickingMode.Ignore;
            root.Add(dialogLayer);

            // Build LLM stack (Barros sidecar primary, LMStudio fallback, OpenAI last)
            var barros = new BarrosBackend();
            var lmstudio = new LMStudioBackend(lmstudioUrl, lmstudioModel);
            var openai = new OpenAIBackend(System.Environment.GetEnvironmentVariable(openaiKeyEnv) ?? "");
            var client = new LLMClient(barros, lmstudio, openai);

            // Serialize catalog for Barros /compose (used by RecipeComposer + CrewPanel)
            var catalog = Recipe.IngredientCatalog.Load();
            var catalogJsonArray = RecipeComposer.SerializeCatalogForBarrosPublic(catalog);

            // Wire NameDialog
            var nameDialogComp = gameObject.AddComponent<NameDialog>();
            nameDialogComp.document = doc;
            nameDialogComp.dialogTree = _nameDialog;

            // Wire TabNavigator
            var nav = gameObject.AddComponent<Sidebar.TabNavigator>();
            nav.document = doc;
            nav.chefVoicePanel = _chefVoice;
            nav.crewPanel = _crew;
            nav.labPanel = _lab;
            nav.designerPanel = _designer;

            // Wire panel controllers with LLM client + Barros + catalog
            var chefVoice = gameObject.AddComponent<ChefVoicePanel>();
            chefVoice.llmClient = client;
            chefVoice.barros = barros;
            chefVoice.catalogJsonArray = catalogJsonArray;
            chefVoice.nameDialog = nameDialogComp;
            var crew = gameObject.AddComponent<CrewPanel>();
            crew.llmClient = client;
            crew.barros = barros;
            crew.catalogJsonArray = catalogJsonArray;
            crew.nameDialog = nameDialogComp;
            var lab = gameObject.AddComponent<LabPanel>();
            lab.llmClient = client;
            lab.barros = barros;
            lab.catalogJsonArray = catalogJsonArray;
            lab.nameDialog = nameDialogComp;
            var designer = gameObject.AddComponent<DesignerPanel>();
            designer.llmClient = client;
            designer.barros = barros;
            designer.catalogJsonArray = catalogJsonArray;
            designer.nameDialog = nameDialogComp;

            Debug.Log("[CreatorUIBootstrap] Creator UI ready. 4 chat panels + NameDialog + Sidebar wired to Barros+LMStudio.");
        }
    }
}
