using Content.Shared.Store;
using Robust.Server.Containers;
using Robust.Shared.Containers;

namespace Content.Trauma.Server.Store.Condititions;

/// <summary>
/// Filters out listings based on if a specified container on the store entity is empty or not.
/// </summary>
public sealed partial class ContainerNotEmptyCondition : ListingCondition
{
    [DataField]
    public string ContainerId = String.Empty;

    public override bool Condition(ListingConditionArgs args)
    {
        if (args.StoreEntity is null)
            return false;

        var containerSystem = args.EntityManager.System<ContainerSystem>();
        var container = containerSystem.EnsureContainer<Container>(args.StoreEntity.Value, ContainerId);
        return container.Count >= 1;
    }
}
