using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;

namespace GameplayTags.Editor
{
    public class OdinGameplayTagPicker : OdinEditorWindow
    {
        public delegate void TagChangedEventHandler(in List<GameplayTagContainer> tagContainers);

        public delegate void GameplayTagAddedEventHandler(in string tagName, in string tagComment, in string tagSource);

        [HideInInspector]
        public TagChangedEventHandler OnTagChanged;
        [HideInInspector]
        public GameplayTagAddedEventHandler OnGameplayTagAdded;
        [HideInInspector]
        public bool MultiSelect = true;

        [HideInInspector]
        public bool ReadOnly = false;

        [HideInInspector]
        public GameplayTagPickerMode Mode = GameplayTagPickerMode.SelectionMode;

        [HideInInspector]
        public string SearchText = "";

        [HideInInspector]
        public Vector2 ScrollPosition;

        [HideInInspector]
        public List<GameplayTagContainer> TagContainers = new();
        [HideInInspector]
        public List<GameplayTagNode> TagItems = new();
        [HideInInspector]
        public List<GameplayTagNode> CachedExpandedItems = new();
        [HideInInspector]
        public InspectorProperty Property;
        private GUIStyle iconStyle;

        bool NewTagWidgetVisible = false;

        [SerializeField]
        [ShowIf("NewTagWidgetVisible")]
        [PropertyOrder(3)]
        private string Name;

        [SerializeField]
        [ShowIf("NewTagWidgetVisible")]
        [PropertyOrder(3)]
        private string Comment;

        [SerializeField]
        [ShowIf("NewTagWidgetVisible")]
        [PropertyOrder(3)]
        [ValueDropdown(nameof(TagSourceOptions))]
        private string TagSource;

        private List<string> TagSourceOptions = new() { "DefaultGameplayTags.asset" };

        private bool CanSelectTags => !ReadOnly && (Mode == GameplayTagPickerMode.SelectionMode || Mode == GameplayTagPickerMode.HybridMode);

        public static OdinGameplayTagPicker ShowWindow(
            Rect buttonRect,
            bool multiSelect,
            InspectorProperty property,
            TagChangedEventHandler callback,
            List<GameplayTagContainer> tagContainers,
            GameplayTagPickerMode mode = GameplayTagPickerMode.SelectionMode)
        {
            var window = CreateInstance<OdinGameplayTagPicker>();
            window.MultiSelect = multiSelect;
            window.Property = property;
            window.OnTagChanged = callback;
            window.TagContainers = tagContainers;
            window.Mode = mode;
            window.position = new Rect(buttonRect.x, buttonRect.y + buttonRect.height, 300, 400);

            if (window.Property != null)
            {
                GetEditableTagContainersFromProperty(window.Property, window.TagContainers);
            }
            window.GetFilteredGameplayRootTags(window.TagItems);


            window.Show();

            return window;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            titleContent = new GUIContent("Select Tags");

            // 初始化样式
            iconStyle = new GUIStyle() { margin = new RectOffset(5, 0, 4, 0) };
        }

        [PropertyOrder(4)]
        [OnInspectorGUI]
        private void DrawTagTree()
        {
            SirenixEditorGUI.BeginVerticalList();
            {
                ScrollPosition = EditorGUILayout.BeginScrollView(ScrollPosition);
                {
                    foreach (GameplayTagNode node in TagItems)
                    {
                        DrawNode(node);
                    }
                }
                EditorGUILayout.EndScrollView();
            }
            SirenixEditorGUI.EndVerticalList();
        }

