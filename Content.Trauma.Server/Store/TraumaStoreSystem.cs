// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Administration.Logs;
using Content.Server.Mind.Toolshed;
using Content.Server.Store.Systems;
using Content.Shared.Database;
using Content.Shared.Mind;
using Content.Shared.Store.Components;
using Content.Trauma.Common.Traitor;
using Content.Trauma.Common.Wizard;
using Content.Trauma.Shared.JobListings;

namespace Content.Trauma.Server.Store;

public sealed partial class TraumaStoreSystem : EntitySystem
{
    [Dependency] private StoreSystem _store = default!;
    [Dependency] private IAdminLogManager _admin = default!;

    public override void Initialize()
    {
        base.Initialize();


        SubscribeLocalEvent<StoreComponent, StoreRefundAllListingsMessage>(OnRefundAll);
        SubscribeLocalEvent<StoreComponent, StoreRefundListingMessage>(OnRefundListing);
    }

    /// <summary>
    /// Input a mind that owns an uplink. 
    /// This method finds any remote store associated with that uplink, and changes the store to a new store spawned based on a prototype id.
    /// </summary>
    /// <param name="mind"></param>
    /// <param name="storeProto"></param>
    public void ChangeStore(Entity<MindComponent, UplinkOwnerComponent> mind, EntProtoId<StoreComponent> storeProto)
    {
        var store = Spawn(storeProto);
        var user = mind.Comp1.OwnedEntity;

        foreach (var remote in mind.Comp2.Remotes)
        {
            if (!TryComp<RemoteStoreComponent>(remote, out var remoteComp))
                return;
            remoteComp.Store = store;
        }

        _store.UpdateUserInterface(user, store);
    }

    private void OnRefundListing(Entity<StoreComponent> ent, ref StoreRefundListingMessage args)
    {
        if (args.Actor is not { Valid: true } buyer)
            return;

        var (uid, component) = ent;

        var listing = GetEntity(args.ListingEntity);

        if (_store.RefundListing(uid, component, listing, buyer, true))
            _store.UpdateUserInterface(buyer, uid, component);

        _store.UpdateRefundUserInterface(uid, component);
    }

    private void OnRefundAll(Entity<StoreComponent> ent, ref StoreRefundAllListingsMessage args)
    {
        if (args.Actor is not { Valid: true } buyer)
            return;

        var (uid, component) = ent;

        if (!_store.IsOnStartingMap(uid, component) || !component.RefundAllowed || component.BoughtEntities.Count == 0)
        {
            _store.UpdateRefundUserInterface(uid, component);
            return;
        }

        _admin.Add(LogType.StoreRefund, LogImpact.Low, $"{buyer:player} has refunded their purchases from {uid:store}");

        for (var i = component.BoughtEntities.Count - 1; i >= 0; i--)
        {
            var purchase = component.BoughtEntities[i];

            _store.RefundListing(uid, component, purchase, buyer, false);
        }

        _store.UpdateUserInterface(buyer, uid, component);
        _store.UpdateRefundUserInterface(uid, component);
    }
}
