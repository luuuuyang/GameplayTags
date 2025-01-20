using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector;
using Sirenix.Utilities.Editor;

namespace GameplayTags.Editor
{
    public class GameplayTagsEditor : OdinEditorWindow
    {

        // public void CreateGUI()
        // {
        //     var gameplayTagsManager = GameplayTagsManager.Instance;

        //     // Each editor window contains a root VisualElement object
        //     VisualElement root = rootVisualElement;

        //     // Instantiate UXML
        //     VisualElement labelFromUXML = m_VisualTreeAsset.Instantiate();
        //     root.Add(labelFromUXML);

        //     var section = root.Q<VisualElement>("Section");

        //     root.Q<Button>("AddNewGameplayTagButton").clicked += () =>
        //     {
        //         section.style.display = section.style.display == DisplayStyle.None ? DisplayStyle.Flex : DisplayStyle.None;
        //     };

        //     var defaultGameplayTags = AssetDatabase.LoadAssetAtPath<GameplayTagsList>("Assets/GameplayEffects/DefaultGameplayTags.asset");

        //     var items = new List<TreeViewItemData<string>>();

        //     var item = new TreeViewItemData<string>(0, "A");
        //     var item2 = new List<TreeViewItemData<string>>() { new TreeViewItemData<string>(1, "B") };
        //     var item3 = new List<TreeViewItemData<string>>() { new TreeViewItemData<string>(2, "C") };

        //     // for (int i = 0; i < defaultGameplayTags.GameplayTagList.Count; i++)
        //     // {
        //     //     var tag = defaultGameplayTags.GameplayTagList[i];
        //     //     var splitTag = tag.Split('.');
        //     //     for (int j = 0; j < splitTag.Length; j++)
        //     //     {
        //     //         var data = new TreeViewItemData<string>(i, splitTag[j]);
        //     //         if (!items.Contains(data))
        //     //         {
        //     //             items.Add(data);
        //     //         }
        //     //     }
        //     // }

        //     var treeView = root.Q<TreeView>("TreeView");
        //     Func<VisualElement> makeItem = () => new Label();
        //     Action<VisualElement, int> bindItem = (e, i) =>
        //     {
        //         var item = treeView.GetItemDataForIndex<string>(i);
        //         (e as Label).text = item;
        //     };

        //     treeView.SetRootItems(items);
        //     treeView.makeItem = makeItem;
        //     treeView.bindItem = bindItem;
        //     treeView.Rebuild();

        //     var tagNameTextField = root.Q<TextField>("TagNameTextField");

        //     var addNewTagButton = root.Q<Button>("AddNewTagButton");
        //     addNewTagButton.clicked += () =>
        //     {
        //         // var splitTag = tagNameTextField.text.Split('.');
        //         // treeView.AddItem(new TreeViewItemData<string>(treeView.GetTreeCount(), tagNameTextField.text));
        //         // items[1] = new TreeViewItemData<string>(1, "tagNameTextField.text");
        //         // defaultGameplayTags.GameplayTagList.Add(tagNameTextField.text);
        //         // EditorUtility.SetDirty(defaultGameplayTags);
        //         // AssetDatabase.SaveAssets();
        //         // treeView.RefreshItem(1);

        //         // Debug.Log(treeView.itemsSource);
        //     };

        //     treeView.RegisterCallback<MouseUpEvent>(e =>
        //     {
        //         if (e.button == 1)
        //         {
        //             GenericMenu menu = new();
        //             menu.AddItem(new GUIContent("Add Sub Tag"), false, () => Debug.Log("Option 1 clicked"));
        //             menu.AddItem(new GUIContent("Duplicate Tag"), false, () => Debug.Log("Option 2 clicked"));
        //             menu.AddSeparator("");
        //             menu.AddItem(new GUIContent("Rename Tag"), false, () => Debug.Log("Option 3 clicked"));
        //             menu.AddItem(new GUIContent("Delete Tag"), false, () => Debug.Log("Option 4 clicked"));
        //             menu.ShowAsContext();
        //         }
        //     });

        //     // treeView.selectionChanged += (selectedItems) =>
        //     // {
        //     //     Debug.Log(selectedItems);
        //     // };

        //     GenericMenu.MenuFunction deleteTag = () =>
        //     {
        //         var tag = treeView.selectedIndex;
        //         var tagName = treeView.GetItemDataForIndex<string>(tag);
        //         defaultGameplayTags.GameplayTagList.Remove(tagName);
        //         EditorUtility.SetDirty(defaultGameplayTags);
        //         AssetDatabase.SaveAssets();
        //         treeView.Rebuild();
        //     };



        //     // // Callback invoked when the user double clicks an item
        //     // treeView.itemsChosen += (selectedItems) =>
        //     // {
        //     //     Debug.Log("Items chosen: " + string.Join(", ", selectedItems));
        //     // };

        //     // // Callback invoked when the user changes the selection inside the TreeView
        //     // treeView.selectedIndicesChanged += (selectedItems) =>
        //     // {
        //     //     Debug.Log("Items selected: " + string.Join(", ", selectedItems));
        //     // };

        // }
    }
}
#endif