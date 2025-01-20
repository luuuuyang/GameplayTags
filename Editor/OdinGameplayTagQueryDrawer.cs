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
    public class OdinGameplayTagQueryDrawer : OdinValueDrawer<GameplayTagQuery>
    {
        public delegate void OnTagQueryChangedDelegate(in GameplayTagQuery tagQuery);
        public bool ReadOnly;
        public OnTagQueryChangedDelegate OnTagQueryChanged;
        public List<GameplayTagQuery> CachedQueries = new();
        private string QueryDescription;
        private string QueryDescriptionTooltip;

        protected override void Initialize()
        {
            CacheQueryList();
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            SirenixEditorGUI.BeginHorizontalPropertyLayout(label);
            {
                if (SirenixEditorGUI.Button(QueryDescription, ButtonSizes.Medium))
                {
                    OnEditButtonClicked();
                }

                if (SirenixEditorGUI.IconButton(EditorIcons.Pen))
                {
                    OnEditButtonClicked();
                }

                if (SirenixEditorGUI.IconButton(EditorIcons.X))
                {
                    OnClearAllButtonClicked();
                }
            }
            SirenixEditorGUI.EndHorizontalPropertyLayout();
        }

        private void OnEditButtonClicked()
        {
            GameplayTagQueryWindowArgs args = new()
            {
                OnQueriesCommitted = OnQueriesCommitted,
                EditableQueries = CachedQueries,
                ReadOnly = ReadOnly,
            };
            EditorWindow.GetWindow<GameplayTagQueryWindow>().OpenWindow(args);
        }

        private void OnClearAllButtonClicked()
        {
            Property.ValueEntry.WeakSmartValue = GameplayTagQuery.EmptyQuery;
            Property.ValueEntry.ApplyChanges();

            for (int i = 0; i < CachedQueries.Count; i++)
            {
                CachedQueries[i] = GameplayTagQuery.EmptyQuery;
            }

            if (!CachedQueries.IsEmpty())
            {
                OnTagQueryChanged?.Invoke(CachedQueries[0]);
            }

            CacheQueryList();
        }

        public void CacheQueryList()
        {
            CachedQueries.Clear();

            GameplayTagQuery query = Property.ValueEntry.WeakSmartValue as GameplayTagQuery;
            if (query is not null)
            {
                CachedQueries.Add(query);
            }

            QueryDescription = "Empty";
            QueryDescriptionTooltip = "Empty Gameplay Tag Query";

            bool allSame = true;
            for (int i = 1; i < CachedQueries.Count; i++)
            {
                if (CachedQueries[i] != CachedQueries[0])
                {
                    allSame = false;
                    break;
                }
            }

            if (!allSame)
            {
                QueryDescription = "Multiple Selected";
                QueryDescriptionTooltip = QueryDescription;
            }
            else if (CachedQueries.Count == 1)
            {
                GameplayTagQuery theQuery = CachedQueries[0];
                string desc = theQuery.Description;

                if (string.IsNullOrEmpty(desc) && !theQuery.IsEmpty())
                {
                    EditableGameplayTagQuery editableQuery = theQuery.CreateEditableQuery();

                    GameplayTagQuery tempQueryForDescription = new();
                    tempQueryForDescription.BuildFromEditableQuery(editableQuery);
                    desc = tempQueryForDescription.Description;
                }

                if (desc.Length > 0)
                {
                    QueryDescription = desc;
                    QueryDescriptionTooltip = GameplayTagEditorUtilities.FormatGameplayTagQueryDescriptionToLines(desc);
                }
            }
        }

        public void OnQueriesCommitted(in List<GameplayTagQuery> tagQueries)
        {
            Property.ValueEntry.WeakSmartValue = tagQueries[0];
            Property.ValueEntry.ApplyChanges();

            CachedQueries = tagQueries;

            if (!tagQueries.IsEmpty())
            {
                OnTagQueryChanged?.Invoke(tagQueries[0]);
            }

            CacheQueryList();
        }
    }
}
#endif