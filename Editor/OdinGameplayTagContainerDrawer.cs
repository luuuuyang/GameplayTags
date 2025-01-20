using GameplayTags;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;

namespace GameplayTags.Editor
{
    public class OdinGameplayTagContainerDrawer : OdinValueDrawer<GameplayTagContainer>
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

        protected override void DrawPropertyLayout(GUIContent label)
        {
            // SirenixEditorGUI.BeginBox();
            SirenixEditorGUI.BeginHorizontalPropertyLayout(label);

            SirenixEditorGUI.BeginVerticalList();

            if (TagsToEdit.Count <= 0)
            {
                if (SirenixEditorGUI.Button("Empty", ButtonSizes.Medium))
                {
                    OnGetMenuContent();
                }
            }

            foreach (EditableItem item in TagsToEdit)
            {
                EditorGUILayout.BeginHorizontal();
                if (SirenixEditorGUI.Button(item.Tag.TagName, ButtonSizes.Medium))
                {
                    OnGetMenuContent();
                }
                if (SirenixEditorGUI.IconButton(EditorIcons.X))
                {
                    OnClearTagClicked(item.Tag);
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }

            SirenixEditorGUI.EndVerticalList();

            SirenixEditorGUI.EndHorizontalPropertyLayout();
            // SirenixEditorGUI.EndBox();
        }

        protected override void Initialize()
        {
            RefreshTagContainer();
        }

        public void RefreshTagContainer()
        {
            CachedTagContainers.Reset();
            TagsToEdit.Reset();

            GameplayTagPicker.EnumerateEditableTagContainersFromProperty(Property, (tagContainer) =>
            {
                CachedTagContainers.Add(tagContainer);
                foreach (GameplayTag tag in tagContainer)
                {
                    int existingItemIndex = TagsToEdit.FindIndex(item => item.Tag == tag);
                    if (existingItemIndex != -1)
                    {
                        TagsToEdit[existingItemIndex].Count++;
                    }
                    else
                    {
                        TagsToEdit.Add(new EditableItem(tag));
                    }
                }
            });

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

        private void OnGetMenuContent()
        {
            List<GameplayTagContainer> tagContainersToEdit = new();

            // PopupWindow.Show(EditorGUILayout.GetControlRect(), new GameplayTagPicker
            // {
            //     Property = Property,
            //     MultiSelect = true,
            //     TagContainers = tagContainersToEdit,
            //     OnTagChanged = OnTagChanged
            // });

            OdinGameplayTagPicker.ShowWindow(EditorGUILayout.GetControlRect(), true, Property, OnTagChanged, tagContainersToEdit);
        }

        private void OnTagChanged(in List<GameplayTagContainer> tagContainers)
        {
            CachedTagContainers = tagContainers;

            if (!tagContainers.IsEmpty())
            {
                // OnTagContainerChanged?.Invoke();
            }

            RefreshTagContainer();
        }

        private void OnClearTagClicked(GameplayTag tagToClear)
        {
            foreach (var tagContainer in CachedTagContainers)
            {
                tagContainer.RemoveTag(tagToClear);
            }

            if (!CachedTagContainers.IsEmpty())
            {
                // OnTagContainerChanged?.Invoke();
            }

            RefreshTagContainer();
        }
    }
}
#endif