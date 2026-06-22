// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;
using Content.Shared.Temperature.Components;
using Content.Shared.Temperature.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Temperature;

public abstract partial class SharedItemSlotHeaterSystem : EntitySystem
{
    [Dependency] protected IGameTiming _timing = default!;
    [Dependency] protected ItemSlotsSystem _itemSlots = default!;
    [Dependency] protected SharedTemperatureSystem _temp = default!;
    [Dependency] protected EntityQuery<TemperatureComponent> _temperatureQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ItemSlotHeaterComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<ItemSlotHeaterComponent, EntRemovedFromContainerMessage>(OnRemoved);
    }

    public override void Update(float frameTime)
    {
        var now = _timing.CurTime;

        var eqe = EntityQueryEnumerator<ActiveItemSlotHeaterComponent, ItemSlotHeaterComponent>();
        while (eqe.MoveNext(out var uid, out var active, out var heat))
        {
            if (now < active.NextUpdate)
                continue;

            if (_itemSlots.GetItemOrNull(uid, heat.Slot) is not {} item)
                continue;

            if (!_temperatureQuery.TryComp(item, out var temp))
                continue;

            // In order to support cooling, we must check if we are heating the entity or not.
            // If we are heating, then check if we are higher than the Max Temperature set,
            // otherwise check if we are below the Max Temperature (if we are cooling).
            if ( ( heat.Temp >= 0 && temp.CurrentTemperature >= heat.MaxTemp )
                || ( heat.Temp < 0 && temp.CurrentTemperature <= heat.MaxTemp ) )
                continue;

            _temp.ChangeHeat(item, heat.Temp);

            active.NextUpdate = now + heat.Update;
            Dirty(uid, active);
        }
    }

    private void OnInserted(Entity<ItemSlotHeaterComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        // Required to not cause mispredicts
        if (_timing.ApplyingState)
            return;

        // Only apply the component if we inserted it into the correct item slot
        if (!_itemSlots.TryGetSlot(ent.Owner, ent.Comp.Slot, out var itemSlot))
                return;

        if (itemSlot.Item is not { } item || args.Entity != item)
                return;

        EnsureComp<ActiveItemSlotHeaterComponent>(ent);
    }

    private void OnRemoved(Entity<ItemSlotHeaterComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        // Required to not cause mispredicts
        if (_timing.ApplyingState)
            return;

        // The item slot must be empty in order to stop the heating proccess
        if (_itemSlots.GetItemOrNull(ent.Owner, ent.Comp.Slot) != null)
            return;

        RemCompDeferred<ActiveItemSlotHeaterComponent>(ent);
    }

}
