using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector;

namespace GameplayTags.Editor
{
    public delegate void OnQueriesCommittedDelegate(in List<GameplayTagQuery> tagQueries);

    public class OdinGameplayTagQueryWindow : OdinEditorWindow
    {
        private bool ReadOnly;
        private List<GameplayTagQuery> TagQueries;
        private OnQueriesCommittedDelegate OnQueriesCommitted;
        private Action OnOK;
        private Action OnCancel;
        [SerializeField] private EditableGameplayTagQuery EditableQuery;
        private List<GameplayTagQuery> OriginalTagQueries;

        public void OpenGameplayTagQueryWindow(GameplayTagQueryWindowArgs args)
        {
            TagQueries = args.EditableQueries;

            ReadOnly = args.ReadOnly;
            OnQueriesCommitted = args.OnQueriesCommitted;

            EditableGameplayTagQuery editableGameplayTagQuery = CreateEditableQuery(TagQueries[0]);
            EditableQuery = editableGameplayTagQuery;

            OriginalTagQueries = TagQueries;

            OnOK += Close;
            OnCancel += Close;
        }

        public EditableGameplayTagQuery CreateEditableQuery(GameplayTagQuery Q)
        {
            EditableGameplayTagQuery editableQuery = Q.CreateEditableQuery();
            return editableQuery;
        }

        public void SaveToTagQuery()
        {
            if (EditableQuery == null || ReadOnly)
            {
                return;
            }

            GameplayTagQuery newQuery = new();

            EditableGameplayTagQuery currentEditableQuery = EditableQuery;
            if (currentEditableQuery != null)
            {
                if (currentEditableQuery.RootExpression != null)
                {
                    newQuery.BuildFromEditableQuery(currentEditableQuery);
                }
            }

            for (int i = 0; i < TagQueries.Count; i++)
            {
                TagQueries[i] = newQuery;
            }
        }

        [Button("OK")]
        [HorizontalGroup()]
        public void OnOkClicked()
        {
            if (!ReadOnly)
            {
                SaveToTagQuery();

                OnQueriesCommitted?.Invoke(TagQueries);
                OnOK?.Invoke();
            }
            else
            {
                OnCancel?.Invoke();
            }
        }

        [Button("Cancel")]
        [HorizontalGroup()]
        public void OnCancelClicked()
        {
            TagQueries = OriginalTagQueries;

            OnQueriesCommitted?.Invoke(TagQueries);
            OnCancel?.Invoke();
        }
    }

    public struct GameplayTagQueryWindowArgs
    {
        public string Title;
        public bool ReadOnly;
        public OnQueriesCommittedDelegate OnQueriesCommitted;
        public List<GameplayTagQuery> EditableQueries;
    };
}

#endif

