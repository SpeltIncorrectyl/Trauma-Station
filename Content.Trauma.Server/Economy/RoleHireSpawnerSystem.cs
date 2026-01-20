using Content.Server.Ghost.Roles.Events;
using Content.Server.Mind;
using Content.Shared.Mind;
using Content.Shared.Store;
using Content.Trauma.Shared.Economy;

namespace Content.Trauma.Server.Economy;

public sealed class RoleHireSpawnerSystem : SharedRoleHireSpawnerSystem
{
    [Dependency] private readonly MindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GhostRoleSpawnerUsedEvent>(OnGhostRoleSpawn);
        SubscribeLocalEvent<RoleHireSpawnerComponent, RoleHireSpawnerUsedEvent>(OnHireSpawnerUsed);
    }

    private void OnGhostRoleSpawn(GhostRoleSpawnerUsedEvent args)
    {
        var ev = new RoleHireSpawnerUsedEvent(args.Spawned);
        RaiseLocalEvent(args.Spawner, ref ev);
    }

    private void OnHireSpawnerUsed(Entity<RoleHireSpawnerComponent> entity, ref RoleHireSpawnerUsedEvent args)
    {
        if (entity.Comp.User is null)
            return;

        AddComp(entity.Owner, new RoleHireComponent {Boss = entity.Comp.User.Value});

        if (!_mind.TryGetMind(args.Result, out var mindId, out var mindComp))
            return;

        foreach (var objective in entity.Comp.Objectives)
        {
            _mind.TryAddObjective(mindId, mindComp, objective);
        }
    }
}

[ByRefEvent]
public record struct RoleHireSpawnerUsedEvent(EntityUid Result);
