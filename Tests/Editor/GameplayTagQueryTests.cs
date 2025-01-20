using NUnit.Framework;
using System.Collections.Generic;

namespace GameplayTags.Tests
{
    public class GameplayTagQueryTests
    {
        private GameplayTagContainer AllGameplayTags = new();
        private GameplayTag RandomTag;
        private GameplayTagContainer EmptyTagContainer;
        private GameplayTagContainer NonEmptyContainer;

        [OneTimeSetUp]
        public void Setup()
        {
            GameplayTagsManager.Instance.RequestAllGameplayTags(AllGameplayTags, true);
            Assert.That(!AllGameplayTags.IsEmpty(), "GameplayTags are defined for this Project");

            // 设置测试数据
            RandomTag = AllGameplayTags.First();
            EmptyTagContainer = new GameplayTagContainer();
            NonEmptyContainer = new GameplayTagContainer(RandomTag);
        }

        [Test]
        public void EmptyQueryTest()
        {
            // 完全空的查询
            GameplayTagQuery query = new();
            Assert.IsFalse(query.Matches(EmptyTagContainer), "Match Empty Query w/ Empty Container");
            Assert.IsFalse(query.Matches(NonEmptyContainer), "Match Empty Query w/ Non-Empty Container");
        }

        [Test]
        public void EmptyExpressionQueryTest()
        {
            // 表达式存在但没有有效标签的查询
            GameplayTagQuery query = new();
            GameplayTagQueryExpression tempExpression = new();

            // AllExprMatch
            tempExpression.AllExprMatch();
            query.Build(tempExpression, "Empty Tag Query Expression - AllExprMatch");
            Assert.IsTrue(query.Matches(EmptyTagContainer), "Match Empty AllExprMatch w/ Empty Container");
            Assert.IsTrue(query.Matches(NonEmptyContainer), "Match Empty AllExprMatch w/ Non-Empty Container");

            // AllTagsMatch
            tempExpression.AllTagsMatch();
            query.Build(tempExpression, "Empty Tag Query Expression - AllTagsMatch");
            Assert.IsTrue(query.Matches(EmptyTagContainer), "Match Empty AllTagsMatch w/ Empty Container");
            Assert.IsTrue(query.Matches(NonEmptyContainer), "Match Empty AllTagsMatch w/ Non-Empty Container");

            // AnyExprMatch
            tempExpression.AnyExprMatch();
            query.Build(tempExpression, "Empty Tag Query Expression - AnyExprMatch");
            Assert.IsFalse(query.Matches(EmptyTagContainer), "Match Empty AnyExprMatch w/ Empty Container");
            Assert.IsFalse(query.Matches(NonEmptyContainer), "Match Empty AnyExprMatch w/ Non-Empty Container");

            tempExpression.AnyTagsMatch();
            query.Build(tempExpression, "Empty Tag Query Expression - AnyTagsMatch");
            Assert.IsFalse(query.Matches(EmptyTagContainer), "Match Empty AnyTagsMatch w/ Empty Container");
            Assert.IsFalse(query.Matches(NonEmptyContainer), "Match Empty AnyTagsMatch w/ Non-Empty Container");

            tempExpression.NoExprMatch();
            query.Build(tempExpression, "Empty Tag Query Expression - NoExprMatch");
            Assert.IsTrue(query.Matches(EmptyTagContainer), "Match Empty NoExprMatch w/ Empty Container");
            Assert.IsTrue(query.Matches(NonEmptyContainer), "Match Empty NoExprMatch w/ Non-Empty Container");

            // NoTagsMatch
            tempExpression.NoTagsMatch();
            query.Build(tempExpression, "Empty Tag Query Expression - NoTagsMatch");
            Assert.IsTrue(query.Matches(EmptyTagContainer), "Match Empty NoTagsMatch w/ Empty Container");
            Assert.IsTrue(query.Matches(NonEmptyContainer), "Match Empty NoTagsMatch w/ Non-Empty Container");
        }

        [Test]
        public void NonEmptyExpressionQueryTest()
        {
            // 表达式存在且有有效标签的查询
            GameplayTagQuery query = new();
            GameplayTagQueryExpression tempExpression = new()
            {
                TagSet = new List<GameplayTag> { RandomTag }
            };

            // AllExprMatch
            tempExpression.AllExprMatch();
            query.Build(tempExpression, "Non-Empty Tag Query Expression - AllExprMatch");
            Assert.IsTrue(query.Matches(EmptyTagContainer), "Match Non-Empty AllExprMatch w/ Empty Container");
            Assert.IsTrue(query.Matches(NonEmptyContainer), "Match Non-Empty AllExprMatch w/ Non-Empty Container");

            // AllTagsMatch
            tempExpression.AllTagsMatch();
            query.Build(tempExpression, "Non-Empty Tag Query Expression - AllTagsMatch");
            Assert.IsFalse(query.Matches(EmptyTagContainer), "Match Non-Empty AllTagsMatch w/ Empty Container");
            Assert.IsTrue(query.Matches(NonEmptyContainer), "Match Non-Empty AllTagsMatch w/ Non-Empty Container");

            // AnyExprMatch
            tempExpression.AnyExprMatch();
            query.Build(tempExpression, "Non-Empty Tag Query Expression - AnyExprMatch");
            Assert.IsFalse(query.Matches(EmptyTagContainer), "Match Non-Empty AnyExprMatch w/ Empty Container");
            Assert.IsFalse(query.Matches(NonEmptyContainer), "Match Non-Empty AnyExprMatch w/ Non-Empty Container");

            // AnyTagsMatch
            tempExpression.AnyTagsMatch();
            query.Build(tempExpression, "Non-Empty Tag Query Expression - AnyTagsMatch");
            Assert.IsFalse(query.Matches(EmptyTagContainer), "Match Non-Empty AnyTagsMatch w/ Empty Container");
            Assert.IsTrue(query.Matches(NonEmptyContainer), "Match Non-Empty AnyTagsMatch w/ Non-Empty Container");

            // NoExprMatch
            tempExpression.NoExprMatch();
            query.Build(tempExpression, "Non-Empty Tag Query Expression - NoExprMatch");
            Assert.IsTrue(query.Matches(EmptyTagContainer), "Match Non-Empty NoExprMatch w/ Empty Container");
            Assert.IsTrue(query.Matches(NonEmptyContainer), "Match Non-Empty NoExprMatch w/ Non-Empty Container");

            // NoTagsMatch
            tempExpression.NoTagsMatch();
            query.Build(tempExpression, "Non-Empty Tag Query Expression - NoTagsMatch");
            Assert.IsTrue(query.Matches(EmptyTagContainer), "Match Non-Empty NoTagsMatch w/ Empty Container");
            Assert.IsFalse(query.Matches(NonEmptyContainer), "Match Non-Empty NoTagsMatch w/ Non-Empty Container");
        }
    }
}
