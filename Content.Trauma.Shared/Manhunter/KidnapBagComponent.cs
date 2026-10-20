// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Manhunter;

/// <summary>
/// Component put on the manhunter's disappearing bodybag, look at <see cref="KidnapBagSystem"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class KidnapBagComponent : Component;