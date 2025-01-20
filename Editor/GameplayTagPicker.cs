using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;

namespace GameplayTags.Editor
{
    public enum GameplayTagPickerMode
    {
        SelectionMode,
        ManagementMode,
        HybridMode
    }

    public class GameplayTagPicker : PopupWindowContent
    {
        public delegate void TagChangedEventHandler(in List<GameplayTagContainer> tagContainers);
        public delegate void OnRefreshTagContainers(GameplayTagPicker tagPicker);
        public List<TreeViewItemData<GameplayTagNode>> TagItems = new();
        public List<GameplayTagNode> CachedExpandedItems = new();

        public bool ReadOnly = false;
        public bool MultiSelect = true;
        public GameplayTagPickerMode GameplayTagPickerMode = GameplayTagPickerMode.SelectionMode;
        public List<GameplayTagContainer> TagContainers = new();
        public InspectorProperty Property;
        public TagChangedEventHandler OnTagChanged;

        private TreeView TreeViewWidget;

        public GameplayTagPicker(GameplayTagPickerMode mode = GameplayTagPickerMode.SelectionMode)
        {
            GameplayTagPickerMode = mode;
        }

        public override VisualElement CreateGUI()
        {
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/GameplayTagPicker.uxml");
            TemplateContainer root = visualTree.Instantiate();

            if (Property != null)
            {
                GetEditableTagContainersFromProperty(Property, TagContainers);
            }
            GetFilteredGameplayRootTags(TagItems);

            TreeViewWidget = root.Q<TreeView>();
            TreeViewWidget.SetRootItems(TagItems);

            TreeViewWidget.makeItem = () =>
            {
                if (GameplayTagPickerMode == GameplayTagPickerMode.ManagementMode)
                    return new Label();
                else
                    return new Toggle();
            };
            TreeViewWidget.bindItem = (visualElement, index) =>
            {
                if (GameplayTagPickerMode == GameplayTagPickerMode.ManagementMode)
                {
                    Label label = visualElement.Q<Label>();
                    GameplayTagNode itemData = TreeViewWidget.GetItemDataForIndex<GameplayTagNode>(index);
                    label.text = itemData.SimpleTagName;
                }
                else
                {
                    Toggle toggle = visualElement.Q<Toggle>();
                    GameplayTagNode itemData = TreeViewWidget.GetItemDataForIndex<GameplayTagNode>(index);
                    toggle.SetValueWithoutNotify(IsTagChecked(itemData));
                    toggle.text = itemData.SimpleTagName;
                    toggle.RegisterCallback<ChangeEvent<bool>, GameplayTagNode>(OnTagCheckStatusChanged, itemData);
                }
            };
            TreeViewWidget.unbindItem = (visualElement, index) =>
            {
                if (GameplayTagPickerMode == GameplayTagPickerMode.ManagementMode)
                {

                }
                else
                {
                    Toggle toggle = visualElement.Q<Toggle>();
                    toggle.UnregisterCallback<ChangeEvent<bool>, GameplayTagNode>(OnTagCheckStatusChanged);
                }
            };
            TreeViewWidget.Rebuild();

            return root;
        }

        public static bool EnumerateEditableTagContainersFromProperty(InspectorProperty property, Action<GameplayTagContainer> callback)
        {
            if (property.ValueEntry.TypeOfValue == typeof(GameplayTagContainer))
            {
                callback((GameplayTagContainer)property.ValueEntry.WeakSmartValue);
                return true;
            }
            if (property.ValueEntry.TypeOfValue == typeof(GameplayTag))
            {
                GameplayTagContainer container = new();
                container.AddTag((GameplayTag)property.ValueEntry.WeakSmartValue);
                callback(container);
                return true;
            }
            return false;
        }

        public static bool GetEditableTagContainersFromProperty(InspectorProperty property, List<GameplayTagContainer> editableTagContainers)
        {
            editableTagContainers.Reset();
            return EnumerateEditableTagContainersFromProperty(property, container => editableTagContainers.Add(container));
        }

