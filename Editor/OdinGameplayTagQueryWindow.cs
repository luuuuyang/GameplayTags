using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

#if ODIN_INSPECTOR

using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector;

namespace GameplayTags.Editor
{
    public class OdinGameplayTagQueryWindow : OdinEditorWindow
    {
        public delegate void OnQueriesCommittedDelegate(in List<GameplayTagQuery> tagQueries);

        [HideInInspector]
        public bool ReadOnly;

        [HideInInspector]
        public List<GameplayTagQuery> TagQueries;

        [HideInInspector]
        public OnQueriesCommittedDelegate OnQueriesCommitted;

        [HideInInspector]
        public Action OnOK;

        [HideInInspector]
        public Action OnCancel;

        public EditableGameplayTagQuery EditableQuery;

        [HideInInspector]
        public List<GameplayTagQuery> OriginalTagQueries;


        public void OpenWindow(GameplayTagQueryWindowArgs args)
        {
            TagQueries = args.EditableQueries;

            ReadOnly = args.ReadOnly;
            OnQueriesCommitted = args.OnQueriesCommitted;

            EditableGameplayTagQuery editableGameplayTagQuery = CreateEditableQuery(TagQueries[0]);
            EditableQuery = editableGameplayTagQuery;

            OriginalTagQueries = TagQueries;
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
        public GameplayTagQueryWindow.OnQueriesCommittedDelegate OnQueriesCommitted;
        public List<GameplayTagQuery> EditableQueries;
    };
}

#endif

