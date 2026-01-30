using System.Linq;
using Content.Shared._Shitmed.Medical.Surgery.Conditions;
using Content.Shared._Shitmed.Medical.Surgery.Steps.Parts;
using Content.Shared._Shitmed.Medical.Surgery.Traumas.Systems;
using Content.Shared._Shitmed.Medical.Surgery.Wounds.Components;
using Content.Shared._Shitmed.Medical.Surgery.Wounds.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;

namespace Content.Trauma.Shared.Surgery;

public sealed class SurgeryFeasibleSystem : EntitySystem
{
    [Dependency] private readonly WoundSystem _wounds = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly TraumaSystem _trauma = default!;

    private EntityQuery<BodyComponent> _bodyQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SurgeryCloseIncisionConditionComponent, SurgeryFeasibleEvent>(OnCloseIncisionValid);
        SubscribeLocalEvent<SurgeryHasBodyConditionComponent, SurgeryFeasibleEvent>(OnHasBodyConditionValid);
        SubscribeLocalEvent<SurgeryPartConditionComponent, SurgeryFeasibleEvent>(OnPartConditionValid);
        SubscribeLocalEvent<SurgeryOrganConditionComponent, SurgeryFeasibleEvent>(OnOrganConditionValid);
        SubscribeLocalEvent<SurgeryWoundedConditionComponent, SurgeryFeasibleEvent>(OnWoundedValid);
        SubscribeLocalEvent<SurgeryPartRemovedConditionComponent, SurgeryFeasibleEvent>(OnPartRemovedConditionValid);
        SubscribeLocalEvent<SurgeryBodyConditionComponent, SurgeryFeasibleEvent>(OnBodyConditionValid);
        SubscribeLocalEvent<SurgeryOrganSlotConditionComponent, SurgeryFeasibleEvent>(OnOrganSlotConditionValid);
        SubscribeLocalEvent<SurgeryPartPresentConditionComponent, SurgeryFeasibleEvent>(OnPartPresentConditionValid);
        SubscribeLocalEvent<SurgeryTraumaPresentConditionComponent, SurgeryFeasibleEvent>(OnTraumaPresentConditionValid);
        SubscribeLocalEvent<SurgeryBleedsPresentConditionComponent, SurgeryFeasibleEvent>(OnBleedsPresentConditionValid);
        SubscribeLocalEvent<SurgeryMarkingConditionComponent, SurgeryFeasibleEvent>(OnMarkingPresentValid);
        SubscribeLocalEvent<SurgeryBodyComponentConditionComponent, SurgeryFeasibleEvent>(OnBodyComponentConditionValid);
        SubscribeLocalEvent<SurgeryPartComponentConditionComponent, SurgeryFeasibleEvent>(OnPartComponentConditionValid);
        SubscribeLocalEvent<SurgeryOrganOnAddConditionComponent, SurgeryFeasibleEvent>(OnOrganOnAddConditionValid);
    }

    private void OnCloseIncisionValid(Entity<SurgeryCloseIncisionConditionComponent> ent, ref SurgeryFeasibleEvent args)
    {
        if (!HasComp<IncisionOpenComponent>(args.Part) ||
            !HasComp<BleedersClampedComponent>(args.Part) ||
            !HasComp<SkinRetractedComponent>(args.Part) ||
            !HasComp<BodyPartReattachedComponent>(args.Part) ||
            !HasComp<InternalBleedersClampedComponent>(args.Part))
        {
            args.Cancel(ent.Comp.InfeasibleMessage);
        }
    }

    private void OnWoundedValid(Entity<SurgeryWoundedConditionComponent> ent, ref SurgeryFeasibleEvent args)
    {
        if (!TryComp(args.Part, out WoundableComponent? partWoundable)
            || _wounds.GetWoundableSeverityPoint(
                args.Part,
                partWoundable,
                ent.Comp.DamageGroup,
                healable: true) <= 0)
            args.Cancel(ent.Comp.InfeasibleMessage);
    }

    private void OnBodyComponentConditionValid(Entity<SurgeryBodyComponentConditionComponent> ent, ref SurgeryFeasibleEvent args)
    {
        var present = true;
        foreach (var reg in ent.Comp.Components.Values)
        {
            var compType = reg.Component.GetType();
            if (!HasComp(args.Body, compType))
                present = false;
        }

        if (!ent.Comp.Inverse && !present)
            args.Hide();
        if (ent.Comp.Inverse && present)
            args.Hide();
    }

    private void OnPartComponentConditionValid(Entity<SurgeryPartComponentConditionComponent> ent, ref SurgeryFeasibleEvent args)
    {
        var present = true;
        foreach (var reg in ent.Comp.Components.Values)
        {
            var compType = reg.Component.GetType();
            if (!HasComp(args.Part, compType))
                present = false;
        }

        if (!ent.Comp.Inverse && !present)
            args.Hide();
        if (ent.Comp.Inverse && present)
            args.Hide();
    }

    // This is literally a duplicate of the checks in OnToolCheck for SurgeryStepComponent.AddOrganOnAdd
    private void OnOrganOnAddConditionValid(Entity<SurgeryOrganOnAddConditionComponent> ent, ref SurgeryFeasibleEvent args)
    {
        if (!TryComp<BodyPartComponent>(args.Part, out var part)
            || part.Body != args.Body)
        {
            args.Hide();
            return;
        }

        var organSlotIdToOrgan = _body.GetPartOrgans(args.Part, part).ToDictionary(o => o.Component.SlotId, o => o.Component);

        var allOnAddFound = true;
        var zeroOnAddFound = true;

        foreach (var (organSlotId, components) in ent.Comp.Components)
        {
            if (!organSlotIdToOrgan.TryGetValue(organSlotId, out var organ))
                continue;

            foreach (var key in components.Keys)
            {
                if (!organ.AddedKeys.Contains(key))
                    allOnAddFound = false;
                else
                    zeroOnAddFound = false;
            }
        }

        if (!ent.Comp.Inverse && zeroOnAddFound)
            args.Hide();
        if (ent.Comp.Inverse && allOnAddFound)
            args.Hide();
    }

    private void OnHasBodyConditionValid(Entity<SurgeryHasBodyConditionComponent> ent, ref SurgeryValidEvent args)
    {
        if (CompOrNull<BodyPartComponent>(args.Part)?.Body == null)
            args.Cancelled = true;
    }

    private void OnPartConditionValid(Entity<SurgeryPartConditionComponent> ent, ref SurgeryValidEvent args)
    {
        if (!TryComp<BodyPartComponent>(args.Part, out var part))
        {
            args.Cancelled = true;
            return;
        }

        var typeMatch = ent.Comp.Parts.Contains(part.PartType);
        var symmetryMatch = ent.Comp.Symmetry == null || part.Symmetry == ent.Comp.Symmetry;
        var valid = typeMatch && symmetryMatch;

        if (ent.Comp.Inverse ? valid : !valid)
            args.Cancelled = true;
    }

    private void OnOrganConditionValid(Entity<SurgeryOrganConditionComponent> ent, ref SurgeryValidEvent args)
    {
        if (!TryComp<BodyPartComponent>(args.Part, out var partComp)
            || partComp.Body != args.Body
            || ent.Comp.Organ == null)
        {
            args.Cancelled = true;
            return;
        }

        foreach (var reg in ent.Comp.Organ.Values)
        {
            if (_body.TryGetBodyPartOrgans(args.Part, reg.Component.GetType(), out var organs)
                && organs.Count > 0)
            {
                if (ent.Comp.Inverse
                    && (!ent.Comp.Reattaching
                    || ent.Comp.Reattaching
                    && !organs.Any(organ => HasComp<OrganReattachedComponent>(organ.Id))))
                    args.Cancelled = true;
            }
            else if (!ent.Comp.Inverse)
                args.Cancelled = true;
        }
    }

    private void OnBodyConditionValid(Entity<SurgeryBodyConditionComponent> ent, ref SurgeryValidEvent args)
    {
        if (_bodyQuery.CompOrNull(args.Body)?.Prototype is { } bodyId)
            args.Cancelled |= ent.Comp.Accepted.Contains(bodyId) == ent.Comp.Inverse;
    }

    private void OnOrganSlotConditionValid(Entity<SurgeryOrganSlotConditionComponent> ent, ref SurgeryValidEvent args)
    {
        args.Cancelled |= _body.CanInsertOrgan(args.Part, ent.Comp.OrganSlot) ^ !ent.Comp.Inverse;
    }

    private void OnPartRemovedConditionValid(Entity<SurgeryPartRemovedConditionComponent> ent, ref SurgeryValidEvent args)
    {
        if (!_body.CanAttachToSlot(args.Part, ent.Comp.Connection))
        {
            args.Cancelled = true;
            return;
        }

        var results = _body.GetBodyChildrenOfType(args.Body, ent.Comp.Part, symmetry: ent.Comp.Symmetry).ToList();
        if (results is not { } || !results.Any())
            return;

        if (!results.Any(part => HasComp<BodyPartReattachedComponent>(part.Id)))
            args.Cancelled = true;
    }

    private void OnPartPresentConditionValid(Entity<SurgeryPartPresentConditionComponent> ent, ref SurgeryValidEvent args)
    {
        if (args.Part == EntityUid.Invalid
            || !HasComp<BodyPartComponent>(args.Part))
            args.Cancelled = true;
    }

    private void OnTraumaPresentConditionValid(Entity<SurgeryTraumaPresentConditionComponent> ent, ref SurgeryValidEvent args)
    {
        if (args.Cancelled)
            return;

        // not inverted = cancel if no trauma present
        // inverted = cancel if trauma present
        if (_trauma.HasWoundableTrauma(args.Part, ent.Comp.TraumaType) == ent.Comp.Inverted)
            args.Cancelled = true;
    }

    private void OnBleedsPresentConditionValid(Entity<SurgeryBleedsPresentConditionComponent> ent, ref SurgeryValidEvent args)
    {
        if (!TryComp<WoundableComponent>(args.Part, out var woundable))
        {
            args.Cancelled = true;
            return;
        }

        if (ent.Comp.Inverted == woundable.Bleeds > 0
            && !HasComp<BleedersClampedComponent>(args.Part))
            args.Cancelled = true;
    }

    private void OnMarkingPresentValid(Entity<SurgeryMarkingConditionComponent> ent, ref SurgeryValidEvent args)
    {
        var markingCategory = MarkingCategoriesConversion.FromHumanoidVisualLayers(ent.Comp.MarkingCategory);

        var hasMarking = TryComp(args.Body, out HumanoidAppearanceComponent? bodyAppearance)
            && bodyAppearance.MarkingSet.Markings.TryGetValue(markingCategory, out var markingList)
            && markingList.Any(marking => marking.MarkingId.Contains(ent.Comp.MatchString));

        if ((!ent.Comp.Inverse && hasMarking) || (ent.Comp.Inverse && !hasMarking))
            args.Cancelled = true;
    }
}

[ByRefEvent]
public struct SurgeryFeasibleEvent(EntityUid body, EntityUid part, BodyPartType partType = default, BodyPartSymmetry? symmetry = default)
{
    public EntityUid Body = body;
    public EntityUid Part = part;
    public BodyPartType PartType = partType;
    public BodyPartSymmetry? Symmetry = symmetry;

    private bool _infeasible = false;
    private bool _hidden = false;
    private List<string> _reasons = new();

    public bool Infeasible => _infeasible;
    public bool Hidden => _hidden;
    public IReadOnlyList<string> Reasons => _reasons;

    public void Cancel(string reason)
    {
        _infeasible = true;
        _reasons.Add(reason);
    }

    public void Hide()
    {
        _infeasible = true;
        _hidden = true;
    }
}
