using UnityEngine;
using UnityEngine.UIElements;

namespace creator_ui.Chat
{
    // Resources loader for UXML/USS panels (works in Editor + PlayMode + builds).
    // Resources are expected at: Assets/Resources/UI/{Panels,Sidebar,Shared}/
    public static class CreatorUIResourcesLoader
    {
        public static VisualTreeAsset LoadPanel(string panelName) =>
            Resources.Load<VisualTreeAsset>($"UI/Panels/{panelName}");

        public static StyleSheet LoadPanelUSS(string panelName) =>
            Resources.Load<StyleSheet>($"UI/Panels/{panelName}");

        public static VisualTreeAsset LoadSidebar() =>
            Resources.Load<VisualTreeAsset>("UI/Sidebar/SidebarTabs");

        public static StyleSheet LoadSidebarUSS() =>
            Resources.Load<StyleSheet>("UI/Sidebar/SidebarTabs");

        public static StyleSheet LoadShared(string sheetName) =>
            Resources.Load<StyleSheet>($"UI/Shared/{sheetName}");
    }
}
