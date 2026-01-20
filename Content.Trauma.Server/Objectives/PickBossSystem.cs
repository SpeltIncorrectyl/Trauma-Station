using Content.Server.Objectives.Components;
using Content.Server.Objectives.Systems;
using Content.Shared.Objectives.Components;
using Content.Trauma.Server.Economy;

namespace Content.Trauma.Server.Objectives;

public sealed class PickBossSystem : EntitySystem
{
    [Dependency] private readonly TargetObjectiveSystem _target = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PickBossComponent, ObjectiveAssignedEvent>(OnBossPicked);
    }

    private void OnBossPicked(Entity<PickBossComponent> entity, ref ObjectiveAssignedEvent args)
    {
        if (!TryComp<RoleHireComponent>(entity.Owner, out var roleHire))
        {
            args.Cancelled = true;
            return;
        }

        _target.SetTarget(entity.Owner, roleHire.Boss);
    }
}
