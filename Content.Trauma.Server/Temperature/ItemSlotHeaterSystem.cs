// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Examine;
using Content.Trauma.Shared.Temperature;

namespace Content.Trauma.Server.Temperature;

public sealed class ItemSlotHeaterSystem : SharedItemSlotHeaterSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ItemSlotHeaterComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(Entity<ItemSlotHeaterComponent> ent, ref ExaminedEvent args)
    {
        if (_itemSlots.GetItemOrNull(ent.Owner, ent.Comp.Slot) is not { } item || !_temperatureQuery.TryComp(item, out var temp))
            return;

        args.PushMarkup(Loc.GetString("item-slot-heater-temp", ("temp", temp.CurrentTemperature.ToString("F1"))));
    }
}
