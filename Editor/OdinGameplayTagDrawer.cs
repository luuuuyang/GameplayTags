using GameplayTags;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;

namespace GameplayTags.Editor
{
    public class OdinGameplayTagDrawer : OdinValueDrawer<GameplayTag>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            SirenixEditorGUI.BeginHorizontalPropertyLayout(label);

            if (SirenixEditorGUI.Button(string.IsNullOrEmpty(ValueEntry.SmartValue.TagName) ? "None" : ValueEntry.SmartValue.TagName, ButtonSizes.Medium))
            {
                var buttonRect = GUILayoutUtility.GetLastRect();
                PopupWindow.Show(buttonRect, new GameplayTagPicker
                {
                    MultiSelect = false,
                    Property = Property,
                    OnTagChanged = OnTagSelected
                });
            }

            if (SirenixEditorGUI.IconButton(EditorIcons.X))
            {
                OnClearTag();
            }

            SirenixEditorGUI.EndHorizontalPropertyLayout();
        }

        private void OnTagSelected(in List<GameplayTagContainer> tagContainers)
        {
            ValueEntry.SmartValue = tagContainers.IsEmpty() ? new GameplayTag() : tagContainers[0].First();
            ValueEntry.ApplyChanges();
        }

        private void OnClearTag()
        {
            ValueEntry.SmartValue = new GameplayTag();
            ValueEntry.ApplyChanges();
        }
    }
}
#endif