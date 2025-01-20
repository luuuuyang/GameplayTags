using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameplayTags
{
	[Serializable]
	public struct GameplayTagTableRow : IComparable<GameplayTagTableRow>
	{
		public string Tag;
		public string DevComment;

		public GameplayTagTableRow(string tag, string devComment)
		{
			Tag = tag;
			DevComment = devComment;
		}

		public int CompareTo(GameplayTagTableRow other)
		{
			return string.Compare(Tag, other.Tag, StringComparison.Ordinal);
		}
	}

	public enum GameplayTagSourceType
	{
		DefaultTagList,
		TagList,
		DataTable,
		Invalid,
	}

	public class GameplayTagNode
	{
		private string Tag;
		private GameplayTagContainer CompleteTagWithParents = new();
		private List<GameplayTagNode> ChildTags = new();
		private GameplayTagNode ParentNode;

		public GameplayTagNode() { }

		public GameplayTagNode(string tag, string fullTag, GameplayTagNode parentNode, bool isExplicitTag = false)
		{
			Tag = tag;
			ParentNode = parentNode;
			CompleteTagWithParents.GameplayTags.Add(new GameplayTag(fullTag));

			if (ParentNode is not null && string.IsNullOrEmpty(ParentNode.SimpleTagName) == false)
			{
				GameplayTagContainer parentContainer = ParentNode.SingleTagContainer;
				CompleteTagWithParents.ParentTags.Add(parentContainer.GameplayTags[0]);
				CompleteTagWithParents.ParentTags.AddRange(parentContainer.ParentTags);
			}

#if UNITY_EDITOR
			IsExplicitTag = isExplicitTag;
#endif
		}

		public GameplayTag CompleteTag => CompleteTagWithParents.Num > 0 ? CompleteTagWithParents.GameplayTags[0] : GameplayTag.EmptyTag;

		public string CompleteTagName => CompleteTag.TagName;

		public string SimpleTagName => Tag;

		public GameplayTagNode ParentTagNode => ParentNode;

		public List<GameplayTagNode> ChildTagNodes => ChildTags;

		public GameplayTagContainer SingleTagContainer => CompleteTagWithParents;

#if UNITY_EDITOR
		public List<string> SourceNames = new();
		public bool IsExplicitTag;
		public string DevComment;
#endif

		public void ResetNode()
		{
			Tag = null;
			CompleteTagWithParents.Reset();

			foreach (var child in ChildTags)
			{
				child.ResetNode();
			}
			ChildTags.Clear();

			ParentNode = null;

#if UNITY_EDITOR
			IsExplicitTag = false;
#endif
		}
	}

	public class GameplayTagSource
	{
		public string SourceName;
		public GameplayTagSourceType SourceType;
		public GameplayTagsList SourceTagList;

		public static readonly string DefaultGameplayTagsSO = "DefaultGameplayTags.asset";

		public static string GetDefaultName()
		{
			return DefaultGameplayTagsSO;
		}
	}

	public class GameplayTagsManager : Singleton<GameplayTagsManager>
	{
		private GameplayTagNode GameplayRootTag;

		private Dictionary<GameplayTag, GameplayTagNode> GameplayTagNodeMap = new();

		private Dictionary<string, GameplayTagSource> TagSources = new();

		private string InvalidTagCharacters = "!@#$%^&*()_+-=[]{}|;:,.<>?~`";

		private HashSet<string> MissingTagNames = new();

		public override void InitializeSingleton()
		{
			ConstructGameplayTagTree();
		}

		public void PopulateTreeFromDataTable(List<string> tags)
		{
			Debug.Assert(GameplayRootTag is not null, "ConstructGameplayTagTree() must be called before PopulateTreeFromDataTable()");

			string sourceName = "TempDataTable";

			GameplayTagSource foundSource = FindOrAddTagSource(sourceName, GameplayTagSourceType.DataTable);

			foreach (var tag in tags)
			{
				AddTagTableRow(new GameplayTagTableRow(tag, ""), sourceName);
			}
		}

		public void EditorRefreshGameplayTagTree()
		{
			DestroyGameplayTagTree();
			ConstructGameplayTagTree();
		}

		public GameplayTagContainer GetSingleTagContainer(in GameplayTag gameplayTag)
		{
			GameplayTagNodeMap.TryGetValue(gameplayTag, out GameplayTagNode node);
			if (node is not null)
			{
				return node.SingleTagContainer;
			}
			return null;
		}


		public GameplayTag RequestGameplayTag(string tagName, bool errorIfNotFound = true)
		{
			GameplayTag possibleTag = new(tagName);

			if (GameplayTagNodeMap.ContainsKey(possibleTag))
			{
				return possibleTag;
			}
			else if (errorIfNotFound)
			{
				if (!MissingTagNames.Contains(tagName))
				{
					Debug.LogError($"Requested Gameplay Tag {tagName} was not found, tags must be loaded from config or registered as a native tag");
					MissingTagNames.Add(tagName);
				}
			}

			return new GameplayTag();
		}

		public bool IsValidGameplayTagString(in string tagString, out string error, out string fixedString)
		{
			bool isValid = true;
			fixedString = tagString;

			if (string.IsNullOrEmpty(fixedString))
			{
				error = "Tag is empty";
				isValid = false;
			}

			while (fixedString.StartsWith(".", StringComparison.Ordinal))
			{
				error = "Tag starts with .";
				fixedString.Remove(0, 1);
				isValid = false;
			}

			while (fixedString.EndsWith(".", StringComparison.Ordinal))
			{
				error = "Tag ends with .";
				fixedString.Remove(fixedString.Length - 1, 1);
				isValid = false;
			}

			while (fixedString.StartsWith(" ", StringComparison.Ordinal))
			{
				error = "Tag starts with a space";
				fixedString.Remove(0, 1);
				isValid = false;
			}

			while (fixedString.EndsWith(" ", StringComparison.Ordinal))
			{
				error = "Tag ends with a space";
				fixedString.Remove(fixedString.Length - 1, 1);
				isValid = false;
			}

			foreach (char c in InvalidTagCharacters)
			{
				if (fixedString.Contains(c))
				{
					isValid = false;
				}
			}

			error = null;
			return isValid;
		}

		public bool IsDictionaryTag(string tagName)
		{
			GameplayTagNode node = FindTagNode(tagName);
			if (node is not null && node.IsExplicitTag)
			{
				return true;
			}

			return false;
		}

		public bool GetTagEditorData(string tagName, ref string comment, ref string firstTagSource, ref bool isTagExplicit)
		{
			GameplayTagNode node = FindTagNode(tagName);
			if (node is not null)
			{
				comment = node.DevComment;
				firstTagSource = node.SourceNames[0];
				isTagExplicit = node.IsExplicitTag;
				return true;
			}
			return false;
		}

		public GameplayTagContainer RequestGameplayTagParents(GameplayTag gameplayTag)
		{
			var parentTags = GetSingleTagContainer(gameplayTag);
			return parentTags.GetGameplayTagParents();
		}

		public GameplayTagContainer RequestAllGameplayTags(GameplayTagContainer tagContainer, bool onlyIncludeDictionaryTags = false)
		{
			List<GameplayTagNode> valueArray = GameplayTagNodeMap.Values.ToList();

			foreach (GameplayTagNode tagNode in valueArray)
			{
#if UNITY_EDITOR
				bool dictTag = IsDictionaryTag(tagNode.CompleteTagName);
#else
				bool dictTag = false;
#endif
				if (!onlyIncludeDictionaryTags || dictTag)
				{
					GameplayTag tag = GameplayTagNodeMap.First(x => x.Value == tagNode).Key;
					Debug.Assert(tag is not null);
					tagContainer.AddTagFast(tag);
				}
			}

			return tagContainer;
		}

		public GameplayTag RequestGameplayTagDirectParent(in GameplayTag gameplayTag)
		{
			GameplayTagNode gameplayTagNode = FindTagNode(gameplayTag);
			if (gameplayTagNode != null)
			{
				GameplayTagNode parent = gameplayTagNode.ParentTagNode;
				if (parent != null)
				{
					return parent.CompleteTag;
				}
			}
			return new GameplayTag();
		}

		public GameplayTagNode FindTagNode(in GameplayTag gameplayTag)
		{
			GameplayTagNodeMap.TryGetValue(gameplayTag, out var node);
			return node;
		}

		public GameplayTagNode FindTagNode(string tagName)
		{
			GameplayTag possibleTag = new(tagName);
			return FindTagNode(possibleTag);
		}

		public GameplayTagSource FindTagSource(string tagSourceName)
		{
			return TagSources.GetValueOrDefault(tagSourceName);
		}

		public GameplayTagSource FindOrAddTagSource(string tagSourceName, GameplayTagSourceType sourceType)
		{
			GameplayTagSource foundSource = FindTagSource(tagSourceName);
			if (foundSource is not null)
			{
				if (foundSource.SourceType == sourceType)
				{
					return foundSource;
				}

				return null;
			}

			GameplayTagSource newSource = new()
			{
				SourceName = tagSourceName,
				SourceType = sourceType,
			};
			TagSources[tagSourceName] = newSource;

			if (sourceType == GameplayTagSourceType.DefaultTagList)
			{
				newSource.SourceTagList = AssetDatabase.LoadAssetAtPath<GameplayTagsList>("Assets/GameplayTags/Config/DefaultGameplayTags.asset");
			}

			return newSource;
		}

		public void ConstructGameplayTagTree()
		{
			if (GameplayRootTag is null)
			{
				GameplayRootTag = new GameplayTagNode();

				GameplayTagSettings mutableDefault = AssetDatabase.LoadAssetAtPath<GameplayTagSettings>("Assets/GameplayTags/Config/DefaultGameplayTags.asset");

				InvalidTagCharacters = mutableDefault.InvalidTagCharacters;
				InvalidTagCharacters += "\r\n\t";

#if UNITY_EDITOR
				mutableDefault.SortTags();
#endif

				string TagSource = GameplayTagSource.GetDefaultName();
				GameplayTagSource defaultSource = FindOrAddTagSource(TagSource, GameplayTagSourceType.DefaultTagList);

				foreach (GameplayTagTableRow tableRow in mutableDefault.GameplayTagList)
				{
					AddTagTableRow(tableRow, TagSource);
				}
			}
		}

		public void DestroyGameplayTagTree()
		{
			if (GameplayRootTag is not null)
			{
				GameplayRootTag.ResetNode();
				GameplayRootTag = null;

				GameplayTagNodeMap.Clear();
			}
		}

		public void AddTagTableRow(in GameplayTagTableRow tagRow, string sourceName)
		{
			GameplayTagNode curNode = GameplayRootTag;
			List<GameplayTagNode> ancestorNodes = new();

			string originalTagName = tagRow.Tag;
			string fullTagString = originalTagName;

#if UNITY_EDITOR
			if (!IsValidGameplayTagString(originalTagName, out string errorText, out string fixedString))
			{
				if (fixedString == null)
				{
					Debug.LogError($"Invalid tag {fullTagString} from source {sourceName}: {errorText}!");
					return;
				}
				else
				{
					Debug.LogError($"Invalid tag {fullTagString} from source {sourceName}: {errorText}! Replacing with {fixedString},  you may need to modify InvalidTagCharacters");
					fullTagString = fixedString;
					originalTagName = fixedString;
				}
			}
#endif

			string[] subTags = fullTagString.Split('.');
			fullTagString = string.Empty;

			int numSubTags = subTags.Length;

			for (int i = 0; i < numSubTags; i++)
			{
				bool isExplicitTag = i == numSubTags - 1;
				string shortTagName = subTags[i];
				string fullTagName;

				if (isExplicitTag)
				{
					fullTagName = originalTagName;
				}
				else if (i == 0)
				{
					fullTagName = shortTagName;
					fullTagString = subTags[i];
				}
				else
				{
					fullTagString += "." + subTags[i];
					fullTagName = fullTagString;
				}

				List<GameplayTagNode> childTags = curNode.ChildTagNodes;
				int insertionIdx = InsertTagIntoNodeArray(shortTagName, fullTagName, curNode, childTags, sourceName, tagRow.DevComment, isExplicitTag);

				curNode = childTags[insertionIdx];
			}
		}

		private int InsertTagIntoNodeArray(string tag, string fullTag, GameplayTagNode parentNode, List<GameplayTagNode> nodeArray, string sourceName, in string devComment, bool isExplicitTag)
		{
			int foundNodeIdx = -1;
			int whereToInsert = -1;

			int lowerBoundIndex = nodeArray.FindIndex((e) => { return string.Compare(e.SimpleTagName, tag) >= 0; });
			if (lowerBoundIndex < 0)
			{
				lowerBoundIndex = nodeArray.Count;
			}

			if (lowerBoundIndex < nodeArray.Count)
			{
				GameplayTagNode curNode = nodeArray[lowerBoundIndex];
				if (curNode.SimpleTagName == tag)
				{
					foundNodeIdx = lowerBoundIndex;
				}
				else
				{
					whereToInsert = lowerBoundIndex;
				}
			}

			if (foundNodeIdx == -1)
			{
				if (whereToInsert == -1)
				{
					whereToInsert = nodeArray.Count;
				}

				GameplayTagNode tagNode = new(tag, fullTag, parentNode != GameplayRootTag ? parentNode : null, isExplicitTag);

				nodeArray.Insert(whereToInsert, tagNode);

				foundNodeIdx = whereToInsert;

				GameplayTag gameplayTag = tagNode.CompleteTag;

				GameplayTagNodeMap.Add(gameplayTag, tagNode);
			}

#if UNITY_EDITOR
			nodeArray[foundNodeIdx].SourceNames.AddUnique(sourceName);

			if (string.IsNullOrEmpty(nodeArray[foundNodeIdx].DevComment) && !string.IsNullOrEmpty(devComment))
			{
				nodeArray[foundNodeIdx].DevComment = devComment;
			}
#endif

			return foundNodeIdx;
		}

		public void GetFilteredGameplayRootTags(List<TreeViewItemData<GameplayTagNode>> outTagArray)
		{
			List<GameplayTagNode> gameplayRootTags = GameplayRootTag.ChildTagNodes;

			outTagArray.Clear();

			int index = 0;
			foreach (GameplayTagNode tagNode in gameplayRootTags)
			{
				RecursiveRootTagSearch(tagNode, outTagArray, ref index);
			}
		}

		public void RecursiveRootTagSearch(GameplayTagNode rootNode, List<TreeViewItemData<GameplayTagNode>> outTagArray, ref int index)
		{
			List<GameplayTagNode> tagNodes = rootNode.ChildTagNodes;
			if (tagNodes.Count == 0)
			{
				outTagArray.Add(new TreeViewItemData<GameplayTagNode>(index++, rootNode));
			}
			else
			{
				List<TreeViewItemData<GameplayTagNode>> children = new();
				foreach (GameplayTagNode tagNode in tagNodes)
				{
					RecursiveRootTagSearch(tagNode, children, ref index);
				}
				outTagArray.Add(new TreeViewItemData<GameplayTagNode>(index++, rootNode, children));
			}
		}

		public void GetFilteredGameplayRootTags(List<GameplayTagNode> outTagArray)
		{
			List<GameplayTagNode> gameplayRootTags = GameplayRootTag.ChildTagNodes;

			outTagArray.Clear();

			for (int i = 0; i < gameplayRootTags.Count; i++)
			{
				RecursiveRootTagSearch(gameplayRootTags[i].ChildTagNodes, outTagArray);
			}

			if (outTagArray.Count == 0)
			{
				outTagArray.AddRange(gameplayRootTags);
			}
		}

		public void RecursiveRootTagSearch(in List<GameplayTagNode> gameplayRootTags, List<GameplayTagNode> outTagArray)
		{
			for (int i = 0; i < gameplayRootTags.Count; i++)
			{
				RecursiveRootTagSearch(gameplayRootTags[i].ChildTagNodes, outTagArray);
			}
		}

		public void RecursiveRootTagSearch(string filterString, List<GameplayTagNode> gameplayRootTag, List<GameplayTagNode> outTagArray)
		{
			string currentFilter, restOfFilter;
			var splitFilter = filterString.Split('.');
			currentFilter = splitFilter[0];
			restOfFilter = splitFilter[1];

			for (int i = 0; i < gameplayRootTag.Count; i++)
			{
				var rootTagName = gameplayRootTag[i].SimpleTagName;
				if (rootTagName.Equals(currentFilter))
				{
					if (string.IsNullOrEmpty(restOfFilter))
					{
						outTagArray.Add(gameplayRootTag[i]);
					}
					else
					{
						RecursiveRootTagSearch(restOfFilter, gameplayRootTag[i].ChildTagNodes, outTagArray);
					}
				}
			}
		}
	}
}