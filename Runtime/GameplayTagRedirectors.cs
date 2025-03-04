using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace GameplayTags
{
    [Serializable]
    public struct GameplayTagRedirect
    {
        public string OldTagName;
        public string NewTagName;
    }

    public class GameplayTagRedirectors
    {
        private Dictionary<string, GameplayTag> TagRedirects = new();

        private static GameplayTagRedirectors _instance;

        public static GameplayTagRedirectors Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GameplayTagRedirectors();
                }
                return _instance;
            }
        }

        private GameplayTagRedirectors()
        {
            // GameplayTagSettings mutableDefault = GameplayTagSettings.GetOrCreateSettings();

            bool foundDeprecated = false;

#if UNITY_EDITOR
            GameplayTagsManager.OnEditorRefreshGameplayTagTree += RefreshTagRedirects;
#endif

            RefreshTagRedirects();
        }

        public GameplayTag RedirectTag(in string tagName)
        {
            return TagRedirects.GetValueOrDefault(tagName, GameplayTag.EmptyTag);
        }

        public void RefreshTagRedirects()
        {
            TagRedirects.Clear();

            GameplayTagSettings mutableDefault = GameplayTagSettings.GetOrCreateSettings();

            foreach (GameplayTagRedirect redirect in mutableDefault.GameplayTagRedirects)
            {
                string oldTagName = redirect.OldTagName;
                string newTagName = redirect.NewTagName;

                UnityEngine.Debug.Assert(!TagRedirects.ContainsKey(oldTagName), $"Tag {oldTagName} is already a redirector");

                int iterationsLeft = 10;
                while (!string.IsNullOrEmpty(newTagName))
                {
                    bool foundRedirect = false;

                    foreach (GameplayTagRedirect secondRedirect in mutableDefault.GameplayTagRedirects)
                    {
                        if (secondRedirect.OldTagName == newTagName)
                        {
                            newTagName = secondRedirect.NewTagName;
                            foundRedirect = true;
                            break;
                        }
                    }
                    iterationsLeft--;

                    if (!foundRedirect)
                    {
                        break;
                    }

                    if (iterationsLeft <= 0)
                    {
                        UnityEngine.Debug.LogWarning($"Tag {oldTagName} has a redirect loop");
                        break;
                    }
                }

                TagRedirects.Add(oldTagName, new GameplayTag(newTagName));
            }
        }
    }
}
