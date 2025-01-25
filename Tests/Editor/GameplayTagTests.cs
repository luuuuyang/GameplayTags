using NUnit.Framework;
using System.Collections.Generic;
using Unity.PerformanceTesting;

namespace GameplayTags.Tests
{
    public class GameplayTagTests
    {
        [OneTimeSetUp]
        public void InitTestSuite()
        {
            // 注册测试标签
            List<string> testTags = new()
            {
                "Effect.Damage",
                "Effect.Damage.Basic",
                "Effect.Damage.Type1",
                "Effect.Damage.Type2",
                "Effect.Damage.Reduce",
                "Effect.Damage.Buffable",
                "Effect.Damage.Buff",
                "Effect.Damage.Physical",
                "Effect.Damage.Fire",
                "Effect.Damage.Buffed.FireBuff",
                "Effect.Damage.Mitigated.Armor",
                "Effect.Lifesteal",
                "Effect.Shield",
                "Effect.Buff",
                "Effect.Immune",
                "Effect.FireDamage",
                "Effect.Shield.Absorb",
                "Effect.Protect.Damage",
                "Stackable",
                "Stack.DiminishingReturns",
                "GameplayCue.Burning",
                "Expensive.Status.Tag.Type.1",
                "Expensive.Status.Tag.Type.2",
                "Expensive.Status.Tag.Type.3",
                "Expensive.Status.Tag.Type.4",
                "Expensive.Status.Tag.Type.5",
                "Expensive.Status.Tag.Type.6",
                "Expensive.Status.Tag.Type.7",
                "Expensive.Status.Tag.Type.8",
                "Expensive.Status.Tag.Type.9",
                "Expensive.Status.Tag.Type.10",
                "Expensive.Status.Tag.Type.11",
                "Expensive.Status.Tag.Type.12",
                "Expensive.Status.Tag.Type.13",
                "Expensive.Status.Tag.Type.14",
                "Expensive.Status.Tag.Type.15",
                "Expensive.Status.Tag.Type.16",
                "Expensive.Status.Tag.Type.17",
                "Expensive.Status.Tag.Type.18",
                "Expensive.Status.Tag.Type.19",
                "Expensive.Status.Tag.Type.20",
                "Expensive.Status.Tag.Type.21",
                "Expensive.Status.Tag.Type.22",
                "Expensive.Status.Tag.Type.23",
                "Expensive.Status.Tag.Type.24",
                "Expensive.Status.Tag.Type.25",
                "Expensive.Status.Tag.Type.26",
                "Expensive.Status.Tag.Type.27",
                "Expensive.Status.Tag.Type.28",
                "Expensive.Status.Tag.Type.29",
                "Expensive.Status.Tag.Type.30",
                "Expensive.Status.Tag.Type.31",
                "Expensive.Status.Tag.Type.32",
                "Expensive.Status.Tag.Type.33",
                "Expensive.Status.Tag.Type.34",
                "Expensive.Status.Tag.Type.35",
                "Expensive.Status.Tag.Type.36",
                "Expensive.Status.Tag.Type.37",
                "Expensive.Status.Tag.Type.38",
                "Expensive.Status.Tag.Type.39",
                "Expensive.Status.Tag.Type.40",
            };

            GameplayTagsManager.Instance.PopulateTreeFromDataTable(testTags);
        }

        [Test]
        public void SimpleTest()
        {
            string tagName = "Stack.DiminishingReturns";
            GameplayTag tag = GameplayTagsManager.Instance.RequestGameplayTag(tagName);
            Assert.AreEqual(tagName, tag.TagName);
        }

        [Test]
        public void TagComparisonTest()
        {
            GameplayTag effectDamageTag = GetTagForString("Effect.Damage");
            GameplayTag effectDamage1Tag = GetTagForString("Effect.Damage.Type1");
            GameplayTag effectDamage2Tag = GetTagForString("Effect.Damage.Type2");
            GameplayTag cueTag = GetTagForString("GameplayCue.Burning");
            GameplayTag emptyTag = new();

            Assert.IsTrue(effectDamage1Tag == effectDamage1Tag);
            Assert.IsTrue(effectDamage1Tag != effectDamage2Tag);
            Assert.IsTrue(effectDamage1Tag != effectDamageTag);

            Assert.IsTrue(effectDamage1Tag.MatchesTag(effectDamageTag));
            Assert.IsTrue(!effectDamage1Tag.MatchesTagExact(effectDamageTag));
            Assert.IsTrue(!effectDamage1Tag.MatchesTag(emptyTag));
            Assert.IsTrue(!effectDamage1Tag.MatchesTagExact(emptyTag));
            Assert.IsTrue(!emptyTag.MatchesTag(emptyTag));
            Assert.IsTrue(!emptyTag.MatchesTagExact(emptyTag));

            Assert.IsTrue(effectDamage1Tag.RequestDirectParent() == effectDamageTag);
        }

