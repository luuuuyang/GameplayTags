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

		public GameplayTagTableRow(string tag, string devComment = "")
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
		Native,
		DefaultTagList,
		TagList,
		DataTable,
		Invalid,
	}

	public class GameplayTagNode
	{
		public GameplayTag CompleteTag => CompleteTagWithParents.Count > 0 ? CompleteTagWithParents.GameplayTags[0] : GameplayTag.EmptyTag;
		public string CompleteTagName => CompleteTag.TagName;
		public string SimpleTagName => Tag;
		public GameplayTagNode ParentTagNode => ParentNode;
		public List<GameplayTagNode> ChildTagNodes => ChildTags;
		public GameplayTagContainer SingleTagContainer => CompleteTagWithParents;

		private string Tag;
		private GameplayTagContainer CompleteTagWithParents = new();
		private List<GameplayTagNode> ChildTags = new();
		private GameplayTagNode ParentNode;

#if UNITY_EDITOR
		public List<string> SourceNames = new();
		public bool IsExplicitTag;
		public string DevComment;
#endif

		public GameplayTagNode() { }

		public GameplayTagNode(string tag, string fullTag, GameplayTagNode parentNode, bool isExplicitTag = false)
		{
			Tag = tag;
			ParentNode = parentNode;

			CompleteTagWithParents.GameplayTags.Add(new GameplayTag(fullTag));

			if (ParentNode is not null && !string.IsNullOrEmpty(ParentNode.SimpleTagName))
			{
				GameplayTagContainer parentContainer = ParentNode.SingleTagContainer;

				CompleteTagWithParents.ParentTags.Add(parentContainer.GameplayTags[0]);
				CompleteTagWithParents.ParentTags.AddRange(parentContainer.ParentTags);
			}

#if UNITY_EDITOR
			IsExplicitTag = isExplicitTag;
#endif
		}

		public void ResetNode()
		{
			Tag = null;
			CompleteTagWithParents.Reset();

			for (int childIdx = 0; childIdx < ChildTags.Count; childIdx++)
			{
				ChildTags[childIdx].ResetNode();
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
		public GameplayTagsList SourceTagList = new();
		public static string DefaultName => NAME_DefaultGameplayTagsSO;
		public static string NativeName => NAME_NativeGameplayTags;

		private static readonly string NAME_DefaultGameplayTagsSO = "DefaultGameplayTags.asset";
		private static readonly string NAME_NativeGameplayTags = "Native";
	}

	public class GameplayTagsManager : Singleton<GameplayTagsManager>
	{
		private HashSet<string> LegacyNativeTags = new();
		private GameplayTagNode GameplayRootTag;

		private Dictionary<GameplayTag, GameplayTagNode> GameplayTagNodeMap = new();

		private Dictionary<string, GameplayTagSource> TagSources = new();

		private string InvalidTagCharacters;

		private HashSet<string> MissingTagNames = new();

		private bool ShouldDeferGameplayTagTreeRebuilds;

		private bool DoneAddingNativeTags;

		public override void InitializeSingleton()
		{
			ConstructGameplayTagTree();
		}

		public void PopulateTreeFromDataTable(List<string> tags)
		{
			Debug.Assert(GameplayRootTag is not null, "ConstructGameplayTagTree() must be called before PopulateTreeFromDataTable()");

			string sourceName = "TempDataTable";

			GameplayTagSource foundSource = FindOrAddTagSource(sourceName, GameplayTagSourceType.DataTable);

			foreach (string tag in tags)
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
			if (GameplayTagNodeMap.TryGetValue(gameplayTag, out GameplayTagNode node))
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

			if (errorIfNotFound)
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

		public GameplayTagContainer RequestGameplayTagParents(in GameplayTag gameplayTag)
		{
			GameplayTagContainer parentTags = GetSingleTagContainer(gameplayTag);

			if (parentTags is not null)
			{
				return parentTags.GetGameplayTagParents();
			}
			return new GameplayTagContainer();
		}

		public bool ExtractParentTags(in GameplayTag gameplayTag, List<GameplayTag> uniqueParentTags)
		{
			if (!gameplayTag.IsValid())
			{
				return false;
			}

			List<GameplayTag> validationCopy = new();

			int oldSize = uniqueParentTags.Count;
			string rawTag = gameplayTag.TagName;

			if (GameplayTagNodeMap.TryGetValue(gameplayTag, out GameplayTagNode node))
			{
				GameplayTagContainer singleContainer = node.SingleTagContainer;
				foreach (GameplayTag parentTag in singleContainer.ParentTags)
				{
					uniqueParentTags.AddUnique(parentTag);
				}
			}
			else
			{
				gameplayTag.ParseParentTags(uniqueParentTags);
			}


			return uniqueParentTags.Count != oldSize;
		}

		public void RequestAllGameplayTags(GameplayTagContainer tagContainer, bool onlyIncludeDictionaryTags = false)
		{
			foreach (var nodePair in GameplayTagNodeMap)
			{
				GameplayTagNode tagNode = nodePair.Value;
				if (!onlyIncludeDictionaryTags || tagNode.IsExplicitTag)
				{
					tagContainer.AddTagFast(tagNode.CompleteTag);
				}
			}
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
			if (GameplayTagNodeMap.TryGetValue(gameplayTag, out GameplayTagNode node))
			{
				return node;
			}
			return null;
		}

		public GameplayTagNode FindTagNode(string tagName)
		{
			GameplayTag possibleTag = new(tagName);
			return FindTagNode(possibleTag);
		}

		public GameplayTagSource FindTagSource(string tagSourceName)
		{
			if (TagSources.TryGetValue(tagSourceName, out GameplayTagSource source))
			{
				return source;
			}
			return null;
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
			TagSources.Add(tagSourceName, newSource);

			if (sourceType == GameplayTagSourceType.Native)
			{
				newSource.SourceTagList = ScriptableObject.CreateInstance<GameplayTagsList>();
			}
			else if (sourceType == GameplayTagSourceType.DefaultTagList)
			{
				newSource.SourceTagList = AssetDatabase.LoadAssetAtPath<GameplayTagsList>("Packages/com.luuuuyang.gameplaytags/Editor/Config/DefaultGameplayTags.asset");
			}

			return newSource;
		}

		public void ConstructGameplayTagTree()
		{
			if (GameplayRootTag is null)
			{
				GameplayRootTag = new GameplayTagNode();

				GameplayTagSettings mutableDefault = AssetDatabase.LoadAssetAtPath<GameplayTagSettings>("Packages/com.luuuuyang.gameplaytags/Editor/Config/DefaultGameplayTags.asset");

				InvalidTagCharacters = mutableDefault.InvalidTagCharacters;
				InvalidTagCharacters += "\r\n\t";

				{
					foreach (string tagToAdd in LegacyNativeTags)
					{
						AddTagTableRow(new GameplayTagTableRow(tagToAdd), GameplayTagSource.NativeName);
					}

					foreach (NativeGameplayTag nativeTag in NativeGameplayTag.RegisteredNativeTags)
					{
						FindOrAddTagSource(nativeTag.ModuleName, GameplayTagSourceType.Native);
						AddTagTableRow(nativeTag.GameplayTagTableRow, nativeTag.ModuleName);
					}
				}

				FindOrAddTagSource(GameplayTagSource.NativeName, GameplayTagSourceType.Native);

#if UNITY_EDITOR
				mutableDefault.SortTags();
#endif

				string TagSource = GameplayTagSource.DefaultName;
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
			fullTagString = null;

			int numSubTags = subTags.Length;

			for (int subTagIdx = 0; subTagIdx < numSubTags; subTagIdx++)
			{
				bool isExplicitTag = subTagIdx == (numSubTags - 1);
				string shortTagName = subTags[subTagIdx];
				string fullTagName;

				if (isExplicitTag)
				{
					fullTagName = originalTagName;
				}
				else if (subTagIdx == 0)
				{
					fullTagName = shortTagName;
					fullTagString = subTags[subTagIdx];
				}
				else
				{
					fullTagString += "." + subTags[subTagIdx];
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

			int lowerBoundIndex = nodeArray.FindIndex(node => { return string.Compare(node.SimpleTagName, tag) >= 0; });

			if (lowerBoundIndex >= 0 && lowerBoundIndex < nodeArray.Count)
			{
				GameplayTagNode curNode = nodeArray[lowerBoundIndex];
				if (curNode.SimpleTagName == tag)
				{
					foundNodeIdx = lowerBoundIndex;
#if UNITY_EDITOR
					if (isExplicitTag)
					{
						if (curNode.IsExplicitTag && isExplicitTag)
						{

						}
						curNode.IsExplicitTag = curNode.IsExplicitTag || isExplicitTag;
					}
#endif
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

				Debug.Assert(gameplayTag.TagName == fullTag);

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

		public GameplayTag AddNativeGameplayTag(string tagName, in string tagDevComment)
		{
			if (string.IsNullOrEmpty(tagName))
			{
				return new GameplayTag();
			}

			if (!DoneAddingNativeTags)
			{
				GameplayTag newTag = new(tagName);

				AddTagTableRow(new GameplayTagTableRow(tagName, tagDevComment), GameplayTagSource.NativeName);

				return newTag;
			}

			return new GameplayTag();
		}

		public void AddNativeGameplayTag(NativeGameplayTag tagSource)
		{
			GameplayTagSource nativeSource = FindOrAddTagSource(tagSource.ModuleName, GameplayTagSourceType.Native);
			nativeSource.SourceTagList.GameplayTagList.Add(tagSource.GameplayTagTableRow);

			AddTagTableRow(tagSource.GameplayTagTableRow, nativeSource.SourceName);

			HandleGameplayTagTreeChanged(false);
		}

		public void RemoveNativeGameplayTag(NativeGameplayTag tagSource)
		{

			HandleGameplayTagTreeChanged(true);
		}

		public void BroadcastOnGameplayTagTreeChanged()
		{

		}

		public void HandleGameplayTagTreeChanged(bool recreateTree)
		{
			if (recreateTree && !ShouldDeferGameplayTagTreeRebuilds)
			{
#if UNITY_EDITOR
				EditorRefreshGameplayTagTree();
				return;
#endif
				DestroyGameplayTagTree();
				ConstructGameplayTagTree();
			}
			else
			{
				BroadcastOnGameplayTagTreeChanged();
			}
		}

	}
}