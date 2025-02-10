using GameplayTags;

public interface IGameplayTagAssetInterface
{
    virtual void GetOwnedGameplayTags(GameplayTagContainer tagContainer) { }

    virtual bool HasMatchingGameplayTag(GameplayTag tagToCheck)
    {
        GameplayTagContainer tagContainer = new GameplayTagContainer();
        GetOwnedGameplayTags(tagContainer);

        return tagContainer.HasTag(tagToCheck);
    }

    virtual bool HasAnyMatchingGameplayTags(GameplayTagContainer tagContainer)
    {
        GameplayTagContainer ownedTags = new GameplayTagContainer();
        GetOwnedGameplayTags(ownedTags);

        return ownedTags.HasAny(tagContainer);
    }

    virtual bool HasAllMatchingGameplayTags(GameplayTagContainer tagContainer)
    {
        GameplayTagContainer ownedTags = new GameplayTagContainer();
        GetOwnedGameplayTags(ownedTags);

        return ownedTags.HasAll(tagContainer);
    }
}
