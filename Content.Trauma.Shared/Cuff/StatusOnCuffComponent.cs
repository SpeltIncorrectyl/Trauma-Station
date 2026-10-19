// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.StatusEffectNew.Components;

namespace Content.Trauma.Shared.Cuff;

/// <summary>
/// Apply a status effect to someone when they are restrained with cuffs with this component.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StatusOnCuffComponent : Component
{
    /// <summary>
    /// The status effect to apply to the cuffed person.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId<StatusEffectComponent> Proto;
}