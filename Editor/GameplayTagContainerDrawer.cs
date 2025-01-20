using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameplayTags.Editor
{
    // [CustomPropertyDrawer(typeof(GameplayTagContainer))]
    public class GameplayTagContainerDrawer : PropertyDrawer
    {
        private class EditableItem
        {
            public GameplayTag Tag;
            public int Count;
            public bool MultipleValues;

            public EditableItem(GameplayTag tag, int count = 1)
            {
                Tag = tag;
                Count = count;
            }
        }

        private List<GameplayTagContainer> CachedTagContainers = new();
        private List<EditableItem> TagsToEdit = new();
        private ListView TagListView;
        private Button EmptyButton;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            RefreshTagContainer(property);

            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/GameplayTagContainerDrawer.uxml");
            TemplateContainer root = visualTree.Instantiate();

            Label label = root.Q<Label>();
            label.text = preferredLabel;

            EmptyButton = root.Q<Button>();
            EmptyButton.RegisterCallback<ClickEvent, SerializedProperty>(OnGetMenuContent, property);
            EmptyButton.style.display = TagsToEdit.Count > 0 ? DisplayStyle.None : DisplayStyle.Flex;

            TagListView = root.Q<ListView>();
            TagListView.itemsSource = TagsToEdit;
            TagListView.makeItem = () => new Button();
            TagListView.bindItem = (element, index) =>
            {
                Button button = element.Q<Button>();
                EditableItem item = TagsToEdit[index];
                button.text = item.Tag.TagName;
                button.RegisterCallback<ClickEvent, SerializedProperty>(OnGetMenuContent, property);
            };
            TagListView.unbindItem = (element, index) =>
            {
                Button button = element.Q<Button>();
                button.UnregisterCallback<ClickEvent, SerializedProperty>(OnGetMenuContent);
            };
            TagListView.style.display = TagsToEdit.Count <= 0 ? DisplayStyle.None : DisplayStyle.Flex;

            return root;
        }

        public void RefreshTagContainer(SerializedProperty property)
        {
            CachedTagContainers.Reset();
            TagsToEdit.Reset();

            // GameplayTagPicker.EnumerateEditableTagContainersFromProperty(property, (tagContainer) =>
            // {
            //     CachedTagContainers.Add(tagContainer);
            //     foreach (GameplayTag tag in tagContainer)
            //     {
            //         int existingItemIndex = TagsToEdit.FindIndex(item => item.Tag == tag);
            //         if (existingItemIndex != -1)
            //         {
            //             TagsToEdit[existingItemIndex].Count++;
            //         }
            //         else
            //         {
            //             TagsToEdit.Add(new EditableItem(tag));
            //         }
            //     }
            // });

            int propertyCount = CachedTagContainers.Count;
            foreach (EditableItem item in TagsToEdit)
            {
                if (item.Count != propertyCount)
                {
                    item.MultipleValues = true;
                }
            }

            TagsToEdit.Sort((a, b) => a.Tag.CompareTo(b.Tag));
        }

        private void OnGetMenuContent(ClickEvent evt, SerializedProperty property)
        {
            List<GameplayTagContainer> tagContainersToEdit = new();

            UnityEditor.PopupWindow.Show((evt.target as Button).worldBound, new GameplayTagPicker
            {
                // Property = property,
                MultiSelect = true,
                TagContainers = tagContainersToEdit,
                // OnTagChanged = (containers) =>
                // {
                //     RefreshTagContainer(property);
                //     TagListView.RefreshItems();

                //     EmptyButton.style.display = TagsToEdit.Count > 0 ? DisplayStyle.None : DisplayStyle.Flex;
                //     TagListView.style.display = TagsToEdit.Count <= 0 ? DisplayStyle.None : DisplayStyle.Flex;
                // }
            });
        }
    }
}