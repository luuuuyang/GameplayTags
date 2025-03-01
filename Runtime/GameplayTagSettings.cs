using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace GameplayTags
{
    [CreateAssetMenu(fileName = "GameplayTagSettings", menuName = "GameplayAbilities/GameplayTagSettings")]
    public class GameplayTagSettings : GameplayTagsList
    {
        public const string SettingsPath = "Packages/com.luuuuyang.gameplaytags/Runtime/GameplayTagSettings.asset";
        
        [ReadOnly]
        public bool ImportTagsFromConfig = true;

        [ReadOnly]
        public List<GameplayTagRedirect> GameplayTagRedirects = new();

        [ReadOnly]
        public string InvalidTagCharacters = "\",";

        public static GameplayTagSettings GetOrCreateSettings()
        {
            GameplayTagSettings settings = AssetDatabase.LoadAssetAtPath<GameplayTagSettings>(SettingsPath);
            if (settings == null)
            {
                settings = CreateInstance<GameplayTagSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
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
