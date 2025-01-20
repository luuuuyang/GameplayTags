using UnityEngine;
using System;

namespace GameplayTags
{
	public enum GameplayTagEventType { NewOrRemoved, AnyCountChange }

	[Serializable]
	public class GameplayTag
	{
		public string TagName;

		public GameplayTag()
		{
			TagName = string.Empty;
		}

		public GameplayTag(string tagName)
		{
			TagName = tagName;
		}

		public bool IsValid()
		{
			return !string.IsNullOrEmpty(TagName);
		}

		public GameplayTag RequestDirectParent()
		{
			return GameplayTagsManager.Instance.RequestGameplayTagDirectParent(this);
		}

		public GameplayTagContainer GetGameplayTagParents()
		{
			return GameplayTagsManager.Instance.RequestGameplayTagParents(this);
		}

		public bool MatchesTag(in GameplayTag tagToCheck)
		{
			GameplayTagContainer tagContainer = GameplayTagsManager.Instance.GetSingleTagContainer(this);

			if (tagContainer is not null)
			{
				return tagContainer.HasTag(tagToCheck);
			}

			Debug.Assert(!IsValid(), $"MatchesTag passed invalid gameplay tag {TagName}, only registered tags can be used in containers");

			return false;
		}

		public bool MatchesTagExact(in GameplayTag tagToCheck)
		{
			if (!tagToCheck.IsValid())
			{
				return false;
			}

			return TagName == tagToCheck.TagName;
		}

		public static GameplayTag RequestGameplayTag(in string tagName, bool errorIfNotFound = true)
		{
			return GameplayTagsManager.Instance.RequestGameplayTag(tagName, errorIfNotFound);
		}

		public bool MatchesAny(in GameplayTagContainer containerToCheck)
		{
			var node = GameplayTagsManager.Instance.FindTagNode(this);

			if (node is not null)
			{
				return node.SingleTagContainer.HasAny(containerToCheck);
			}

			return false;
		}

		public bool MatchesAnyExact(in GameplayTagContainer containerToCheck)
		{
			if (containerToCheck.IsEmpty())
			{
				return false;
			}

			return containerToCheck.GameplayTags.Contains(this);
		}

		public static bool operator ==(GameplayTag a, GameplayTag b)
		{
			return a.TagName == b.TagName;
		}

		public static bool operator !=(GameplayTag a, GameplayTag b)
		{
			return a.TagName != b.TagName;
		}

		public int CompareTo(GameplayTag other)
		{
			return TagName.CompareTo(other.TagName);
		}

		public override bool Equals(object obj)
		{
			return obj is GameplayTag tag && TagName == tag.TagName;
		}

		public override int GetHashCode()
		{
			return TagName.GetHashCode();
		}

		public override string ToString()
		{
			return TagName;
		}

		public static readonly GameplayTag EmptyTag;
	}
}