        [Test]
        public void TagContainerTest()
        {
            GameplayTag effectDamageTag = GetTagForString("Effect.Damage");
            GameplayTag effectDamage1Tag = GetTagForString("Effect.Damage.Type1");
            GameplayTag effectDamage2Tag = GetTagForString("Effect.Damage.Type2");
            GameplayTag cueTag = GetTagForString("GameplayCue.Burning");
            GameplayTag emptyTag = new();

            GameplayTagContainer emptyContainer = new();

            GameplayTagContainer tagContainer = new();
            tagContainer.AddTag(effectDamage1Tag);
            tagContainer.AddTag(cueTag);

            GameplayTagContainer reverseContainer = new();
            reverseContainer.AddTag(cueTag);
            reverseContainer.AddTag(effectDamage1Tag);

            GameplayTagContainer tagContainer2 = new();
            tagContainer2.AddTag(effectDamage2Tag);
            tagContainer2.AddTag(cueTag);

            Assert.IsTrue(tagContainer == tagContainer);
            Assert.IsTrue(tagContainer == reverseContainer);
            Assert.IsTrue(tagContainer != tagContainer2);

            // 容器复制测试
            GameplayTagContainer tagContainerCopy = new(tagContainer);
            Assert.IsTrue(tagContainerCopy == tagContainer);
            Assert.IsTrue(tagContainerCopy != tagContainer2);

            tagContainerCopy.Reset();
            tagContainerCopy.AppendTags(tagContainer);

            Assert.IsTrue(tagContainerCopy == tagContainer);
            Assert.IsTrue(tagContainerCopy != tagContainer2);

            Assert.IsTrue(tagContainer.HasAny(tagContainer2));
            Assert.IsTrue(tagContainer.HasAnyExact(tagContainer2));
            Assert.IsTrue(!tagContainer.HasAll(tagContainer2));
            Assert.IsTrue(!tagContainer.HasAllExact(tagContainer2));
            Assert.IsTrue(tagContainer.HasAll(tagContainerCopy));
            Assert.IsTrue(tagContainer.HasAllExact(tagContainerCopy));

            Assert.IsTrue(tagContainer.HasAll(emptyContainer));
            Assert.IsTrue(tagContainer.HasAllExact(emptyContainer));
            Assert.IsTrue(!tagContainer.HasAny(emptyContainer));
            Assert.IsTrue(!tagContainer.HasAnyExact(emptyContainer));

            Assert.IsTrue(emptyContainer.HasAll(emptyContainer));
            Assert.IsTrue(emptyContainer.HasAllExact(emptyContainer));
            Assert.IsTrue(!emptyContainer.HasAny(emptyContainer));
            Assert.IsTrue(!emptyContainer.HasAnyExact(emptyContainer));

            Assert.IsTrue(!emptyContainer.HasAll(tagContainer));
            Assert.IsTrue(!emptyContainer.HasAllExact(tagContainer));
            Assert.IsTrue(!emptyContainer.HasAny(tagContainer));
            Assert.IsTrue(!emptyContainer.HasAnyExact(tagContainer));

            Assert.IsTrue(tagContainer.HasTag(effectDamageTag));
            Assert.IsTrue(!tagContainer.HasTagExact(effectDamageTag));
            Assert.IsTrue(!tagContainer.HasTag(emptyTag));
            Assert.IsTrue(!tagContainer.HasTagExact(emptyTag));

            Assert.IsTrue(effectDamage1Tag.MatchesAny(new GameplayTagContainer(effectDamageTag)));
            Assert.IsTrue(!effectDamage1Tag.MatchesAnyExact(new GameplayTagContainer(effectDamageTag)));

            Assert.IsTrue(effectDamage1Tag.MatchesAny(tagContainer));

            GameplayTagContainer filteredTagContainer = tagContainer.FilterExact(new GameplayTagContainer(effectDamageTag));
            Assert.IsTrue(!filteredTagContainer.HasTagExact(cueTag));
            Assert.IsTrue(!filteredTagContainer.HasTagExact(effectDamage1Tag));

            filteredTagContainer = tagContainer.FilterExact(new GameplayTagContainer(effectDamage1Tag));
            Assert.IsTrue(!filteredTagContainer.HasTagExact(cueTag));
            Assert.IsTrue(filteredTagContainer.HasTagExact(effectDamage1Tag));

            filteredTagContainer.Reset();
            filteredTagContainer.AppendMatchingTags(tagContainer, tagContainer2);

            Assert.IsTrue(filteredTagContainer.HasTagExact(cueTag));
            Assert.IsTrue(!filteredTagContainer.HasTagExact(effectDamage1Tag));

            GameplayTagContainer singleTagContainer = effectDamage1Tag.SingleTagContainer;
            GameplayTagContainer parentContainer = effectDamage1Tag.GetGameplayTagParents();

            Assert.IsTrue(singleTagContainer.HasTagExact(effectDamage1Tag));
            Assert.IsTrue(singleTagContainer.HasTag(effectDamageTag));
            Assert.IsTrue(!singleTagContainer.HasTagExact(effectDamageTag));

            Assert.IsTrue(parentContainer.HasTagExact(effectDamage1Tag));
            Assert.IsTrue(parentContainer.HasTag(effectDamageTag));
            Assert.IsTrue(parentContainer.HasTagExact(effectDamageTag));
        }

