using UnityEditor;
using UnityEngine;
using DesignPatterns;
using System.Collections.Generic;

namespace GameplayTags.Editor
{
    public class IGameplayTagsEditorModule : Singleton<IGameplayTagsEditorModule>
    {
        public bool AddNewGameplayTagToINI(in string newTag, in string comment, in string tagSourceName, bool tagIsExplicit = false, bool tagIsRedirected = false, bool tagAllowsNonRedirectedChildren = false)
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

#if UNITY_2023_1_OR_NEWER
                if (!AssetDatabase.AssetPathExists(configFileName))
#else
                if (!AssetDatabase.LoadAssetAtPath<Object>(configFileName))
#endif
                {
                    AssetDatabase.CreateAsset(tagListObj, configFileName);
                }
                EditorUtility.SetDirty(tagListObj);
                AssetDatabase.SaveAssets();
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
            if (tagNodeToDelete is null)
            {
                return false;
            }

            Dictionary<Object, string> objectsToUpdateConfig = new Dictionary<Object, string>();
            const bool onlyLog = false;
            bool returnValue = DeleteTagFromINIInternal(tagNodeToDelete, onlyLog, ref objectsToUpdateConfig);
            if (objectsToUpdateConfig.Count > 0)
            {
                UpdateTagSourcesAfterDelete(onlyLog, objectsToUpdateConfig);
                GameplayTagsManager.Instance.EditorRefreshGameplayTagTree();
            }

            return returnValue;
        }

        private bool DeleteTagFromINIInternal(in GameplayTagNode tagNodeToDelete, bool onlyLog, ref Dictionary<Object, string> objectsToUpdateConfig)
        {
            string tagName = tagNodeToDelete.CompleteTagName;

            GameplayTagsManager manager = GameplayTagsManager.Instance;
            GameplayTagSettings settings = GameplayTagSettings.GetOrCreateSettings();

            string comment = null;
            List<string> tagSourceNames = new List<string>();
            bool tagIsExplicit = false;
            bool tagIsRedirected = false;
            bool tagAllowsNonRedirectedChildren = false;

            if (DeleteTagRedirector(tagName, onlyLog, false, objectsToUpdateConfig))
            {
                return true;
            }

            if (!manager.GetTagEditorData(tagName, ref comment, ref tagSourceNames, ref tagIsExplicit, ref tagIsRedirected, ref tagAllowsNonRedirectedChildren))
            {
                Debug.LogError($"Cannot delete tag {tagName}, does not exist!");
                return false;
            }

            Debug.Assert(tagIsRedirected == tagNodeToDelete.IsRedirectedGameplayTag);

            if (!tagIsExplicit || tagSourceNames.Count == 0)
            {
                Debug.LogError($"Cannot delete tag {tagName} as it is implicit, remove children manually");
                return false;
            }

            GameplayTag actualTag = manager.RequestGameplayTag(tagName);
            GameplayTagContainer childTags = manager.RequestGameplayTagChildrenInDictionary(actualTag);

            List<string> tagsThatWillBeDeleted = new List<string>();

            tagsThatWillBeDeleted.Add(tagName);

            GameplayTag parentTag = actualTag.RequestDirectParent();
            while (parentTag.IsValid() && !manager.FindTagNode(parentTag).IsExplicitTag)
            {
                GameplayTagContainer parentChildTags = manager.RequestGameplayTagChildrenInDictionary(parentTag);

                Debug.Assert(parentChildTags.HasTagExact(actualTag));
                if (parentChildTags.Count == 1)
                {
                    tagsThatWillBeDeleted.Add(parentTag.TagName);
                    parentTag = parentTag.RequestDirectParent();
                }
                else
                {
                    break;
                }
            }

            foreach (string tag in tagsThatWillBeDeleted)
            {
                if (TagReferenceChecker.IsTagReferencedInProject(tag))
                {
                    Debug.LogError($"Cannot delete tag {tagName}, still referenced by {1} and possibly others");
                    return false;
                }
            }

            bool removeAny = false;
            foreach (string tagSourceName in tagSourceNames)
            {
                GameplayTagSource tagSource = manager.FindTagSource(tagSourceName);
                if (tagIsRedirected && tagSource.SourceRestrictedTagList == null)
                {
                    Debug.LogError($"Cannot delete tag {tagName} from source {tagSourceName}, remove manually");
                    continue;
                }

                if (!tagIsRedirected && tagSource.SourceTagList == null)
                {
                    Debug.LogError($"Cannot delete tag {tagName} from source {tagSourceName}, remove manually");
                    continue;
                }

                string configFileName = tagIsRedirected ? tagSource.SourceRestrictedTagList.ConfigFileName : tagSource.SourceTagList.ConfigFileName;

                int numRemoved = 0;
                if (tagIsRedirected)
                {
                    numRemoved = tagSource.SourceRestrictedTagList.RestrictedGameplayTagList.RemoveAll(tag => tagsThatWillBeDeleted.Contains(tag.Tag));
                    if (numRemoved > 0)
                    {
                        objectsToUpdateConfig.Add(tagSource.SourceRestrictedTagList, configFileName);
                    }
                }
                else
                {
                    numRemoved = tagSource.SourceTagList.GameplayTagList.RemoveAll(tag => tagsThatWillBeDeleted.Contains(tag.Tag));
                    if (numRemoved > 0)
                    {
                        objectsToUpdateConfig.Add(tagSource.SourceTagList, configFileName);
                    }
                }

                if (numRemoved > 0)
                {
                    if (childTags.Count > 0)
                    {
                        Debug.LogError($"Deleted explicit tag {tagName}, still exists implicitly due to children");
                    }
                    else
                    {
                        Debug.Log($"Deleted tag {tagName}");
                    }

                    removeAny = true;
                }
            }

            if (!removeAny)
            {
                Debug.LogError($"Cannot delete tag {tagName}, does not exist!");
            }
            return removeAny;
        }

