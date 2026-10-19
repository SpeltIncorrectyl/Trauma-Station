// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.StatusEffectNew;
using Content.Shared.Cuffs;

namespace Content.Trauma.Shared.Cuff;

/// <summary>
/// Facilitates the <see cref="StatusOnCuffComponent"/> applying status effects to cuffed targets.
/// </summary>
public sealed partial class StatusOnCuffSystem : EntitySystem
{
    [Dependency] private StatusEffectsSystem _status = default!;

    [SubscribeLocalEvent]
    private void OnCuffed(Entity<StatusOnCuffComponent> ent, ref HandcuffAppliedEvent args)
    {
        _status.AddEffect(args.Target, ent.Comp.Proto);
    }

    [SubscribeLocalEvent]
    private void OnUncuffed(Entity<StatusOnCuffComponent> ent, ref HandcuffRemovedEvent args)
    {
        _status.RemoveEffects(args.Target, [ent.Comp.Proto]); // no api for removing just 1 effect :(
    }
}