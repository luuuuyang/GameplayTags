using UnityEditor;
using UnityEngine;
using DesignPatterns;

namespace GameplayTags.Editor
{
    public class IGameplayTagsEditorModule : Singleton<IGameplayTagsEditorModule>
    {
        public bool AddNewGameplayTagToINI(in string newTag, in string comment, in string tagSourceName)
        {
            GameplayTagsManager manager = GameplayTagsManager.Instance;

            if (string.IsNullOrEmpty(newTag))
            {
                return false;
            }

            if (!manager.IsValidGameplayTagString(newTag, out string error, out string fixedString))
            {
                Debug.LogError($"Invalid Gameplay Tag: {newTag}");
                return false;
            }

            string newTagName = newTag;

            if (manager.IsDictionaryTag(newTagName))
            {
                Debug.LogError($"Dictionary Tag: {newTagName} is not allowed to be added to the INI file");
                return false;
            }

            string ancestorTag = newTag;
            bool wasSplit = false;

            while (wasSplit)
            {
                if (manager.IsDictionaryTag(ancestorTag))
                {
                    ancestorTag = ancestorTag.Substring(0, ancestorTag.LastIndexOf('.'));
                }
            }

            GameplayTagSource tagSource = manager.FindTagSource(tagSourceName);

            if (tagSource is null)
            {
                tagSource = manager.FindOrAddTagSource(tagSourceName, GameplayTagSourceType.TagList);
            }

            bool success = false;
            if (tagSource is not null)
            {
                Object tagListObj = null;
                string configFileName = null;

                if (tagSource.SourceTagList is not null)
                {
                    GameplayTagsList tagList = tagSource.SourceTagList;
                    tagListObj = tagList;
                    tagList.GameplayTagList.AddUnique(new GameplayTagTableRow(newTag, comment));
                    tagList.SortTags();
                    configFileName = tagList.ConfigFileName;
                    success = true;
                }

                if (!AssetDatabase.AssetPathExists(configFileName))
                {
                    AssetDatabase.CreateAsset(tagListObj, configFileName);
                    AssetDatabase.SaveAssets();
                }
            }

            if (!success)
            {
                Debug.LogError($"Failed to add new Gameplay Tag: {newTagName} to the INI file");
                return false;
            }

            {
                manager.EditorRefreshGameplayTagTree();
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

        public virtual bool AddNewGameplayTagSource(in string newTagSource, in string rootDirToUse)
        {
            GameplayTagsManager manager = GameplayTagsManager.Instance;

            if (string.IsNullOrEmpty(newTagSource))
            {
                return false;
            }

            string tagSourceName;
            if (newTagSource.EndsWith(".asset"))
            {
                tagSourceName = newTagSource;
            }
            else
            {
                tagSourceName = newTagSource + ".asset";
            }

            manager.FindOrAddTagSource(tagSourceName, GameplayTagSourceType.TagList, rootDirToUse);
            
            return true;
        }
    }
}