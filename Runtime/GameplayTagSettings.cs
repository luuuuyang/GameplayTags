using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

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
            MonoScript script = MonoScript.FromScriptableObject(CreateInstance<GameplayTagSettings>());
            string scriptPath = AssetDatabase.GetAssetPath(script);
            string settingsPath = scriptPath.Replace("Runtime/GameplayTagSettings.cs", "Editor/Config/" + GameplayTagSource.DefaultName);

            GameplayTagSettings settings = AssetDatabase.LoadAssetAtPath<GameplayTagSettings>(settingsPath);
            if (settings == null)
            {
                settings = CreateInstance<GameplayTagSettings>();
                AssetDatabase.CreateAsset(settings, settingsPath);
                AssetDatabase.SaveAssets();
            }
            return settings;
        }

        public static SerializedObject GetSerializedObject()
        {
            return new SerializedObject(GetOrCreateSettings());
        }
    }
}
