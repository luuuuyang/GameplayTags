using System.Collections.Generic;
using UnityEngine;

namespace GameplayTags
{
	public delegate void OnGameplayEffectTagCountChanged(GameplayTag tag, int count_delta);
	public delegate void DeferredTagChangeDelegate();

	public class GameplayTagCountContainer
	{
		public Dictionary<GameplayTag, DelegateInfo> GameplayTagEventMap = new();

		public List<GameplayTag> GameplayTags = new();

		public List<GameplayTag> ParentTags = new();

		public Dictionary<GameplayTag, int> GameplayTagCountMap = new();

		public Dictionary<GameplayTag, int> ExplicitTagCountMap = new();

		public OnGameplayEffectTagCountChanged OnAnyTagChangeDelegate;

		public GameplayTagContainer ExplicitTags = new();

		public struct DelegateInfo
		{
			public OnGameplayEffectTagCountChanged OnNewOrRemove;
			public OnGameplayEffectTagCountChanged OnAnyChange;
		}

		public void Notify_StackCountChange(in GameplayTag tag)
		{
			GameplayTagContainer tagAndParentsContainer = tag.GetGameplayTagParents();
			foreach (GameplayTag curTag in tagAndParentsContainer)
			{
				if (GameplayTagEventMap.TryGetValue(curTag, out DelegateInfo delegateInfo))
				{
					if (!GameplayTagCountMap.TryGetValue(curTag, out int tagCount))
					{
						GameplayTagCountMap.Add(curTag, 0);
					}
					delegateInfo.OnAnyChange?.Invoke(curTag, tagCount);
				}
			}
		}

		public void FillParentTags()
		{
			ExplicitTags.FillParentTags();
		}

		public bool HasAllMatchingGameplayTags(in GameplayTagContainer tag_container)
		{
			if (tag_container.Num == 0)
			{
				return true;
			}

			var all_match = true;
			foreach (GameplayTag tag in tag_container)
			{
				if (!GameplayTagCountMap.ContainsKey(tag))
				{
					GameplayTagCountMap.Add(tag, 0);
				}
				if (GameplayTagCountMap[tag] <= 0)
				{
					all_match = false;
					break;
				}
			}
			return all_match;
		}

		public bool HasAnyMatchingGameplayTags(in GameplayTagContainer tag_container)
		{
			if (tag_container.Num == 0)
			{
				return false;
			}

			var any_match = false;
			foreach (GameplayTag tag in tag_container)
			{
				if (GameplayTagCountMap.GetValueOrDefault(tag) > 0)
				{
					any_match = true;
					break;
				}
			}
			return any_match;
		}

		public void UpdateTagCount(in GameplayTagContainer container, in int count_delta)
		{
			if (count_delta != 0)
			{
				var deferred_tag_change_delegates = new List<DeferredTagChangeDelegate>();
				foreach (GameplayTag tag in container)
				{
					UpdateTagMapDeferredParentRemoval_Internal(tag, count_delta, deferred_tag_change_delegates);
				}
				foreach (var @delegate in deferred_tag_change_delegates)
				{
					@delegate.Invoke();
				}
			}
		}

		public bool UpdateTagCount_DeferredParentRemoval(in GameplayTag tag, int count_delta, List<DeferredTagChangeDelegate> deferred_tag_change_delegates)
		{
			if (count_delta != 0)
			{
				return UpdateTagMapDeferredParentRemoval_Internal(tag, count_delta, deferred_tag_change_delegates);
			}
			return false;
		}

		public bool UpdateTagCount(in GameplayTag tag, int count_delta)
		{
			if (count_delta != 0)
			{
				return UpdateTagMap_Internal(tag, count_delta);
			}
			return false;
		}

		private bool UpdateTagMap_Internal(in GameplayTag tag, int count_delta)
		{
			if (!UpdateExplicitTags(tag, count_delta, false))
			{
				return false;
			}

			List<DeferredTagChangeDelegate> deferred_tag_change_delegates = new();
			bool significant_change = GatherTagChangeDelegates(tag, count_delta, deferred_tag_change_delegates);
			foreach (DeferredTagChangeDelegate @delegate in deferred_tag_change_delegates)
			{
				@delegate.Invoke();
			}

			return significant_change;
		}

		private bool UpdateTagMapDeferredParentRemoval_Internal(in GameplayTag tag, in int count_delta, List<DeferredTagChangeDelegate> deferred_tag_change_delegates)
		{
			if (!UpdateExplicitTags(tag, count_delta, true))
			{
				return false;
			}

			return GatherTagChangeDelegates(tag, count_delta, deferred_tag_change_delegates);
		}

		private bool GatherTagChangeDelegates(in GameplayTag tag, in int count_delta, List<DeferredTagChangeDelegate> tag_change_delegates)
		{
			GameplayTagContainer tag_and_parents_container = tag.GetGameplayTagParents();
			bool created_significant_change = false;
			foreach (GameplayTag cur_tag in tag_and_parents_container)
			{

				if (!GameplayTagCountMap.ContainsKey(cur_tag))
				{
					GameplayTagCountMap.Add(cur_tag, 0);
				}
				GameplayTagCountMap.TryGetValue(cur_tag, out int tag_count);

				int old_count = tag_count;
				int new_tag_count = Mathf.Max(old_count + count_delta, 0);
				tag_count = new_tag_count;

				GameplayTagCountMap[cur_tag] = tag_count;

				bool significant_change = old_count == 0 || new_tag_count == 0;
				created_significant_change |= significant_change;
				if (significant_change)
				{
					tag_change_delegates.Add(() => OnAnyTagChangeDelegate?.Invoke(cur_tag, new_tag_count));
				}

				if (GameplayTagEventMap.TryGetValue(cur_tag, out DelegateInfo delegate_info))
				{
					tag_change_delegates.Add(() => delegate_info.OnAnyChange?.Invoke(cur_tag, new_tag_count));
					if (significant_change)
					{
						tag_change_delegates.Add(() => delegate_info.OnNewOrRemove?.Invoke(cur_tag, new_tag_count));
					}
				}
			}
			return created_significant_change;
		}

		private bool UpdateExplicitTags(in GameplayTag tag, in int count_delta, in bool defer_parent_tags_on_remove)
		{
			var tag_already_explicitly_exists = ExplicitTags.HasTagExact(tag);
			if (!tag_already_explicitly_exists)
			{
				if (count_delta > 0)
				{
					ExplicitTags.AddTag(tag);
				}
				else
				{
					return false;
				}
			}

			if (!ExplicitTagCountMap.ContainsKey(tag))
			{
				ExplicitTagCountMap.Add(tag, 0);
			}
			ExplicitTagCountMap.TryGetValue(tag, out int existing_count);

			existing_count = Mathf.Max(existing_count + count_delta, 0);

			ExplicitTagCountMap[tag] = existing_count;

			if (existing_count <= 0)
			{
				ExplicitTags.RemoveTag(tag, defer_parent_tags_on_remove);
			}

			return true;
		}

		public OnGameplayEffectTagCountChanged RegisterGameplayTagEvent(GameplayTag gameplay_tag, GameplayTagEventType event_type)
		{
			var info = GameplayTagEventMap.GetValueOrDefault(gameplay_tag);
			if (event_type == GameplayTagEventType.NewOrRemoved)
			{
				return info.OnNewOrRemove;
			}
			return info.OnAnyChange;
		}
	}
}