using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using System.Linq;

namespace GameplayTags.Editor
{
    public class OdinGameplayTagDrawer : OdinValueDrawer<GameplayTag>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            SirenixEditorGUI.BeginHorizontalPropertyLayout(label);

            Rect rect = GUILayoutUtility.GetRect(0f, (float)ButtonSizes.Medium);
            if (GUI.Button(rect, ValueEntry.SmartValue.IsValid() ? ValueEntry.SmartValue.TagName : "None"))
            {
                OdinGameplayTagPicker.ShowWindow(rect, false, Property, OnTagSelected, new List<GameplayTagContainer>());
            }

            if (ValueEntry.SmartValue.IsValid())
            {
                if (SirenixEditorGUI.IconButton(EditorIcons.X))
                {
                    OnClearTag();
                }
            }

            SirenixEditorGUI.EndHorizontalPropertyLayout();
        }

        private void OnTagSelected(in List<GameplayTagContainer> tagContainers)
        {
            ValueEntry.SmartValue = tagContainers.IsEmpty() ? new GameplayTag() : tagContainers.First().First();
            Property.Tree.ApplyChanges();
        }

        private void OnClearTag()
        {
            ValueEntry.SmartValue = new GameplayTag();
            Property.Tree.ApplyChanges();
        }
    }
}