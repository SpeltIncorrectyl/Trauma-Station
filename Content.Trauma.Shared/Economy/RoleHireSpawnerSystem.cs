using Content.Shared.Item.ItemToggle.Components;

namespace Content.Trauma.Shared.Economy;

public abstract class SharedRoleHireSpawnerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoleHireSpawnerComponent, ItemToggledEvent>(OnToggled);
    }

    private void OnToggled(Entity<RoleHireSpawnerComponent> entity, ref ItemToggledEvent args)
    {
        if (!args.Activated)
            return;

        entity.Comp.User = args.User;
        Dirty(entity);

        if (!TryComp<ItemToggleComponent>(entity.Owner, out var toggle))
            return;

        // don't want to be able to turn the spawner back off
        toggle.OnActivate = false;
        toggle.OnUse = false;
        Dirty(entity.Owner, toggle);
    }
}
