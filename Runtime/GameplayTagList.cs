using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GameplayTags
{
    [CreateAssetMenu(fileName = "GameplayTagsList", menuName = "GameplayAbilities/GameplayTagsList")]
    public class GameplayTagsList : ScriptableObject
    {
        [ReadOnly]
        public string ConfigFileName;

        [ReadOnly]
        public List<GameplayTagTableRow> GameplayTagList = new();

        public void SortTags()
        {
            GameplayTagList.Sort();
        }
    }
}
