using GameplayTags;
using System;
using UnityEditor;
using UnityEngine;

namespace GameplayTags.Editor
{
    public class IGameplayTagsEditorModule : Singleton<IGameplayTagsEditorModule>
    {
        public bool AddNewGameplayTagToINI(in string newTag, in string comment, in string tagSourceName)
        {
            if (string.IsNullOrEmpty(newTag))
            {
                return false;
            }

            if (!GameplayTagsManager.Instance.IsValidGameplayTagString(newTag, out string error, out string fixedString))
            {
                Debug.LogError($"Invalid Gameplay Tag: {newTag}");
                return false;
            }

            string newTagName = newTag;

            if (GameplayTagsManager.Instance.IsDictionaryTag(newTagName))
            {
                Debug.LogError($"Dictionary Tag: {newTagName} is not allowed to be added to the INI file");
                return false;
            }

            string ancestorTag = newTag;
            bool wasSplit = false;

            while (wasSplit)
            {
                if (GameplayTagsManager.Instance.IsDictionaryTag(ancestorTag))
                {
                    ancestorTag = ancestorTag.Substring(0, ancestorTag.LastIndexOf('.'));
                }
            }

            GameplayTagSource tagSource = GameplayTagsManager.Instance.FindTagSource(tagSourceName);

            if (tagSource is null)
            {
                tagSource = GameplayTagsManager.Instance.FindOrAddTagSource(tagSourceName, GameplayTagSourceType.TagList);
            }

            bool success = false;
            if (tagSource is not null)
            {
                if (tagSource.SourceTagList is not null)
                {
                    GameplayTagsList tagList = tagSource.SourceTagList;
                    tagList.GameplayTagList.AddUnique(new GameplayTagTableRow(newTag, comment));
                    tagList.SortTags();
                    success = true;
                }
            }

            if (!success)
            {
                Debug.LogError($"Failed to add new Gameplay Tag: {newTagName} to the INI file");
                return false;
            }

            {
                GameplayTagsManager.Instance.EditorRefreshGameplayTagTree();
            }

            return true;
        }

        public bool DeleteTagFromINI(GameplayTagNode tagNodeToDelete)
        {
            string tagName = tagNodeToDelete.CompleteTagName;




            return true;
        }

        public bool DeleteTagRedirect(string tagToDelete)
        {
            GameplayTagSettings settings = AssetDatabase.LoadAssetAtPath<GameplayTagSettings>("Packages/com.luuuuyang.gameplaytags/Editor/Config/DefaultGameplayTags.asset");

            for (int i = 0; i < settings.GameplayTagRedirects.Count; i++)
            {
                if (settings.GameplayTagRedirects[i].OldTagName == tagToDelete)
                {
                    settings.GameplayTagRedirects.RemoveAt(i);

                    GameplayTagsManager.Instance.EditorRefreshGameplayTagTree();
                    return true;
                }
            }

            return false;
        }
    }
}