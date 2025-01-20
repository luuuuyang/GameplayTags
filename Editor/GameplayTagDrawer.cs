using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameplayTags.Editor
{
    // [CustomPropertyDrawer(typeof(GameplayTag))]
    public class GameplayTagDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/GameplayTagDrawer.uxml");
            var root = visualTree.Instantiate();

            Label label = root.Q<Label>();
            label.text = preferredLabel;

            Button button = root.Q<Button>();
            button.clicked += () =>
            {
                List<GameplayTagContainer> tagContainers = new();

                UnityEditor.PopupWindow.Show(button.worldBound, new GameplayTagPicker
                {
                    MultiSelect = false,
                    // Property = property,
                    TagContainers = tagContainers,
                });
            };

            return root;
        }
    }
}