using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

//TODO (MK): THIS FILE IS WORK IN PROGRESS AND CURRENTLY NOT USED ANYWHERE.

class SyntheticHumanEditorToolsWindow : EditorWindow
{
    VisualElement m_Root;
    const string k_UxmlPath = "Packages/com.unity.cv.synthetichumans/Editor/Uxml/SyntheticHumanEditorToolsWindow.uxml";
    static SyntheticHumanEditorToolsWindow s_CurrentWindowInstance;

    static SyntheticHumanEditorToolsWindow instance
    {
        get
        {
            var window = HasOpenInstances<SyntheticHumanEditorToolsWindow>() ? GetWindow<SyntheticHumanEditorToolsWindow>() : CreateWindow<SyntheticHumanEditorToolsWindow>();
            window.titleContent = new GUIContent("Synthetic Humans Editor Tools");
            s_CurrentWindowInstance = window;
            return s_CurrentWindowInstance;
        }
    }

    //[MenuItem("Window/Synthetic Humans/Editor Tools")]
    public static void ShowWindow()
    {
        instance.Focus();
        instance.Show();
        instance.minSize = new Vector2(500, 500);
    }

    public void OnEnable()
    {
        m_Root = rootVisualElement;

        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_UxmlPath);
        visualTree.CloneTree(m_Root);

        var tabview = m_Root.Q<SyntheticHumanTabbedView>("RootTabbedView");

        var tab1Target = m_Root.Q<VisualElement>("Tab1Target");
        var tab2Target = m_Root.Q<VisualElement>("Tab2Target");

        var tab1 = new SyntheticHumanTabButton("Tab1", tab1Target);
        var tab2 = new SyntheticHumanTabButton("Tab2", tab2Target);

        tabview.AddTab(tab1, true);
        tabview.AddTab(tab2, false);
    }
}