        public static bool GetEditableTagContainersFromProperty(InspectorProperty property, List<GameplayTagContainer> editableTagContainers)
        {
            editableTagContainers.Reset();
            return EnumerateEditableTagContainersFromProperty(property, container => editableTagContainers.Add(container));
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

        public void GetFilteredGameplayRootTags(List<GameplayTagNode> nodes)
        {
            nodes.Clear();
            GameplayTagsManager.Instance.GetFilteredGameplayRootTags(nodes);
        }

        [PropertyOrder(2)]
        [OnInspectorGUI]
        private void DrawToolbar()
        {
            SirenixEditorGUI.BeginHorizontalToolbar();
            {
                if (SirenixEditorGUI.IconButton(EditorIcons.Plus, 18, 18, "Add New GameplayTag"))
                {
                    NewTagWidgetVisible = !NewTagWidgetVisible;
                }

                // 搜索框
                var newSearchText = SirenixEditorGUI.ToolbarSearchField(SearchText);
                if (newSearchText != SearchText)
                {
                    SearchText = newSearchText;
                }

                SirenixEditorGUI.IconButton(EditorIcons.Pen);
            }
            SirenixEditorGUI.EndHorizontalToolbar();
        }

        private void DrawNode(GameplayTagNode node)
        {
            SirenixEditorGUI.BeginListItem();
            {
                // 1. 开始水平布局
                SirenixEditorGUI.BeginIndentedHorizontal();
                {
                    // 2. 展开/折叠按钮
                    if (node.ChildTagNodes.Count > 0)
                    {
                        EditorIcon icon = IsTagExpanded(node) ? EditorIcons.TriangleDown : EditorIcons.TriangleRight;
                        SirenixEditorGUI.IconButton(icon, iconStyle);
                    }
                    else
                    {
                        // 空白占位
                        SirenixEditorGUI.IconButton(EditorIcons.Transparent, iconStyle);
                    }

                    // 3. 复选框
                    if (CanSelectTags)
                    {
                        bool newSelected = EditorGUILayout.Toggle(IsTagChecked(node), GUILayout.Width(16));
                        if (newSelected != IsTagChecked(node))
                        {
                            OnTagCheckStatusChanged(newSelected, node);
                        }
                    }

                    // 4. 标签名
                    GUILayout.Label(node.CompleteTag.TagName);
                }
                SirenixEditorGUI.EndIndentedHorizontal();
            }
            SirenixEditorGUI.EndListItem();

            // 6. 递归绘制子节点
            if (node.ChildTagNodes.Count > 0)
            {
                EditorGUI.indentLevel++;
                if (true)
                {
                    foreach (GameplayTagNode child in node.ChildTagNodes)
                    {
                        DrawNode(child);
                    }
                }
                EditorGUI.indentLevel--;
            }
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

        public void OnTagCheckStatusChanged(bool newValue, GameplayTagNode nodeChanged)
        {
            if (newValue == true)
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
                foreach (GameplayTagContainer container in TagContainers)
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

        public bool IsTagExpanded(GameplayTagNode node)
        {
            return true;
        }

        public void SetTagNodeItemExpanded(GameplayTagNode node, bool expand)
        {
            if (node is not null)
            {
                foreach (var item in node.ChildTagNodes)
                {
                    SetTagNodeItemExpanded(item, expand);
                }
            }
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

            OnTagChanged?.Invoke(TagContainers);
        }

        [PropertyOrder(0)]
        [Button(Icon = SdfIconType.X, IconAlignment = IconAlignment.LeftOfText, Name = "Clear Selection")]
        [ShowIf("Mode", GameplayTagPickerMode.SelectionMode)]
        private void OnClearAllClicked()
        {
            foreach (GameplayTagContainer container in TagContainers)
            {
                container.Reset();
            }

            OnContainersChanged();
        }

        [PropertyOrder(1)]
        [Button(Icon = SdfIconType.Gear, IconAlignment = IconAlignment.LeftOfText, Name = "Manage Gameplay Tags...")]
        [ShowIf("Mode", GameplayTagPickerMode.SelectionMode)]
        private void OnManageTagsClicked()
        {
            var window = CreateInstance<OdinGameplayTagPicker>();
            if (window.Property != null)
            {
                GetEditableTagContainersFromProperty(window.Property, window.TagContainers);
            }
            window.GetFilteredGameplayRootTags(window.TagItems);
            window.Mode = GameplayTagPickerMode.ManagementMode;
            window.Show();
        }

        [PropertyOrder(3)]
        [Button(Name = "Add New Tag", Stretch = false, ButtonAlignment = 1f)]
        [ShowIf("NewTagWidgetVisible")]
        private void OnAddNewTagButtonPressed()
        {
            CreateNewGameplayTag();
        }

        private void CreateNewGameplayTag()
        {
            if (TagSource is null)
            {
                Debug.LogError("You must specify a source file for gameplay tags.");
                return;
            }

            string TagName = Name;
            string TagComment = Comment;

            if (string.IsNullOrEmpty(TagName))
            {
                Debug.LogError("You must specify tag name.");
                return;
            }

            if (!GameplayTagsManager.Instance.IsValidGameplayTagString(TagName, out string error, out string fixedString))
            {
                Debug.LogError($"Invalid Gameplay Tag: {TagName}");
                return;
            }

            IGameplayTagsEditorModule.Instance.AddNewGameplayTagToINI(TagName, TagComment, TagSource);

            OnGameplayTagAdded?.Invoke(TagName, TagComment, TagSource);
        }
    }
}
#endif