        private void UpdateTagSourcesAfterDelete(bool onlyLog, in Dictionary<Object, string> objectsToUpdateConfig)
        {
            HashSet<string> configFileNames = new HashSet<string>();
            foreach (var pair in objectsToUpdateConfig)
            {
                configFileNames.Add(pair.Value);
            }

            foreach (var pair in objectsToUpdateConfig)
            {
                EditorUtility.SetDirty(pair.Key);
            }
            AssetDatabase.SaveAssets();
        }

        public bool DeleteTagRedirector(in string tagToDelete, bool onlyLog = false, bool refresh = true, Dictionary<Object, string> objectsToUpdateConfig = null)
        {
            GameplayTagSettings settings = GameplayTagSettings.GetOrCreateSettings();
            GameplayTagsManager manager = GameplayTagsManager.Instance;
            
            for (int i = 0; i < settings.GameplayTagRedirects.Count; i++)
            {
                if (settings.GameplayTagRedirects[i].OldTagName == tagToDelete)
                {
                    settings.GameplayTagRedirects.RemoveAt(i);

                    if (refresh)
                    {
                        EditorUtility.SetDirty(settings);
                        AssetDatabase.SaveAssets();

                        manager.EditorRefreshGameplayTagTree();
                    }
                    else
                    {
                        if (objectsToUpdateConfig != null)
                        {
                            objectsToUpdateConfig.Add(settings, settings.ConfigFileName);
                        }
                    }

                    Debug.Log($"Deleted tag redirect {0} {tagToDelete}");

                    if (refresh)
                    {
                        GameplayTagNode foundNode = manager.FindTagNode(tagToDelete);
                        Debug.Assert(foundNode == null || foundNode.CompleteTagName == tagToDelete, $"Failed to delete redirector {tagToDelete}!");
                    }

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

        public bool RenameTagInINI(in string tagToRename, in string tagToRenameTo)
        {
            string OldTagName = tagToRename;
            string NewTagName = tagToRenameTo;

            GameplayTagsManager manager = GameplayTagsManager.Instance;
            GameplayTagSettings settings = GameplayTagSettings.GetOrCreateSettings();

            string oldComment = null;
            string newComment = null;
            string oldTagSourceName = null;
            string newTagSourceName = null;
            bool tagIsExplicit = false;
            bool tagIsRedirected = false;
            bool tagAllowsNonRedirectedChildren = false;

            DeleteTagRedirector(NewTagName);
            DeleteTagRedirector(OldTagName);

            if (manager.GetTagEditorData(OldTagName, ref oldComment, ref oldTagSourceName, ref tagIsExplicit, ref tagIsRedirected, ref tagAllowsNonRedirectedChildren))
            {
                if (!manager.GetTagEditorData(NewTagName, ref newComment, ref newTagSourceName, ref tagIsExplicit, ref tagIsRedirected, ref tagAllowsNonRedirectedChildren))
                {
                    if (!AddNewGameplayTagToINI(tagToRenameTo, oldComment, oldTagSourceName, tagIsExplicit, tagIsRedirected, tagAllowsNonRedirectedChildren))
                    {
                        return false;
                    }
                }

                GameplayTagSource oldTagSource = manager.FindTagSource(oldTagSourceName);

                if (oldTagSource != null && oldTagSource.SourceTagList != null)
                {
                    GameplayTagsList tagList = oldTagSource.SourceTagList;
                    for (int i = 0; i < tagList.GameplayTagList.Count; i++)
                    {
                        if (tagList.GameplayTagList[i].Tag == OldTagName)
                        {
                            tagList.GameplayTagList.RemoveAt(i);
                            EditorUtility.SetDirty(tagList);
                            AssetDatabase.SaveAssets();
                            break;
                        }
                    }
                }
                else
                {
                    Debug.LogError($"Failed to find tag source {oldTagSourceName}");
                }
            }

            GameplayTagRedirect redirect = new()
            {
                OldTagName = OldTagName,
                NewTagName = NewTagName
            };

            settings.GameplayTagRedirects.AddUnique(redirect);

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            Debug.Log($"Renamed tag {OldTagName} to {NewTagName}");

            manager.EditorRefreshGameplayTagTree();

            return true;
        }
    }
}