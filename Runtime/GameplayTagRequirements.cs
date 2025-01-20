using System;

namespace GameplayTags
{
	[Serializable]
	public class GameplayTagRequirements
	{
		public GameplayTagContainer RequireTags = new();
		public GameplayTagContainer IgnoreTags = new();
		public GameplayTagQuery TagQuery = new();

		public bool RequirementsMet(in GameplayTagContainer container)
		{
			bool hasRequired = container.HasAll(RequireTags);
			bool hasIgnored = container.HasAny(IgnoreTags);
			bool matchQuery = TagQuery.IsEmpty() || TagQuery.Matches(container);

			return hasRequired && !hasIgnored && matchQuery;
		}

		public bool IsEmpty()
		{
			return RequireTags.IsEmpty() && IgnoreTags.IsEmpty() && TagQuery.IsEmpty();
		}

		public static bool operator ==(GameplayTagRequirements a, GameplayTagRequirements b)
		{
			if (a.RequireTags != b.RequireTags)
			{
				return false;
			}
			if (a.IgnoreTags != b.IgnoreTags)
			{
				return false;
			}
			if (a.TagQuery != b.TagQuery)
			{
				return false;
			}
			return true;
		}

		public static bool operator !=(GameplayTagRequirements a, GameplayTagRequirements b)
		{
			return !(a == b);
		}

		public GameplayTagQuery ConvertTagFieldsToTagQuery()
		{
			bool hasRequiredTags = !RequireTags.IsEmpty();
			bool hasIgnoredTags = !IgnoreTags.IsEmpty();

			if (!hasRequiredTags && !hasIgnoredTags)
			{
				return new GameplayTagQuery();
			}

			GameplayTagQueryExpression requiredTagsQueryExpression = new GameplayTagQueryExpression().AllTagsMatch().AddTags(RequireTags);
			GameplayTagQueryExpression ignoreTagsQueryExpression = new GameplayTagQueryExpression().NoTagsMatch().AddTags(IgnoreTags);

			GameplayTagQueryExpression rootQueryExpression = new();
			if (hasRequiredTags && hasIgnoredTags)
			{
				rootQueryExpression = new GameplayTagQueryExpression().AllExprMatch().AddExpr(requiredTagsQueryExpression).AddExpr(ignoreTagsQueryExpression);
			}
			else if (hasRequiredTags)
			{
				rootQueryExpression = requiredTagsQueryExpression;
			}
			else if (hasIgnoredTags)
			{
				rootQueryExpression = ignoreTagsQueryExpression;
			}

			return GameplayTagQuery.BuildQuery(rootQueryExpression);
		}
	}
}