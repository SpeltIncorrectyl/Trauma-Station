using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Content.Shared.Whitelist;
using Content.Trauma.Server.Store.Condititions;
using Robust.Shared.Containers;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Trauma.Shared.Store;

public sealed class AutomatedVendorSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AutomatedVendorComponent, DispenseFromContainerEvent>(OnDispense);
        SubscribeLocalEvent<AutomatedVendorComponent, InteractUsingEvent>(OnInteract);
    }

    private void OnInteract(Entity<AutomatedVendorComponent> entity, ref InteractUsingEvent args)
    {
        foreach (var slot in entity.Comp.Slots)
        {
            if (slot.Whitelist is not null && !_whitelist.IsValid(slot.Whitelist, args.Used))
                continue;
            args.Handled = true;
            var container = _container.EnsureContainer<Container>(entity, slot.ContainerId);
            _container.Insert(args.Used, container);
            _popup.PopupClient(Loc.GetString("auto-vendor-insertion-message", ("item", MetaData(args.Used).EntityName), ("vendor", MetaData(entity.Owner).EntityName)), args.User);
            return;
        }
    }

    private void OnDispense(Entity<AutomatedVendorComponent> entity, ref DispenseFromContainerEvent args)
    {
        var container = _container.EnsureContainer<Container>(entity, args.ContainerId);

        for (var i = 0; i < args.Count; i++)
        {
            var item = container.ContainedEntities.FirstOrNull();
            if (item is null)
                return;
            _container.TryRemoveFromContainer(item.Value);
        }
    }
}

public record struct DispenseFromContainerEvent(string ContainerId, int Count = 1);
