using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GameplayTags
{
    [CreateAssetMenu(fileName = "GameplayTagSettings", menuName = "GameplayAbilities/GameplayTagSettings")]
    public class GameplayTagSettings : GameplayTagsList
    {
        [ReadOnly]
        public bool ImportTagsFromConfig = true;

        [ReadOnly]
        public bool WarnOnInvalidTags = true;

        [ReadOnly]
        public List<GameplayTagRedirect> GameplayTagRedirects = new();

        [ReadOnly]
        public string InvalidTagCharacters = "\",";

        public static GameplayTagSettings GetOrCreateSettings()
        {
            // MonoScript script = MonoScript.FromScriptableObject(CreateInstance<GameplayTagSettings>());
            // string scriptPath = AssetDatabase.GetAssetPath(script);
            // string settingsPath = scriptPath.Replace("Runtime/GameplayTagSettings.cs", "Editor/Config/" + GameplayTagSource.DefaultName);

            // GameplayTagSettings settings = AssetDatabase.LoadAssetAtPath<GameplayTagSettings>(settingsPath);
            // if (settings == null)
            // {
            //     settings = CreateInstance<GameplayTagSettings>();
            //     AssetDatabase.CreateAsset(settings, settingsPath);
            //     AssetDatabase.SaveAssets();
            // }

            GameplayTagSettings settings = Resources.Load<GameplayTagSettings>("DefaultGameplayTags");
            return settings;
        }

#if UNITY_EDITOR
        public static SerializedObject GetSerializedObject()
        {
            return new SerializedObject(GetOrCreateSettings());
        }
#endif
    }
}
