using System.Collections.Generic;

namespace GameplayTags
{
    public struct GameplayTagRedirect
    {
        public string OldTagName;
        public string NewTagName;
    }

    public class GameplayTagRedirectors
    {
        private Dictionary<string, GameplayTag> TagRedirects = new();

        public GameplayTag RedirectTag(in string tagName)
        {
            return TagRedirects.GetValueOrDefault(tagName, GameplayTag.EmptyTag);
        }

        public void RefreshTagRedirects()
        {
            TagRedirects.Clear();
        }
    }
}
