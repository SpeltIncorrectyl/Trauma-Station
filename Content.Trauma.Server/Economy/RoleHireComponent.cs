namespace Content.Trauma.Server.Economy;

/// <summary>
/// This person is a role that got hired by someone, like a lawyer.
/// </summary>
[RegisterComponent]
public sealed partial class RoleHireComponent : Component
{
    /// <summary>
    /// The person this hire works for.
    /// </summary>
    [DataField]
    public EntityUid Boss;
}
