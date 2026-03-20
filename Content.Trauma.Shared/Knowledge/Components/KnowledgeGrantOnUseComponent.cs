// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Knowledge.Components;

/// <summary>
/// Grants some knowledge when used in hand.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class KnowledgeGrantOnUseComponent : Component
{

    /// <summary>
    /// Experience that will be added per use.
    /// </summary>
    [DataField, AlwaysPushInheritance]
    public Dictionary<EntProtoId, int> ExperiencePerUse = new();

    /// <summary>
    /// A cap on each skill which limits how much it can be leveled up to by adding experience from this entity.
    /// </summary>
    [DataField, AlwaysPushInheritance]
    public Dictionary<EntProtoId, int> SkillCaps = new();

    /// <summary>
    /// Knowledge levels which are instantly set when entity is used.
    /// This is different to experience which slowly accumulates and can then level up skills.
    /// After using this entity the skills will be set directly to the values (if they are not already higher).
    /// Most likely to be used this <see cref="SingleUse"/> set to true for the syndicate martial arts scrolls.
    /// </summary>
    [DataField, AlwaysPushInheritance]
    public Dictionary<EntProtoId, int> InstantKnowledge = new();

    /// <summary>
    /// Length of a single doafter to learn this knowledge.
    /// </summary>
    [DataField]
    public TimeSpan DoAfter = TimeSpan.FromSeconds(5);

    /// <summary>
    /// If true, you will instantly gain all the skills then the item is destroyed.
    /// Only <see cref="Skills"/> is used, <see cref="Experience"/> is ignored.
    /// </summary>
    [DataField]
    public bool SingleUse = false;

    /// <summary>
    /// Something to spawn after this entity disintegrates after use.
    /// Only does something is <see cref="SingleUse"/> is set to true.
    /// </summary>
    [DataField]
    public EntProtoId? SpawnOnDisintegration;
}
