using System;
using UnityEngine;

namespace GameplayTags
{
	public enum GameplayTagEventType { NewOrRemoved, AnyCountChange }

	[Serializable]
	public class GameplayTag : IComparable<GameplayTag>, IEquatable<GameplayTag>
	{
		public string TagName;

		public static readonly GameplayTag EmptyTag;

		public GameplayTag()
		{
			TagName = string.Empty;
		}

		public GameplayTag(in string tagName)
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

		public static GameplayTag RequestGameplayTag(in string tagName, bool errorIfNotFound = true)
		{
			return GameplayTagsManager.Instance.RequestGameplayTag(tagName, errorIfNotFound);
		}

		public bool MatchesTag(in GameplayTag tagToCheck)
		{
			GameplayTagNode tagNode = GameplayTagsManager.Instance.FindTagNode(this);

			if (tagNode is not null)
			{
				return tagNode.SingleTagContainer.HasTag(tagToCheck);
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

		public bool MatchesAny(in GameplayTagContainer containerToCheck)
		{
			GameplayTagNode node = GameplayTagsManager.Instance.FindTagNode(this);

			if (node is not null)
			{
				return node.SingleTagContainer.HasAny(containerToCheck);
			}

			Debug.Assert(!IsValid(), $"MatchesAny passed invalid gameplay tag {TagName}, only registered tags can be used in containers");

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

		public bool Equals(GameplayTag other)
		{
			return TagName == other.TagName;
		}

		public override bool Equals(object obj)
		{
			if (obj is GameplayTag other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return TagName.GetHashCode();
		}

		public override string ToString()
		{
			return TagName;
		}
	}
}