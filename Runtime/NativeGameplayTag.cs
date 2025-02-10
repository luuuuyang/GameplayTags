using System.Collections.Generic;
using UnityEngine;

namespace GameplayTags
{
    public class NativeGameplayTag
    {
        public static HashSet<NativeGameplayTag> RegisteredNativeTags = new();

        private GameplayTag InternalTag;

        public string ModuleName;

        public GameplayTagTableRow GameplayTagTableRow => new GameplayTagTableRow(InternalTag.TagName);

        public NativeGameplayTag(string moduleName, string tagName)
        {
            ModuleName = moduleName;

            InternalTag = string.IsNullOrEmpty(tagName) ? new GameplayTag() : new GameplayTag(tagName);

            RegisteredNativeTags.Add(this);

            GameplayTagsManager.Instance.AddNativeGameplayTag(this);
        }

        ~NativeGameplayTag()
        {
            RegisteredNativeTags.Remove(this);

            GameplayTagsManager.Instance.RemoveNativeGameplayTag(this);
        }


        public static implicit operator GameplayTag(NativeGameplayTag nativeTag)
        {
            return nativeTag.InternalTag;
        }
    }

}
