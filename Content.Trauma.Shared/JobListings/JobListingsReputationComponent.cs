// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityTable;
using Robust.Shared.Containers;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.JobListings;

/// <summary>
/// Component added to a store entity to enable side-jobs.
/// Used for progressive traitor.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class JobListingsReputationComponent : Component
{
    /// <summary>
    /// The reputation of the traitor who owns the job board.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Reputation;

    /// <summary>
    /// Reputation required to reach each level.
    /// Level 0 by default, first element in list is rep required for level 1, second element is rep required for level 2, and so on.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<int> ReputationLevels;
}
