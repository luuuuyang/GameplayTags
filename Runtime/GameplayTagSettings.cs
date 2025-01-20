using System.Collections.Generic;
using UnityEngine;

namespace GameplayTags
{
    [CreateAssetMenu(fileName = "GameplayTagsList", menuName = "GameplayAbilities/GameplayTagsList")]
    public class GameplayTagsList : ScriptableObject
    {
        public string ConfigFileName;

        public List<GameplayTagTableRow> GameplayTagList;

        public void SortTags()
        {
            GameplayTagList.Sort();
        }
    }

    [CreateAssetMenu(fileName = "GameplayTagSettings", menuName = "GameplayAbilities/GameplayTagSettings")]
    public class GameplayTagSettings : GameplayTagsList
    {
        public bool ImportTagsFromConfig;
        public List<GameplayTagRedirect> GameplayTagRedirects;
        public string InvalidTagCharacters;
    }
}
