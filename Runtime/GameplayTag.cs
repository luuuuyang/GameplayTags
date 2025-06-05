using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameplayTags
{
	[Serializable]
	public struct GameplayTag : IComparable<GameplayTag>, IEquatable<GameplayTag>, ICloneable/*, ISerializationCallbackReceiver*/
	{
		public string TagName;

		public static readonly GameplayTag EmptyTag;

		public GameplayTag(in string tagName)
		{
			TagName = tagName;
		}

		// public void OnBeforeSerialize()
		// {
		// }

		// public void OnAfterDeserialize()
		// {
		// 	GameplayTagsManager.Instance.SingleGameplayTagLoaded(ref this, null);
		// }

		public readonly bool IsValid()
		{
			return !string.IsNullOrEmpty(TagName);
		}

		public readonly GameplayTag RequestDirectParent()
		{
			return GameplayTagsManager.Instance.RequestGameplayTagDirectParent(this);
		}

		public readonly GameplayTagContainer GetGameplayTagParents()
		{
			return GameplayTagsManager.Instance.RequestGameplayTagParents(this);
		}

		public void ParseParentTags(List<GameplayTag> uniqueParentTags)
		{
			string rawTag = TagName;

			int dotIndex = rawTag.LastIndexOf('.');

			while (dotIndex != -1)
			{
				rawTag = rawTag[..dotIndex];
				dotIndex = rawTag.LastIndexOf('.');

				GameplayTag parentTag = new GameplayTag(rawTag);

				uniqueParentTags.AddUnique(parentTag);
			}
		}

		public readonly GameplayTagContainer SingleTagContainer
		{
			get
			{
				GameplayTagNode node = GameplayTagsManager.Instance.FindTagNode(this);

				if (node is not null)
				{
					return node.SingleTagContainer;
				}

				Debug.Assert(!IsValid(), $"GetSingleTagContainer passed invalid gameplay tag {TagName}, only registered tags can be queried");

				return GameplayTagContainer.EmptyContainer;
			}
		}

		public static GameplayTag RequestGameplayTag(in string tagName, bool errorIfNotFound = true)
		{
			return GameplayTagsManager.Instance.RequestGameplayTag(tagName, errorIfNotFound);
		}

		public readonly bool MatchesTag(in GameplayTag tagToCheck)
		{
			GameplayTagNode tagNode = GameplayTagsManager.Instance.FindTagNode(this);

			if (tagNode is not null)
			{
				return tagNode.SingleTagContainer.HasTag(tagToCheck);
			}

			Debug.Assert(!IsValid(), $"MatchesTag passed invalid gameplay tag {TagName}, only registered tags can be used in containers");

			return false;
		}

		public readonly bool MatchesTagExact(in GameplayTag tagToCheck)
		{
			if (!tagToCheck.IsValid())
			{
				return false;
			}

			return TagName == tagToCheck.TagName;
		}

		public readonly bool MatchesAny(in GameplayTagContainer containerToCheck)
		{
			GameplayTagNode node = GameplayTagsManager.Instance.FindTagNode(this);

			if (node is not null)
			{
				return node.SingleTagContainer.HasAny(containerToCheck);
			}

			Debug.Assert(!IsValid(), $"MatchesAny passed invalid gameplay tag {TagName}, only registered tags can be used in containers");

			return false;
		}

		public readonly bool MatchesAnyExact(in GameplayTagContainer containerToCheck)
		{
			if (containerToCheck.IsEmpty())
			{
				return false;
			}

			return containerToCheck.GameplayTags.Contains(this);
		}

		public string GetTagLeafName()
		{
			string rawTag = TagName;
			if (string.IsNullOrEmpty(rawTag))
			{
				return rawTag;
			}

			int dotIndex = rawTag.LastIndexOf('.');
			if (dotIndex == -1)
			{
				return rawTag;
			}

			return rawTag[(dotIndex + 1)..];
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
			return TagName?.GetHashCode() ?? 0;
		}

		public override string ToString()
		{
			return TagName;
		}

		public object Clone()
		{
			return new GameplayTag(TagName);
		}
	}
}