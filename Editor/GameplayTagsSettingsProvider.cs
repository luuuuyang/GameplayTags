using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameplayTags.Editor
{
    public class GameplayTagSettingsProvider : SettingsProvider
    {
        private SerializedObject SerializedObject;
        
        public GameplayTagSettingsProvider(string path, SettingsScope scope = SettingsScope.User) : base(path, scope)
        {
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            base.OnActivate(searchContext, rootElement);
            SerializedObject = GameplayTagSettings.GetSerializedObject();
        }

        public override void OnGUI(string searchContext)
        {
            EditorGUILayout.PropertyField(SerializedObject.FindProperty("ImportTagsFromConfig"));
            EditorGUILayout.PropertyField(SerializedObject.FindProperty("WarnOnInvalidTags"));
            EditorGUILayout.PropertyField(SerializedObject.FindProperty("GameplayTagRedirects"));
            EditorGUILayout.PropertyField(SerializedObject.FindProperty("InvalidTagCharacters"));

            if (GUILayout.Button("Add New Gameplay Tag Source"))
            {
                OdinAddNewGameplayTagSource.ShowWindow();
            }

            if (GUILayout.Button("Cleanup Unused Tags"))
            {
                // GameplayTagSettings.CleanupUnusedTags();
            }

            if (GUILayout.Button("Manage Gameplay Tags"))
            {
                OdinGameplayTagPicker.ShowWindow(new Rect(0, 0, 800, 600), false, null, null, null, GameplayTagPickerMode.ManagementMode);
            }

            SerializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        [SettingsProvider]
        public static SettingsProvider CreateGameplayTagSettingsProvider()
        {
            return new GameplayTagSettingsProvider("Project/GameplayTags", SettingsScope.Project);
        }
    }
}