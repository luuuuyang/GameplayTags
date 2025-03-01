using System.Collections.Generic;
using GameplayTags;
using GameplayTags.Editor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

public class OdinAddNewGameplayTagSource : OdinEditorWindow
{
    public delegate void OnGameplayTagSourceAdded(in string sourceName);

    public string Name;

    [ValueDropdown("TagRoots", FlattenTreeView = true)]
    public string TagRoot;

    [FolderPath]
    public string Path;

    private OnGameplayTagSourceAdded onGameplayTagSourceAdded;
    private List<string> TagRoots = new();
    private string DefaultNewName;

    public static void ShowWindow()
    {
        OdinAddNewGameplayTagSource window = GetWindow<OdinAddNewGameplayTagSource>("Add New Gameplay Tag Source");
        window.PopulateTagRoots();
    }

    private void PopulateTagRoots()
    {
        GameplayTagsManager manager = GameplayTagsManager.Instance;
        TagRoots.Clear();

        string defaultSource = GameplayTagSource.DefaultName;
        
        List<string> tagRootStrings = new List<string>();
        manager.GetTagSourceSearchPaths(ref tagRootStrings);

        foreach (string tagRoot in tagRootStrings)
        {
            TagRoots.Add(tagRoot);
        }
    }

    [Button("Add New Source")]
    private void OnAddNewSourceButtonPressed()
    {
        IGameplayTagsEditorModule.Instance.AddNewGameplayTagSource(Name, TagRoot);
        // Debug.Log($"{Path}");
    }
}
