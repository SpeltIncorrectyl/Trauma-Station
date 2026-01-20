using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Economy;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RoleHireSpawnerComponent : Component
{
    /// <summary>
    /// The person who activated this item (toggled it on).
    /// This is who the hire should work for.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? User;

    /// <summary>
    /// The objectives the hire must follow.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<EntProtoId> Objectives = new();
}
