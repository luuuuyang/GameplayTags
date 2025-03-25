using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;

namespace GameplayTags.Editor
{
    public class OdinGameplayTagContainerDrawer : OdinValueDrawer<GameplayTagContainer>
    {
        public delegate void OnTagContainerChangedDelegate(in GameplayTagContainer tagContainer);

        private class EditableItem
        {
            public GameplayTag Tag;
            public int Count;
            public bool MultipleValues;
            public EditableItem() { }
            public EditableItem(GameplayTag tag, int count = 1)
            {
                Tag = tag;
                Count = count;
            }
        }

        private List<GameplayTagContainer> CachedTagContainers = new();
        private List<EditableItem> TagsToEdit = new();
        private OnTagContainerChangedDelegate OnTagContainerChanged;

        protected override void DrawPropertyLayout(GUIContent label)
        {
            SirenixEditorGUI.BeginHorizontalPropertyLayout(label);
            {
                SirenixEditorGUI.BeginVerticalList();
                {
                    if (TagsToEdit.Count <= 0)
                    {
                        if (SirenixEditorGUI.Button("Empty", ButtonSizes.Medium))
                        {
                            OnGetMenuContent();
                        }
                    }

                    for (int i = 0; i < TagsToEdit.Count; i++)
                    {
                        EditableItem item = TagsToEdit[i];
                        EditorGUILayout.BeginHorizontal();
                        {
                            if (SirenixEditorGUI.Button(item.Tag.TagName, ButtonSizes.Medium))
                            {
                                OnGetMenuContent();
                            }
                            if (SirenixEditorGUI.IconButton(EditorIcons.X))
                            {
                                OnClearTagClicked(item.Tag);
                                // break;
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
                SirenixEditorGUI.EndVerticalList();
            }
            SirenixEditorGUI.EndHorizontalPropertyLayout();
        }

        protected override void Initialize()
        {
            RefreshTagContainer();
        }

        private void RefreshTagContainer()
        {
            CachedTagContainers.Reset();
            TagsToEdit.Reset();

            OdinGameplayTagPicker.EnumerateEditableTagContainersFromProperty(Property, (tagContainer) =>
            {
                CachedTagContainers.Add(tagContainer);

                foreach (GameplayTag tag in tagContainer)
                {
                    int existingItemIndex = TagsToEdit.FindIndex(item => item != null && item.Tag == tag);
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
                Debug.Assert(item != null);
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

            OdinGameplayTagPicker.ShowWindow(EditorGUILayout.GetControlRect(), true, Property, OnTagChanged, tagContainersToEdit);
        }

        private void OnTagChanged(in List<GameplayTagContainer> tagContainers)
        {
            CachedTagContainers = tagContainers;

            if (!tagContainers.IsEmpty())
            {
                OnTagContainerChanged?.Invoke(tagContainers[0]);
            }

            // this.ValueEntry.Values.ForceMarkDirty();
            

            RefreshTagContainer();
        }

        private void OnClearTagClicked(GameplayTag tagToClear)
        {
            GameplayTagContainer newValues = new();
            OdinGameplayTagPicker.EnumerateEditableTagContainersFromProperty(Property, (editableTagContainer) =>
            {
                GameplayTagContainer tagContainerCopy = editableTagContainer;
                tagContainerCopy.RemoveTag(tagToClear);

                newValues.AppendTags(tagContainerCopy);
            });

            Property.ValueEntry.WeakSmartValue = newValues;

            foreach (GameplayTagContainer tagContainer in CachedTagContainers)
            {
                tagContainer.RemoveTag(tagToClear);
            }

            if (!CachedTagContainers.IsEmpty())
            {
                OnTagContainerChanged?.Invoke(CachedTagContainers[0]);
            }

            RefreshTagContainer();
        }
    }
}