        public void GetFilteredGameplayRootTags(List<TreeViewItemData<GameplayTagNode>> nodes)
        {
            nodes.Clear();
            GameplayTagsManager.Instance.GetFilteredGameplayRootTags(nodes);
        }


        public void OnTagChecked(GameplayTagNode nodeChecked)
        {
            foreach (GameplayTagContainer container in TagContainers)
            {
                GameplayTagNode curNode = nodeChecked;
                bool removeParents = false;
                while (curNode is not null)
                {
                    GameplayTag gameplayTag = curNode.CompleteTag;
                    if (removeParents == false)
                    {
                        removeParents = true;
                        if (MultiSelect == false)
                        {
                            container.Reset();
                        }
                        container.AddTag(gameplayTag);
                    }
                    else
                    {
                        container.RemoveTag(gameplayTag);
                    }
                    curNode = curNode.ParentTagNode;
                }
            }
            OnContainersChanged();
        }

        public void OnTagUnchecked(GameplayTagNode nodeUnchecked)
        {
            if (nodeUnchecked is not null)
            {
                foreach (GameplayTagContainer container in TagContainers)
                {
                    GameplayTag gameplayTag = nodeUnchecked.CompleteTag;
                    container.RemoveTag(gameplayTag);
                    GameplayTagNode parentNode = nodeUnchecked.ParentTagNode;
                    if (parentNode is not null)
                    {
                        bool otherSiblings = false;
                        foreach (GameplayTagNode sibling in parentNode.ChildTagNodes)
                        {
                            gameplayTag = sibling.CompleteTag;
                            if (container.HasTagExact(gameplayTag))
                            {
                                otherSiblings = true;
                                break;
                            }
                        }
                        if (!otherSiblings)
                        {
                            gameplayTag = parentNode.CompleteTag;
                            container.AddTag(gameplayTag);
                        }
                    }
                    foreach (GameplayTagNode childNode in nodeUnchecked.ChildTagNodes)
                    {
                        UncheckChildren(childNode, container);
                    }
                }
                OnContainersChanged();
            }
        }

        public void UncheckChildren(GameplayTagNode nodeUnchecked, GameplayTagContainer editableContainer)
        {
            GameplayTag gameplayTag = nodeUnchecked.CompleteTag;
            editableContainer.RemoveTag(gameplayTag);

            foreach (GameplayTagNode childNode in nodeUnchecked.ChildTagNodes)
            {
                UncheckChildren(childNode, editableContainer);
            }
        }

        public void OnTagCheckStatusChanged(ChangeEvent<bool> evt, GameplayTagNode nodeChanged)
        {
            if (evt.newValue == true)
            {
                OnTagChecked(nodeChanged);
            }
            else
            {
                OnTagUnchecked(nodeChanged);
            }
        }

        public bool IsTagChecked(GameplayTagNode node)
        {
            int numValidAssets = 0;
            int numAssetsTagIsAppliedTo = 0;

            if (node is not null)
            {
                foreach (var container in TagContainers)
                {
                    numValidAssets++;
                    GameplayTag gameplayTag = node.CompleteTag;
                    if (gameplayTag.IsValid())
                    {
                        if (container.HasTag(gameplayTag))
                        {
                            numAssetsTagIsAppliedTo++;
                        }
                    }
                }
            }

            if (numAssetsTagIsAppliedTo == 0)
            {
                return false;
            }
            else if (numValidAssets == numAssetsTagIsAppliedTo)
            {
                return true;
            }
            return false;
        }

        public void OnContainersChanged()
        {
            if (Property != null && MultiSelect)
            {
                Property.ValueEntry.WeakSmartValue = TagContainers[0];
            }
            else if (Property != null && !MultiSelect)
            {
                Property.ValueEntry.WeakSmartValue = TagContainers[0].First();
            }
            Property.ValueEntry.ApplyChanges();
            TreeViewWidget.RefreshItems();

            OnTagChanged?.Invoke(TagContainers);
        }
    }
}
#endif