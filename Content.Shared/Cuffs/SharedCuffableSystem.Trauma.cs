// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared.Cuffs;

/// <summary>
/// Raised on a handcuff once it is successfully used to restrain someone.
/// </summary>
[ByRefEvent]
public sealed partial record HandcuffAppliedEvent(EntityUid Target);

/// <summary>
/// Raised on a handcuff once the person restrained is freed.
/// </summary>
[ByRefEvent]
public sealed partial record HandcuffRemovedEvent(EntityUid Target);