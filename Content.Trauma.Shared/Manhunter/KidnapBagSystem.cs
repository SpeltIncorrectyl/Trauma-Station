// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Cuffs.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Stealth;
using Content.Shared.Storage.Components;
using Content.Trauma.Shared.Manhunter;

/// <summary>
/// Facilitates the functionality of the manhunter's disappearing bodybag.
/// Makes it only invisible when closed like the stealth box.
/// Also stops people inside from uncuffing while the bag is being dragged.
/// </summary>
public sealed partial class KidnapBagSystem : EntitySystem
{
    [Dependency] private SharedStealthSystem _stealth = default!;

    [SubscribeLocalEvent]
    private void OnClosed(Entity<KidnapBagComponent> ent, ref StorageAfterCloseEvent args)
    {
        _stealth.SetEnabled(ent, true);
    }

    [SubscribeLocalEvent]
    private void OnOpened(Entity<KidnapBagComponent> ent, ref StorageAfterOpenEvent args)
    {
        _stealth.SetEnabled(ent, false);
    }

    [SubscribeLocalEvent]
    private void OnAttemptUncuff(Entity<InsideEntityStorageComponent> ent, ref UncuffAttemptEvent args)
    {
        if (!HasComp<KidnapBagComponent>(ent.Comp.Storage))
            return;
        if (!TryComp<PullableComponent>(ent.Comp.Storage, out var pullableComp))
            return;
        if (pullableComp.BeingPulled)
            args.Cancelled = true;
    }
}