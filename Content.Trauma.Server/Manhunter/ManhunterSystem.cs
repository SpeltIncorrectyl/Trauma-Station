// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Mind;
using Content.Shared.Store.Components;
using Content.Trauma.Server.JobListings;
using Content.Trauma.Server.Store;
using Content.Trauma.Shared.JobListings;


/// <summary>
/// A server-side system that handles purchasing the manhunter kit.
/// There is no point putting this in shared because the event it subscribes to is entirely server-side.
/// </summary>
public sealed partial class ManhunterSystem : EntitySystem
{
    [Dependency] private ServerJobListingsSystem _jobs = default!;
    [Dependency] private TraumaStoreSystem _store = default!;

    [SubscribeLocalEvent]
    private void OnPurchased(Entity<MindComponent> mind, ref CreateJobBoardAndChangeStoreEvent args)
    {
        if (!TryComp<UplinkOwnerComponent>(mind, out var uplinkComp))
            return;

        _jobs.CreateJobBoard((mind.Owner, mind.Comp, uplinkComp), args.JobBoardProto);
        _store.ChangeStore((mind.Owner, mind.Comp, uplinkComp), args.StoreProto);
    }
}

/// <summary>
/// Event similar to <see cref="CreateJobBoardEvent"/> except it also changes the uplink owner's store.
/// </summary>
[DataDefinition]
public sealed partial class CreateJobBoardAndChangeStoreEvent : EntityEventArgs
{
    /// <summary>
    /// The prototype for the job board to create.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId<JobListingsComponent> JobBoardProto;

    /// <summary>
    /// The prototype for the store to replace the existing store with.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId<StoreComponent> StoreProto;
}