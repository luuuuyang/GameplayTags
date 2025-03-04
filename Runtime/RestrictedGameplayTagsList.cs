using System.Collections.Generic;
using UnityEngine;

namespace GameplayTags
{
    [CreateAssetMenu(fileName = "RestrictedGameplayTagsList", menuName = "Scriptable Objects/RestrictedGameplayTagsList")]
    public class RestrictedGameplayTagsList : ScriptableObject
    {
        public string ConfigFileName;
        public List<RestrictedGameplayTagTableRow> RestrictedGameplayTagList = new();

        public void SortTags()
        {
            RestrictedGameplayTagList.Sort();
        }
    }
}