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
    public enum GameplayTagPickerMode
    {
        SelectionMode,
        ManagementMode,
        HybridMode
    }

    public class OdinGameplayTagPicker : OdinEditorWindow
    {
        public delegate void TagChangedEventHandler(in List<GameplayTagContainer> tagContainers);
        public delegate void GameplayTagAddedEventHandler(in string tagName, in string tagComment, in string tagSource);
        public delegate void OnRefreshTagContainers(OdinGameplayTagPicker tagPicker);

        private TagChangedEventHandler OnTagChanged;
        private OnRefreshTagContainers onRefreshTagContainers;
        private bool MultiSelect = true;
        private bool ReadOnly = false;
        private GameplayTagPickerMode Mode = GameplayTagPickerMode.SelectionMode;
        private string SearchText = "";
        private Vector2 ScrollPosition;
        private List<GameplayTagContainer> TagContainers = new();
        private List<GameplayTagNode> TagItems = new();
        private List<GameplayTagNode> CachedExpandedItems = new();
        private InspectorProperty Property;
        private GUIStyle iconStyle;
        private bool NewTagWidgetVisible = false;
        private bool PersistExpansionChange = true;
        private List<string> TagSourceOptions = new();
        private bool CanSelectTags => !ReadOnly && (Mode == GameplayTagPickerMode.SelectionMode || Mode == GameplayTagPickerMode.HybridMode);

        [SerializeField]
        [ShowIf("NewTagWidgetVisible")]
        private string Name;

        [SerializeField]
        [ShowIf("NewTagWidgetVisible")]
        private string Comment;

        [SerializeField]
        [ShowIf("NewTagWidgetVisible")]
        [ValueDropdown(nameof(TagSourceOptions))]
        private string TagSource;

        [Button(Name = "Add New Tag", Stretch = false, ButtonAlignment = 1f)]
        [ShowIf("NewTagWidgetVisible")]
        private void OnAddNewTagButtonPressed()
        {
            CreateNewGameplayTag();
        }

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

        public static void ShowWindow(
            Rect buttonRect,
            bool multiSelect,
            InspectorProperty property,
            TagChangedEventHandler callback,
            List<GameplayTagContainer> tagContainers,
            GameplayTagPickerMode mode = GameplayTagPickerMode.SelectionMode)
        {
            OdinGameplayTagPicker window = GetWindow<OdinGameplayTagPicker>();
            window.MultiSelect = multiSelect;
            window.Property = property;
            window.OnTagChanged = callback;
            window.TagContainers = tagContainers;
            window.Mode = mode;
            window.position = new Rect(buttonRect.x, buttonRect.y + buttonRect.height, 300, 400);

            window.Show();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            titleContent = new GUIContent("Select Tags");

            // 初始化样式
            iconStyle = new GUIStyle() { margin = new RectOffset(5, 0, 4, 0) };
        }

        private void PopulateTagSources()
        {
            GameplayTagsManager manager = GameplayTagsManager.Instance;
            TagSourceOptions.Clear();

            string defaultSource = GameplayTagSource.DefaultName;

            TagSourceOptions.Add(defaultSource);

            List<GameplayTagSource> sources = new List<GameplayTagSource>();
            manager.FindTagSourcesWithType(GameplayTagSourceType.TagList, ref sources);

            foreach (GameplayTagSource source in sources)
            {
                if (source != null && source.SourceName != defaultSource)
                {
                    TagSourceOptions.Add(source.SourceName);
                }
            }
        }

        [OnInspectorInit]
        private void Init()
        {
            PopulateTagSources();

            if (Property != null)
            {
                GetEditableTagContainersFromProperty(Property, TagContainers);
            }

            GameplayTagsManager.OnEditorRefreshGameplayTagTree += () => EditorApplication.delayCall += RefreshTags;

            GetFilteredGameplayRootTags(TagItems);
        }

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

        private void DrawNode(GameplayTagNode node)
        {
            Rect rect = SirenixEditorGUI.BeginListItem();
            {
                // 检测右键点击
                if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
                {
                    if (rect.Contains(Event.current.mousePosition))
                    {
                        ShowContextMenu(node);
                        Event.current.Use(); // 标记事件已处理
                    }
                }

                // 1. 开始水平布局
                SirenixEditorGUI.BeginIndentedHorizontal();
                {
                    // 2. 展开/折叠按钮
                    if (node.ChildTagNodes.Count > 0)
                    {
                        EditorIcon icon = IsTagExpanded(node) ? EditorIcons.TriangleDown : EditorIcons.TriangleRight;

                        if (SirenixEditorGUI.IconButton(icon, iconStyle))
                        {
                            OnExpansionChanged(node, !IsTagExpanded(node));
                        }
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
                    GUILayout.Label(node.SimpleTagName);

                    // 5. 标签来源
                    GUILayout.Label(node.SourceNames.Count > 0 ? node.SourceNames[0] : "", node.IsExplicitTag ? SirenixGUIStyles.RightAlignedWhiteMiniLabel : SirenixGUIStyles.RightAlignedGreyMiniLabel);
                }
                SirenixEditorGUI.EndIndentedHorizontal();
            }
            SirenixEditorGUI.EndListItem();

            // 6. 递归绘制子节点
            if (node.ChildTagNodes.Count > 0)
            {
                EditorGUI.indentLevel++;
                if (IsTagExpanded(node))
                {
                    foreach (GameplayTagNode child in node.ChildTagNodes)
                    {
                        DrawNode(child);
                    }
                }
                EditorGUI.indentLevel--;
            }
        }

        private void OnTagChecked(GameplayTagNode nodeChecked)
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

        private void OnTagUnchecked(GameplayTagNode nodeUnchecked)
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

        private void UncheckChildren(GameplayTagNode nodeUnchecked, GameplayTagContainer editableContainer)
        {
            GameplayTag gameplayTag = nodeUnchecked.CompleteTag;
            editableContainer.RemoveTag(gameplayTag);

            foreach (GameplayTagNode childNode in nodeUnchecked.ChildTagNodes)
            {
                UncheckChildren(childNode, editableContainer);
            }
        }

        private void OnTagCheckStatusChanged(bool newValue, GameplayTagNode nodeChanged)
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

        private bool IsTagChecked(GameplayTagNode node)
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

        private bool IsTagExpanded(GameplayTagNode node)
        {
            return CachedExpandedItems.Contains(node);
        }

        private void OnExpansionChanged(GameplayTagNode item, bool isExpanded)
        {
            if (PersistExpansionChange)
            {

                if (isExpanded)
                {
                    CachedExpandedItems.Add(item);
                }
                else
                {
                    CachedExpandedItems.Remove(item);
                }
            }
        }

        private void OnContainersChanged()
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

        private void CreateNewGameplayTag()
        {
            GameplayTagsManager manager = GameplayTagsManager.Instance;

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

            if (!manager.IsValidGameplayTagString(TagName, out string error, out string fixedString))
            {
                Debug.LogError($"Invalid Gameplay Tag: {TagName}");
                return;
            }

            IGameplayTagsEditorModule.Instance.AddNewGameplayTagToINI(TagName, TagComment, TagSource);

            OnGameplayTagAdded(TagName, TagComment, TagSource);
        }

        private void OnGameplayTagAdded(in string tagName, in string tagComment, in string tagSource)
        {
            GameplayTagsManager manager = GameplayTagsManager.Instance;

            GameplayTagNode tagNode = manager.FindTagNode(tagName);
            GameplayTagNode parentTagNode = tagNode;

            while (parentTagNode is not null)
            {
                CachedExpandedItems.Add(parentTagNode);

                parentTagNode = parentTagNode.ParentTagNode;
            }

            RefreshTags();
        }

        private void RefreshTags()
        {
            GameplayTagsManager manager = GameplayTagsManager.Instance;
            manager.GetFilteredGameplayRootTags(TagItems);

            onRefreshTagContainers?.Invoke(this);
        }

        private void ShowContextMenu(GameplayTagNode node)
        {
            GenericMenu menu = new GenericMenu();

            // 添加菜单项
            menu.AddItem(new GUIContent("Rename"), false, () => OnRenameTag(node));
            menu.AddItem(new GUIContent("Delete"), false, () => OnDeleteTag(node));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Copy Tag Name"), false, () => OnCopyTagName(node));

            // 显示菜单
            menu.ShowAsContext();
        }

        private void OnRenameTag(GameplayTagNode node)
        {
            OdinRenameGameplayTagWindow.ShowWindow(node, OnGameplayTagRenamed);
        }

        private void OnDeleteTag(GameplayTagNode node)
        {
            if (node is not null)
            {
                IGameplayTagsEditorModule tagsEditor = IGameplayTagsEditorModule.Instance;

                bool tagRemoved = false;
                if (Mode == GameplayTagPickerMode.HybridMode)
                {
                    foreach (GameplayTagContainer container in TagContainers)
                    {
                        tagRemoved |= container.RemoveTag(node.CompleteTag);

                    }
                }

                bool deleted = tagsEditor.DeleteTagFromINI(node);

                if (tagRemoved || deleted)
                {
                    OnTagChanged?.Invoke(TagContainers);
                }
            }
        }

        private void OnCopyTagName(GameplayTagNode node)
        {
            Debug.Log("Copy Tag Name");
        }

        private void OnGameplayTagRenamed(string oldName, string newName)
        {
            OnTagChanged?.Invoke(TagContainers);
        }
    }
}
#endif