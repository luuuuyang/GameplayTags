using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace GameplayTags.Editor
{
    public class OdinRenameGameplayTagWindow : OdinEditorWindow
    {
        public delegate void OnGameplayTagRenamed(string oldName, string newName);

        private GameplayTagNode gameplayTagNode;

        [SerializeField]
        [ReadOnly]
        private string currentTagName;

        [SerializeField]
        private string newTagName;
        private OnGameplayTagRenamed onGameplayTagRenamed;

        // 静态方法打开窗口
        public static void ShowWindow(GameplayTagNode gameplayTagNode, OnGameplayTagRenamed onGameplayTagRenamed)
        {
            OdinRenameGameplayTagWindow window = GetWindow<OdinRenameGameplayTagWindow>();
            window.gameplayTagNode = gameplayTagNode;
            window.onGameplayTagRenamed = onGameplayTagRenamed;
            window.currentTagName = gameplayTagNode.CompleteTag.TagName;
            window.newTagName = gameplayTagNode.CompleteTag.TagName;
            window.ShowModal();
        }

        private bool IsRenameEnabled()
        {
            string currentTagText = newTagName;

            return !string.IsNullOrEmpty(currentTagText) && currentTagText != gameplayTagNode.CompleteTag.TagName;
        }

        [Button("Rename")]
        [EnableIf("IsRenameEnabled")]
        private void OnRenameClicked()
        {
            RenameAndClose();
        }

        [Button("Cancel")]
        private void OnCancelClicked()
        {
            Close();
        }

        private void RenameAndClose()
        {
            IGameplayTagsEditorModule module = IGameplayTagsEditorModule.Instance;

            string tagToRename = gameplayTagNode.CompleteTag.TagName;
            string newTagName = this.newTagName;

            if (module.RenameTagInINI(tagToRename, newTagName))
            {
                onGameplayTagRenamed?.Invoke(tagToRename, newTagName);
            }
            
            Close();
        }
    }
}