        [Test, Performance]
        public void PerformanceTest()
        {
            GameplayTag effectDamageTag = GetTagForString("Effect.Damage");
            GameplayTag effectDamage1Tag = GetTagForString("Effect.Damage.Type1");
            GameplayTag effectDamage2Tag = GetTagForString("Effect.Damage.Type2");
            GameplayTag cueTag = GetTagForString("GameplayCue.Burning");

            GameplayTagContainer tagContainer = null;

            bool result = true;
            const int smallTest = 1000, largeTest = 10000;

            Measure.Method(() =>
            {
                for (int i = 0; i < largeTest; i++)
                {
                    GameplayTagsManager.Instance.RequestGameplayTag("Effect.Damage");
                }
            }).Run();

            Measure.Method(() =>
            {
                for (int i = 0; i < smallTest; i++)
                {
                    tagContainer = new();
                    tagContainer.AddTag(effectDamage1Tag);
                    tagContainer.AddTag(effectDamage2Tag);
                    tagContainer.AddTag(cueTag);
                    for (int j = 1; j <= 40; j++)
                    {
                        tagContainer.AddTag(GetTagForString($"Expensive.Status.Tag.Type.{j}"));
                    }
                }
            }).Run();

            Measure.Method(() =>
            {
                for (int i = 0; i < smallTest; i++)
                {
                    GameplayTagContainer tagContainerNew = new(effectDamageTag);
                    tagContainerNew.CopyFrom(tagContainer);

                    GameplayTagContainer movedContainer = new(tagContainerNew);

                    result &= movedContainer.Count == tagContainer.Count;
                }
            }).Run();

            Measure.Method(() =>
            {
                for (int i = 0; i < smallTest; i++)
                {
                    GameplayTagContainer tagContainerNew = new();

                    foreach (GameplayTag tag in tagContainer)
                    {
                        tagContainerNew.AddTag(tag);
                    }
                }
            }).Run();

            Measure.Method(() =>
            {
                for (int i = 0; i < smallTest; i++)
                {
                    GameplayTagContainer tagContainerNew = new(effectDamage1Tag);

                    tagContainerNew.AppendTags(tagContainer);
                }
            }).Run();

            Measure.Method(() =>
            {
                for (int i = 0; i < smallTest; i++)
                {
                    GameplayTagContainer tagContainerNew = new(tagContainer);

                    tagContainerNew.AppendTags(tagContainer);
                }
            }).Run();

            Measure.Method(() =>
            {
                for (int i = 0; i < largeTest; i++)
                {
                    GameplayTagContainer tagContainerNew = new(effectDamage1Tag.SingleTagContainer);
                }
            }).Run();

            Measure.Method(() =>
            {
                for (int i = 0; i < largeTest; i++)
                {
                    GameplayTagContainer tagContainerParents = new(effectDamage1Tag.GetGameplayTagParents());
                }
            }).Run();

            GameplayTagContainer tagContainer2 = new();
            tagContainer2.AddTag(effectDamage1Tag);
            tagContainer2.AddTag(effectDamage2Tag);
            tagContainer2.AddTag(cueTag);

            Measure.Method(() =>
            {
                for (int i = 0; i < largeTest; i++)
                {
                    result &= effectDamage1Tag.MatchesAnyExact(tagContainer);
                }
            }).Run();

            Measure.Method(() =>
            {
                for (int i = 0; i < largeTest; i++)
                {
                    result &= effectDamage1Tag.MatchesAny(tagContainer);
                }
            }).Run();

            Measure.Method(() =>
            {
                for (int i = 0; i < largeTest; i++)
                {
                    result &= tagContainer.HasTagExact(effectDamage1Tag);
                }
            }).Run();

            Measure.Method(() =>
            {
                for (int i = 0; i < largeTest; i++)
                {
                    result &= tagContainer.HasTag(effectDamage1Tag);
                }
            }).Run();

            Measure.Method(() =>
            {
                for (int i = 0; i < largeTest; i++)
                {
                    result &= tagContainer.HasAll(tagContainer2);
                }
            }).Run();

            Measure.Method(() =>
            {
                for (int i = 0; i < largeTest; i++)
                {
                    result &= tagContainer.HasAny(tagContainer2);
                }
            }).Run();

            Assert.IsTrue(result);
        }

        private GameplayTag GetTagForString(in string tagName)
        {
            return GameplayTagsManager.Instance.RequestGameplayTag(tagName);
        }
    }
}
