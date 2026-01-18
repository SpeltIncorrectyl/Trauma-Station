using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Trauma.Shared.Store;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AutomatedVendorComponent : Component
{
    /// <summary>
    /// Vendor slots this vendor has: <see cref="VendorSlot"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<VendorSlot> Slots = new();
}

/// <summary>
/// A slot for crew to insert goods into the vendor. The goods are then to be sold.
/// </summary>
[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class VendorSlot
{
    /// <summary>
    /// The container this slot represents.
    /// </summary>
    [DataField]
    public string ContainerId = String.Empty;

    /// <summary>
    /// The whitelist for inserting things into this slot.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    public VendorSlot() {}

    public VendorSlot(VendorSlot other)
    {
        CopyFrom(other);
    }

    public void CopyFrom(VendorSlot other)
    {
        ContainerId = other.ContainerId;
        Whitelist = other.Whitelist;
    }
}
