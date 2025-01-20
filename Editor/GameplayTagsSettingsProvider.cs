using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameplayTags.Editor
{
    public class GameplayTagSettingsProvider : SettingsProvider
    {
        public GameplayTagSettingsProvider(string path, SettingsScope scope = SettingsScope.User) : base(path, scope)
        {
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            base.OnActivate(searchContext, rootElement);
        }

        public override void OnGUI(string searchContext)
        {
            EditorGUILayout.LabelField("Gameplay Tag Settings");
            if (GUILayout.Button("Manage Gameplay Tags"))
            {
                OdinGameplayTagPicker.ShowWindow(new Rect(0, 0, 800, 600), false, null, null, null, GameplayTagPickerMode.ManagementMode);
            }
        }

        [SettingsProvider]
        public static SettingsProvider CreateGameplayTagSettingsProvider()
        {
            return new GameplayTagSettingsProvider("Project/GameplayTags", SettingsScope.Project);
        }
    }
}