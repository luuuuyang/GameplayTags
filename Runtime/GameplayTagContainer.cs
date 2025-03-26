using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameplayTags
{
	[Serializable]
	public class GameplayTagContainer : IEquatable<GameplayTagContainer>/*, ISerializationCallbackReceiver*/
	{
		public static readonly GameplayTagContainer EmptyContainer = new();

		public List<GameplayTag> GameplayTags = new();

		public List<GameplayTag> ParentTags = new();

		public int Count => GameplayTags.Count;

		public GameplayTagContainer()
		{
		}

		public GameplayTagContainer(in GameplayTagContainer other)
		{
			CopyFrom(other);
		}

		public GameplayTagContainer(in GameplayTag tag)
		{
			AddTag(tag);
		}

		public bool IsValidIndex(int index)
		{
			return GameplayTags.IsValidIndex(index);
		}

		public GameplayTag GetByIndex(int index)
		{
			return IsValidIndex(index) ? GameplayTags[index] : new GameplayTag();
		}

		public bool IsEmpty()
		{
			return GameplayTags.Count == 0;
		}

		public GameplayTag First()
		{
			return GameplayTags.Count > 0 ? GameplayTags.First() : new GameplayTag();
		}

		public GameplayTag Last()
		{
			return GameplayTags.Count > 0 ? GameplayTags.Last() : new GameplayTag();
		}

		public IEnumerator<GameplayTag> GetEnumerator()
		{
			return GameplayTags.GetEnumerator();
		}

		public void CopyFrom(GameplayTagContainer other)
		{
			if (this == other)
			{
				return;
			}

			GameplayTags.Clear();
			GameplayTags.AddRange(other.GameplayTags);

			ParentTags.Clear();
			ParentTags.AddRange(other.ParentTags);
		}

		// public void OnBeforeSerialize()
		// {
		// }

		// public void OnAfterDeserialize()
		// {
		// 	GameplayTagsManager.Instance.GameplayTagContainerLoaded(this, null);
		// 	FillParentTags();
		// }

		public static bool operator ==(GameplayTagContainer a, GameplayTagContainer b)
		{
			if (a.GameplayTags.Count != b.GameplayTags.Count)
			{
				return false;
			}
			return a.HasAllExact(b);
		}

		public static bool operator !=(GameplayTagContainer a, GameplayTagContainer b)
		{
			return !(a == b);
		}

		public override bool Equals(object obj)
		{
			if (obj is GameplayTagContainer other)
			{
				return Equals(other);
			}
			return false;
		}

		public bool Equals(GameplayTagContainer other)
		{
			return this == other;
		}

		public override string ToString()
		{
			string retString = string.Empty;
			for (int i = 0; i < GameplayTags.Count; i++)
			{
				retString += GameplayTags[i].ToString();

				if (i < GameplayTags.Count - 1)
				{
					retString += ", ";
				}
			}
			return retString;
		}

		public bool HasTag(in GameplayTag tagToCheck)
		{
			if (!tagToCheck.IsValid())
			{
				return false;
			}

			return GameplayTags.Contains(tagToCheck) || ParentTags.Contains(tagToCheck);
		}

		public bool HasTagExact(in GameplayTag tagToCheck)
		{
			if (!tagToCheck.IsValid())
			{
				return false;
			}
			return GameplayTags.Contains(tagToCheck);
		}

		public bool HasAny(GameplayTagContainer containerToCheck)
		{
			if (containerToCheck.IsEmpty())
			{
				return false;
			}
			foreach (GameplayTag otherTag in containerToCheck)
			{
				if (GameplayTags.Contains(otherTag) || ParentTags.Contains(otherTag))
				{
					return true;
				}
			}
			return false;
		}

		public bool HasAnyExact(GameplayTagContainer containerToCheck)
		{
			if (containerToCheck.IsEmpty())
			{
				return false;
			}
			foreach (GameplayTag otherTag in containerToCheck)
			{
				if (GameplayTags.Contains(otherTag))
				{
					return true;
				}
			}
			return false;
		}

		public bool HasAll(GameplayTagContainer containerToCheck)
		{
			if (containerToCheck.IsEmpty())
			{
				return true;
			}
			foreach (GameplayTag otherTag in containerToCheck)
			{
				if (!GameplayTags.Contains(otherTag) && !ParentTags.Contains(otherTag))
				{
					return false;
				}
			}
			return true;
		}

		public bool HasAllExact(GameplayTagContainer containerToCheck)
		{
			if (containerToCheck.IsEmpty())
			{
				return true;
			}
			foreach (GameplayTag otherTag in containerToCheck)
			{
				if (!GameplayTags.Contains(otherTag))
				{
					return false;
				}
			}
			return true;
		}

		public void AddTag(in GameplayTag tagToAdd)
		{
			if (tagToAdd.IsValid())
			{
				GameplayTags.AddUnique(tagToAdd);

				GameplayTagsManager.Instance.ExtractParentTags(tagToAdd, ParentTags);
			}
		}

		public void AddTagFast(in GameplayTag tagToAdd)
		{
			GameplayTags.AddUnique(tagToAdd);
			GameplayTagsManager.Instance.ExtractParentTags(tagToAdd, ParentTags);
		}

		public void AddParentsForTag(in GameplayTag tag)
		{
			GameplayTagsManager.Instance.ExtractParentTags(tag, ParentTags);
		}

		public void FillParentTags()
		{
			ParentTags.Reset();

			if (GameplayTags.Count > 0)
			{
				foreach (GameplayTag tag in GameplayTags)
				{
					GameplayTagsManager.Instance.ExtractParentTags(tag, ParentTags);
				}
			}
		}

		public GameplayTagContainer GetGameplayTagParents()
		{
			GameplayTagContainer resultContainer = new()
			{
				GameplayTags = new List<GameplayTag>(GameplayTags)
			};

			foreach (GameplayTag tag in ParentTags)
			{
				resultContainer.GameplayTags.AddUnique(tag);
			}

			return resultContainer;
		}

		public GameplayTagContainer Filter(in GameplayTagContainer otherContainer)
		{
			GameplayTagContainer resultContainer = new();

			foreach (GameplayTag tag in GameplayTags)
			{
				if (tag.MatchesAny(otherContainer))
				{
					resultContainer.AddTagFast(tag);
				}
			}

			return resultContainer;
		}

		public GameplayTagContainer FilterExact(in GameplayTagContainer otherContainer)
		{
			GameplayTagContainer resultContainer = new();

			foreach (GameplayTag tag in GameplayTags)
			{
				if (tag.MatchesAnyExact(otherContainer))
				{
					resultContainer.AddTagFast(tag);
				}
			}

			return resultContainer;
		}

		public bool MatchesQuery(in GameplayTagQuery query)
		{
			return query.Matches(this);
		}

		public void AppendTags(in GameplayTagContainer other)
		{
			if (other.IsEmpty())
			{
				return;
			}

			int oldTagCount = GameplayTags.Count;
			GameplayTags.Capacity = oldTagCount + other.GameplayTags.Count;
			foreach (GameplayTag otherTag in other)
			{
				int searchIndex = 0;
				while (true)
				{
					if (searchIndex >= oldTagCount)
					{
						GameplayTags.Add(otherTag);
						break;
					}
					else if (GameplayTags[searchIndex] == otherTag)
					{
						break;
					}

					searchIndex++;
				}
			}

			oldTagCount = ParentTags.Count;
			ParentTags.Capacity = oldTagCount + other.ParentTags.Count;
			foreach (GameplayTag otherParentTag in other.ParentTags)
			{
				int searchIndex = 0;
				while (true)
				{
					if (searchIndex >= oldTagCount)
					{
						ParentTags.Add(otherParentTag);
						break;
					}
					else if (ParentTags[searchIndex] == otherParentTag)
					{
						break;
					}

					searchIndex++;
				}
			}
		}

		public void AppendMatchingTags(in GameplayTagContainer otherA, in GameplayTagContainer otherB)
		{
			foreach (GameplayTag otherATag in otherA.GameplayTags)
			{
				if (otherATag.MatchesAny(otherB))
				{
					AddTag(otherATag);
				}
			}
		}

		public bool RemoveTag(in GameplayTag tagToRemove, bool deferParentTags = false)
		{
			int numChanged = GameplayTags.RemoveSingle(tagToRemove);

			if (numChanged > 0)
			{
				if (!deferParentTags)
				{
					FillParentTags();
				}
				return true;
			}
			return false;
		}

		public void RemoveTags(in GameplayTagContainer tagsToRemove)
		{
			int numChanged = 0;

			foreach (GameplayTag tag in tagsToRemove)
			{
				numChanged += GameplayTags.RemoveSingle(tag);
			}

			if (numChanged > 0)
			{
				FillParentTags();
			}
		}

		public void Reset(int slack = 0)
		{
			GameplayTags.Reset(slack);
			ParentTags.Reset(slack);
		}
	}

	public enum GameplayTagQueryExprType
	{
		Undefined,
		AnyTagsMatch,
		AllTagsMatch,
		NoTagsMatch,
		AnyExprMatch,
		AllExprMatch,
		NoExprMatch,
	}

	public enum GameplayTagQueryStreamVersion
	{
		InitialVersion = 0,
		VersionPlusOne,
		LastestVersion = VersionPlusOne - 1,
	}

	public class QueryEvaluator
	{
		internal GameplayTagQuery Query;
		internal int CurStreamIdx;
		internal int Version;
		internal bool ReadError;

		private byte GetToken()
		{
			if (Query.QueryTokenStream.IsValidIndex(CurStreamIdx))
			{
				return Query.QueryTokenStream[CurStreamIdx++];
			}

			Debug.LogWarning("Error parsing GameplayTagQuery");
			ReadError = true;
			return 0;
		}

		public bool Eval(in GameplayTagContainer tags)
		{
			CurStreamIdx = 0;

			Version = GetToken();
			if (ReadError)
			{
				return false;
			}

			bool ret = false;

			byte hasRootExpression = GetToken();
			if (!ReadError && hasRootExpression != 0)
			{
				ret = EvalExpr(tags);
			}

			Debug.Assert(CurStreamIdx == Query.QueryTokenStream.Count);
			return ret;

		}

		public void Read(GameplayTagQueryExpression e)
		{
			e = new GameplayTagQueryExpression();
			CurStreamIdx = 0;

			if (Query.QueryTokenStream.Count > 0)
			{
				Version = GetToken();
				if (!ReadError)
				{
					byte hasRootExpression = GetToken();
					if (!ReadError && hasRootExpression != 0)
					{
						ReadExpr(e);
					}
				}

				Debug.Assert(CurStreamIdx == Query.QueryTokenStream.Count);
			}
		}


		private void ReadExpr(GameplayTagQueryExpression e)
		{
			e.ExprType = (GameplayTagQueryExprType)GetToken();
			if (ReadError)
			{
				return;
			}

			if (e.UsesTagSet)
			{
				int numTags = GetToken();
				if (ReadError)
				{
					return;
				}

				for (int i = 0; i < numTags; i++)
				{
					int tagIndex = GetToken();
					if (ReadError)
					{
						return;
					}

					GameplayTag tag = Query.GetTagFromIndex(tagIndex);
					e.AddTag(tag);
				}
			}
			else
			{
				int numExprs = GetToken();
				if (ReadError)
				{
					return;
				}

				for (int i = 0; i < numExprs; i++)
				{
					GameplayTagQueryExpression expr = new();
					ReadExpr(expr);
					e.AddExpr(expr);
				}
			}
		}

		private bool EvalAnyTagsMatch(in GameplayTagContainer tags, bool skip)
		{
			bool shortCircuit = skip;
			bool result = false;

			int numTags = GetToken();
			if (ReadError)
			{
				return false;
			}

			for (int i = 0; i < numTags; i++)
			{
				int tagIndex = GetToken();
				if (ReadError)
				{
					return false;
				}

				if (shortCircuit == false)
				{
					GameplayTag tag = Query.GetTagFromIndex(tagIndex);

					bool hasTag = tags.HasTag(tag);

					if (hasTag)
					{
						result = true;
						shortCircuit = true;
					}
				}

			}

			return result;
		}

		private bool EvalAllTagsMatch(in GameplayTagContainer tags, bool skip)
		{
			bool shortCircuit = skip;
			bool result = true;

			int numTags = GetToken();
			if (ReadError)
			{
				return false;
			}

			for (int i = 0; i < numTags; i++)
			{
				int tagIndex = GetToken();
				if (ReadError)
				{
					return false;
				}

				if (shortCircuit == false)
				{
					GameplayTag tag = Query.GetTagFromIndex(tagIndex);

					bool hasTag = tags.HasTag(tag);

					if (hasTag == false)
					{
						shortCircuit = true;
						result = false;
					}
				}
			}

			return result;
		}

		private bool EvalNoTagsMatch(in GameplayTagContainer tags, bool skip)
		{
			bool shortCircuit = skip;
			bool result = true;

			int numTags = GetToken();
			if (ReadError)
			{
				return false;
			}

			for (int i = 0; i < numTags; i++)
			{
				int tagIndex = GetToken();
				if (ReadError)
				{
					return false;
				}

				if (shortCircuit == false)
				{
					GameplayTag tag = Query.GetTagFromIndex(tagIndex);

					bool hasTag = tags.HasTag(tag);

					if (hasTag == true)
					{
						shortCircuit = true;
						result = false;
					}
				}
			}

			return result;
		}

		private bool EvalAnyExprMatch(in GameplayTagContainer tags, bool skip)
		{
			bool shortCircuit = skip;
			bool result = false;

			int numExprs = GetToken();
			if (ReadError)
			{
				return false;
			}

			for (int i = 0; i < numExprs; i++)
			{
				bool exprResult = EvalExpr(tags, shortCircuit);
				if (shortCircuit == false)
				{
					if (exprResult)
					{
						result = true;
						shortCircuit = true;
					}
				}
			}

			return result;
		}

		private bool EvalAllExprMatch(in GameplayTagContainer tags, bool skip)
		{
			bool shortCircuit = skip;
			bool result = true;

			int numExprs = GetToken();
			if (ReadError)
			{
				return false;
			}

			for (int i = 0; i < numExprs; i++)
			{
				bool exprResult = EvalExpr(tags, shortCircuit);
				if (shortCircuit == false)
				{
					if (exprResult == false)
					{
						result = false;
						shortCircuit = true;
					}
				}
			}

			return result;
		}

		private bool EvalNoExprMatch(in GameplayTagContainer tags, bool skip)
		{
			bool shortCircuit = skip;
			bool result = true;

			int numExprs = GetToken();
			if (ReadError)
			{
				return false;
			}

			for (int i = 0; i < numExprs; i++)
			{
				bool exprResult = EvalExpr(tags, shortCircuit);
				if (shortCircuit == false)
				{
					if (exprResult)
					{
						result = false;
						shortCircuit = true;
					}
				}
			}

			return result;
		}

		private bool EvalExpr(in GameplayTagContainer tags, bool skip = false)
		{
			GameplayTagQueryExprType exprType = (GameplayTagQueryExprType)GetToken();
			if (ReadError)
			{
				return false;
			}

			switch (exprType)
			{
				case GameplayTagQueryExprType.AnyTagsMatch:
					return EvalAnyTagsMatch(tags, skip);
				case GameplayTagQueryExprType.AllTagsMatch:
					return EvalAllTagsMatch(tags, skip);
				case GameplayTagQueryExprType.NoTagsMatch:
					return EvalNoTagsMatch(tags, skip);
				case GameplayTagQueryExprType.AnyExprMatch:
					return EvalAnyExprMatch(tags, skip);
				case GameplayTagQueryExprType.AllExprMatch:
					return EvalAllExprMatch(tags, skip);
				case GameplayTagQueryExprType.NoExprMatch:
					return EvalNoExprMatch(tags, skip);
			}

			return false;
		}

#if UNITY_EDITOR
		public EditableGameplayTagQuery CreateEditableQuery()
		{
			CurStreamIdx = 0;

			EditableGameplayTagQuery editableQuery = new();

			if (!Query.IsEmpty())
			{
				Version = GetToken();
				if (!ReadError)
				{
					byte hasRootExpression = GetToken();
					if (!ReadError && hasRootExpression != 0)
					{
						editableQuery.RootExpression = ReadEditableQueryExpr(editableQuery);
					}
				}
				Debug.Assert(CurStreamIdx == Query.QueryTokenStream.Count);
			}

			editableQuery.UserDescription = Query.UserDescription;

			return editableQuery;
		}

		private EditableGameplayTagQueryExpression ReadEditableQueryExpr(object exprOuter)
		{
			GameplayTagQueryExprType exprType = (GameplayTagQueryExprType)GetToken();
			if (ReadError)
			{
				return null;
			}

			Type exprClass = null;
			switch (exprType)
			{
				case GameplayTagQueryExprType.AnyTagsMatch:
					exprClass = typeof(EditableGameplayTagQueryExpression_AnyTagsMatch);
					break;
				case GameplayTagQueryExprType.AllTagsMatch:
					exprClass = typeof(EditableGameplayTagQueryExpression_AllTagsMatch);
					break;
				case GameplayTagQueryExprType.NoTagsMatch:
					exprClass = typeof(EditableGameplayTagQueryExpression_NoTagsMatch);
					break;
				case GameplayTagQueryExprType.AnyExprMatch:
					exprClass = typeof(EditableGameplayTagQueryExpression_AnyExprMatch);
					break;
				case GameplayTagQueryExprType.AllExprMatch:
					exprClass = typeof(EditableGameplayTagQueryExpression_AllExprMatch);
					break;
				case GameplayTagQueryExprType.NoExprMatch:
					exprClass = typeof(EditableGameplayTagQueryExpression_NoExprMatch);
					break;
			}

			EditableGameplayTagQueryExpression newExpr = null;
			if (exprClass != null)
			{
				newExpr = Activator.CreateInstance(exprClass) as EditableGameplayTagQueryExpression;
				if (newExpr != null)
				{
					switch (exprType)
					{
						case GameplayTagQueryExprType.AnyTagsMatch:
						case GameplayTagQueryExprType.AllTagsMatch:
						case GameplayTagQueryExprType.NoTagsMatch:
							ReadEditableQueryTags(newExpr);
							break;
						case GameplayTagQueryExprType.AnyExprMatch:
						case GameplayTagQueryExprType.AllExprMatch:
						case GameplayTagQueryExprType.NoExprMatch:
							ReadEditableQueryExprList(newExpr);
							break;
					}
				}
			}

			return newExpr;
		}

		private void ReadEditableQueryTags(EditableGameplayTagQueryExpression editableQueryExpr)
		{
			GameplayTagContainer tags = null;
			if (editableQueryExpr is EditableGameplayTagQueryExpression_AnyTagsMatch)
			{
				tags = (editableQueryExpr as EditableGameplayTagQueryExpression_AnyTagsMatch).Tags;
			}
			else if (editableQueryExpr is EditableGameplayTagQueryExpression_AllTagsMatch)
			{
				tags = (editableQueryExpr as EditableGameplayTagQueryExpression_AllTagsMatch).Tags;
			}
			else if (editableQueryExpr is EditableGameplayTagQueryExpression_NoTagsMatch)
			{
				tags = (editableQueryExpr as EditableGameplayTagQueryExpression_NoTagsMatch).Tags;
			}
			Debug.Assert(tags is not null);

			if (tags is not null)
			{
				byte numTags = GetToken();
				if (ReadError)
				{
					return;
				}

				for (int i = 0; i < numTags; i++)
				{
					int tagIndex = GetToken();
					if (ReadError)
					{
						return;
					}

					GameplayTag tag = Query.GetTagFromIndex(tagIndex);
					tags.AddTag(tag);
				}
			}
		}

		private void ReadEditableQueryExprList(EditableGameplayTagQueryExpression editableQueryExpr)
		{
			List<EditableGameplayTagQueryExpression> exprList = null;
			if (editableQueryExpr is EditableGameplayTagQueryExpression_AnyExprMatch)
			{
				exprList = (editableQueryExpr as EditableGameplayTagQueryExpression_AnyExprMatch).Expressions;
			}
			else if (editableQueryExpr is EditableGameplayTagQueryExpression_AllExprMatch)
			{
				exprList = (editableQueryExpr as EditableGameplayTagQueryExpression_AllExprMatch).Expressions;
			}
			else if (editableQueryExpr is EditableGameplayTagQueryExpression_NoExprMatch)
			{
				exprList = (editableQueryExpr as EditableGameplayTagQueryExpression_NoExprMatch).Expressions;
			}
			Debug.Assert(exprList != null);

			if (exprList != null)
			{
				byte numExprs = GetToken();
				if (ReadError)
				{
					return;
				}

				for (int i = 0; i < numExprs; i++)
				{
					EditableGameplayTagQueryExpression newExpr = ReadEditableQueryExpr(editableQueryExpr);
					exprList.Add(newExpr);
				}
			}
		}
#endif
	}

	[Serializable]
	public class GameplayTagQuery
	{
		public int TokenStreamVersion;
		public List<GameplayTag> TagDictionary = new();
		public List<byte> QueryTokenStream = new();
		public string UserDescription = string.Empty;
		public string AutoDescription = string.Empty;
		public string Description
		{
			get
			{
				return string.IsNullOrEmpty(UserDescription) ? AutoDescription : UserDescription;
			}
		}
		public static GameplayTagQuery EmptyQuery = new();

		public void CopyFrom(GameplayTagQuery other)
		{
			TokenStreamVersion = other.TokenStreamVersion;
			TagDictionary.Clear();
			TagDictionary.AddRange(other.TagDictionary);
			QueryTokenStream.Clear();
			QueryTokenStream.AddRange(other.QueryTokenStream);
			UserDescription = other.UserDescription;
			AutoDescription = other.AutoDescription;
		}

		public static bool operator ==(GameplayTagQuery a, GameplayTagQuery b)
		{
			return a.TokenStreamVersion == b.TokenStreamVersion &&
				a.TagDictionary == b.TagDictionary &&
				a.QueryTokenStream == b.QueryTokenStream &&
				a.UserDescription == b.UserDescription &&
				a.AutoDescription == b.AutoDescription;
		}

		public static bool operator !=(GameplayTagQuery a, GameplayTagQuery b)
		{
			return !(a == b);
		}

		public bool Matches(in GameplayTagContainer tags)
		{
			if (IsEmpty())
			{
				return false;
			}

			QueryEvaluator evaluator = new()
			{
				Query = this
			};
			return evaluator.Eval(tags);
		}

		public bool IsEmpty()
		{
			return QueryTokenStream.Count == 0;
		}

		public void Clear()
		{
			CopyFrom(EmptyQuery);
		}

		public void Build(GameplayTagQueryExpression rootQueryExpr, string userDescription = null)
		{
			TokenStreamVersion = (int)GameplayTagQueryStreamVersion.LastestVersion;
			UserDescription = userDescription;

			QueryTokenStream.Reset(128);
			TagDictionary.Reset();

			QueryTokenStream.Add((byte)GameplayTagQueryStreamVersion.LastestVersion);

			QueryTokenStream.Add(1);
			rootQueryExpr.EmitTokens(QueryTokenStream, TagDictionary);
		}

		public static GameplayTagQuery BuildQuery(GameplayTagQueryExpression rootQueryExpr, string description = null)
		{
			GameplayTagQuery Q = new();
			Q.Build(rootQueryExpr, description);
			return Q;
		}

#if UNITY_EDITOR
		public void BuildFromEditableQuery(EditableGameplayTagQuery editableQuery)
		{
			QueryTokenStream.Reset();
			TagDictionary.Reset();

			UserDescription = editableQuery.UserDescription;

			QueryTokenStream.Add((byte)GameplayTagQueryStreamVersion.LastestVersion);
			editableQuery.EmitTokens(QueryTokenStream, TagDictionary, ref AutoDescription);
		}

		public EditableGameplayTagQuery CreateEditableQuery()
		{
			QueryEvaluator QE = new()
			{
				Query = this
			};
			return QE.CreateEditableQuery();
		}
#endif

		public GameplayTag GetTagFromIndex(int tagIdx)
		{
			Debug.Assert(TagDictionary.IsValidIndex(tagIdx));
			return TagDictionary[tagIdx];
		}

		public static GameplayTagQuery MakeQuery_MatchAnyTags(in GameplayTagContainer tags)
		{
			return BuildQuery(new GameplayTagQueryExpression().AnyTagsMatch().AddTags(tags));
		}

		public static GameplayTagQuery MakeQuery_MatchAllTags(in GameplayTagContainer tags)
		{
			return BuildQuery(new GameplayTagQueryExpression().AllTagsMatch().AddTags(tags));
		}

		public static GameplayTagQuery MakeQuery_MatchNoTags(in GameplayTagContainer tags)
		{
			return BuildQuery(new GameplayTagQueryExpression().NoTagsMatch().AddTags(tags));
		}

		public static GameplayTagQuery MakeQuery_MatchTag(in GameplayTag tag)
		{
			return BuildQuery(new GameplayTagQueryExpression().AllTagsMatch().AddTag(tag));
		}
	}

	public class GameplayTagQueryExpression
	{
		public GameplayTagQueryExprType ExprType;
		public List<GameplayTagQueryExpression> ExprSet = new();
		public List<GameplayTag> TagSet = new();
		public bool UsesTagSet => ExprType == GameplayTagQueryExprType.AnyTagsMatch || ExprType == GameplayTagQueryExprType.AllTagsMatch || ExprType == GameplayTagQueryExprType.NoTagsMatch;
		public bool UsesExprSet => ExprType == GameplayTagQueryExprType.AnyExprMatch || ExprType == GameplayTagQueryExprType.AllExprMatch || ExprType == GameplayTagQueryExprType.NoExprMatch;

		public GameplayTagQueryExpression AnyTagsMatch()
		{
			ExprType = GameplayTagQueryExprType.AnyTagsMatch;
			return this;
		}

		public GameplayTagQueryExpression AllTagsMatch()
		{
			ExprType = GameplayTagQueryExprType.AllTagsMatch;
			return this;
		}

		public GameplayTagQueryExpression NoTagsMatch()
		{
			ExprType = GameplayTagQueryExprType.NoTagsMatch;
			return this;
		}

		public GameplayTagQueryExpression AnyExprMatch()
		{
			ExprType = GameplayTagQueryExprType.AnyExprMatch;
			return this;
		}

		public GameplayTagQueryExpression AllExprMatch()
		{
			ExprType = GameplayTagQueryExprType.AllExprMatch;
			return this;
		}

		public GameplayTagQueryExpression NoExprMatch()
		{
			ExprType = GameplayTagQueryExprType.NoExprMatch;
			return this;
		}

		public GameplayTagQueryExpression AddTag(string tagName)
		{
			GameplayTag tag = GameplayTagsManager.Instance.RequestGameplayTag(tagName);
			return AddTag(tag);
		}

		public GameplayTagQueryExpression AddTag(GameplayTag tag)
		{
			Debug.Assert(UsesTagSet);
			TagSet.Add(tag);
			return this;
		}

		public GameplayTagQueryExpression AddTags(GameplayTagContainer tags)
		{
			Debug.Assert(UsesTagSet);
			TagSet.AddRange(tags.GameplayTags);
			return this;
		}

		public GameplayTagQueryExpression AddExpr(GameplayTagQueryExpression expr)
		{
			Debug.Assert(UsesExprSet);
			ExprSet.Add(expr);
			return this;
		}

		public void EmitTokens(List<byte> tokenStream, List<GameplayTag> tagDictionary)
		{
			tokenStream.Add((byte)ExprType);

			switch (ExprType)
			{
				case GameplayTagQueryExprType.AnyTagsMatch:
				case GameplayTagQueryExprType.AllTagsMatch:
				case GameplayTagQueryExprType.NoTagsMatch:
					byte numTags = (byte)TagSet.Count;
					tokenStream.Add(numTags);

					foreach (GameplayTag tag in TagSet)
					{
						int tagIdx = tagDictionary.AddUnique(tag);
						Debug.Assert(tagIdx <= 254);
						tokenStream.Add((byte)tagIdx);
					}
					break;
				case GameplayTagQueryExprType.AnyExprMatch:
				case GameplayTagQueryExprType.AllExprMatch:
				case GameplayTagQueryExprType.NoExprMatch:
					byte numExprs = (byte)ExprSet.Count;
					tokenStream.Add(numExprs);

					foreach (GameplayTagQueryExpression expr in ExprSet)
					{
						expr.EmitTokens(tokenStream, tagDictionary);
					}
					break;
				default:
					break;
			}
		}
	}

	[Serializable]
	public class EditableGameplayTagQuery
	{
		public string UserDescription;

		[HideInInspector]
		public string AutoDescription;

		[SerializeReference]
		public EditableGameplayTagQueryExpression RootExpression;

#if UNITY_EDITOR
		public void EmitTokens(List<byte> tokenStream, List<GameplayTag> tagDictionary, ref string debugString)
		{
			if (debugString != null)
			{
				debugString = string.Empty;
			}

			if (RootExpression != null)
			{
				tokenStream.Add(1);
				RootExpression.EmitTokens(tokenStream, tagDictionary, ref debugString);
			}
			else
			{
				tokenStream.Add(0);
				if (debugString != null)
				{
					debugString += "undefined";
				}
			}
		}
#endif
	}

	[Serializable]
	public abstract class EditableGameplayTagQueryExpression
	{
#if UNITY_EDITOR
		public virtual void EmitTokens(List<byte> tokenStream, List<GameplayTag> tagDictionary, ref string debugString)
		{

		}

		protected void EmitTagTokens(in GameplayTagContainer tagsToEmit, List<byte> tokenStream, List<GameplayTag> tagDictionary, ref string debugString)
		{
			byte numTags = (byte)tagsToEmit.Count;
			tokenStream.Add(numTags);

			bool firstTag = true;

			foreach (GameplayTag tag in tagsToEmit)
			{
				int tagIdx = tagDictionary.AddUnique(tag);
				Debug.Assert(tagIdx <= 255);
				tokenStream.Add((byte)tagIdx);

				if (debugString != null)
				{
					if (firstTag == false)
					{
						debugString += ",";
					}

					debugString += " ";
					debugString += tag.ToString();
				}

				firstTag = false;
			}
		}

		protected void EmitExprListTokens(in List<EditableGameplayTagQueryExpression> exprList, List<byte> tokenStream, List<GameplayTag> tagDictionary, ref string debugString)
		{
			byte numExprs = (byte)exprList.Count;
			tokenStream.Add(numExprs);

			bool firstExpr = true;

			foreach (EditableGameplayTagQueryExpression expr in exprList)
			{
				if (debugString != null)
				{
					if (firstExpr == false)
					{
						debugString += ",";
					}

					debugString += " ";
				}

				if (expr != null)
				{
					expr.EmitTokens(tokenStream, tagDictionary, ref debugString);
				}
				else
				{
					tokenStream.Add((byte)GameplayTagQueryExprType.Undefined);
					if (debugString != null)
					{
						debugString += "undefined";
					}
				}

				firstExpr = false;
			}
		}
#endif
	}

	[Serializable]
	public class EditableGameplayTagQueryExpression_AnyTagsMatch : EditableGameplayTagQueryExpression
	{
		[SerializeField]
		public GameplayTagContainer Tags = new();

#if UNITY_EDITOR
		public override void EmitTokens(List<byte> tokenStream, List<GameplayTag> tagDictionary, ref string debugString)
		{
			tokenStream.Add((byte)GameplayTagQueryExprType.AnyTagsMatch);

			if (debugString != null)
			{
				debugString += " ANY(";
			}

			EmitTagTokens(Tags, tokenStream, tagDictionary, ref debugString);

			if (debugString != null)
			{
				debugString += " )";
			}
		}
#endif
	}

	[Serializable]
	public class EditableGameplayTagQueryExpression_AllTagsMatch : EditableGameplayTagQueryExpression
	{
		[SerializeField]
		public GameplayTagContainer Tags = new();

#if UNITY_EDITOR
		public override void EmitTokens(List<byte> tokenStream, List<GameplayTag> tagDictionary, ref string debugString)
		{
			tokenStream.Add((byte)GameplayTagQueryExprType.AllTagsMatch);

			if (debugString != null)
			{
				debugString += " ALL(";
			}

			EmitTagTokens(Tags, tokenStream, tagDictionary, ref debugString);

			if (debugString != null)
			{
				debugString += " )";
			}
		}
#endif
	}

	[Serializable]
	public class EditableGameplayTagQueryExpression_NoTagsMatch : EditableGameplayTagQueryExpression
	{
		[SerializeField]
		public GameplayTagContainer Tags = new();

#if UNITY_EDITOR
		public override void EmitTokens(List<byte> tokenStream, List<GameplayTag> tagDictionary, ref string debugString)
		{
			tokenStream.Add((byte)GameplayTagQueryExprType.NoTagsMatch);

			if (debugString != null)
			{
				debugString += " NONE(";
			}

			EmitTagTokens(Tags, tokenStream, tagDictionary, ref debugString);

			if (debugString != null)
			{
				debugString += " )";
			}
		}
#endif
	}

	[Serializable]
	public class EditableGameplayTagQueryExpression_AnyExprMatch : EditableGameplayTagQueryExpression
	{
		[SerializeReference]
		public List<EditableGameplayTagQueryExpression> Expressions = new();

#if UNITY_EDITOR
		public override void EmitTokens(List<byte> tokenStream, List<GameplayTag> tagDictionary, ref string debugString)
		{
			tokenStream.Add((byte)GameplayTagQueryExprType.AnyExprMatch);

			if (debugString != null)
			{
				debugString += " ANY(";
			}

			EmitExprListTokens(Expressions, tokenStream, tagDictionary, ref debugString);

			if (debugString != null)
			{
				debugString += " )";
			}
		}
#endif
	}

	[Serializable]
	public class EditableGameplayTagQueryExpression_AllExprMatch : EditableGameplayTagQueryExpression
	{
		[SerializeReference]
		public List<EditableGameplayTagQueryExpression> Expressions = new();

#if UNITY_EDITOR
		public override void EmitTokens(List<byte> tokenStream, List<GameplayTag> tagDictionary, ref string debugString)
		{
			tokenStream.Add((byte)GameplayTagQueryExprType.AllExprMatch);

			if (debugString != null)
			{
				debugString += " ALL(";
			}

			EmitExprListTokens(Expressions, tokenStream, tagDictionary, ref debugString);

			if (debugString != null)
			{
				debugString += " )";
			}
		}
#endif
	}

	[Serializable]
	public class EditableGameplayTagQueryExpression_NoExprMatch : EditableGameplayTagQueryExpression
	{
		[SerializeReference]
		public List<EditableGameplayTagQueryExpression> Expressions = new();

#if UNITY_EDITOR
		public override void EmitTokens(List<byte> tokenStream, List<GameplayTag> tagDictionary, ref string debugString)
		{
			tokenStream.Add((byte)GameplayTagQueryExprType.NoExprMatch);

			if (debugString != null)
			{
				debugString += " NONE(";
			}

			EmitExprListTokens(Expressions, tokenStream, tagDictionary, ref debugString);

			if (debugString != null)
			{
				debugString += " )";
			}
		}
#endif
	}
}