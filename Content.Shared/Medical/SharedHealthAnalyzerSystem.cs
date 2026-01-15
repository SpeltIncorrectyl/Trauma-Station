using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage.Components;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.MedicalScanner;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.PowerCell;
using Content.Shared.Temperature.Components;
using Content.Shared.Traits.Assorted;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

// Shitmed Change
using Content.Shared._Shitmed.Medical.HealthAnalyzer;
using Content.Shared._Shitmed.Medical.Surgery.Wounds;
using Content.Shared._Shitmed.Medical.Surgery.Wounds.Components;
using Content.Shared._Shitmed.Medical.Surgery.Wounds.Systems;
using Content.Shared._Shitmed.Medical.Surgery.Pain.Components;
using Content.Shared._Shitmed.Medical.Surgery.Traumas;
using Content.Shared._Shitmed.Medical.Surgery.Traumas.Components;
using Content.Shared._Shitmed.Medical.Surgery.Traumas.Systems;
using Content.Shared._Shitmed.Targeting;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Goobstation.Maths.FixedPoint;
using System.Linq;
using Content.Shared.Mobs.Systems; // Goobstation

namespace Content.Shared.Medical;

public abstract class SharedHealthAnalyzerSystem : EntitySystem
{
    // <Trauma>
    [Dependency] protected readonly MobThresholdSystem _threshold = default!;
    [Dependency] protected readonly SharedBodySystem _body = default!;
    [Dependency] protected readonly TraumaSystem _trauma = default!;
    [Dependency] protected readonly WoundSystem _wound = default!;
    // </Trauma>
    [Dependency] protected readonly IGameTiming _timing = default!;
    [Dependency] protected readonly PowerCellSystem _cell = default!;
    [Dependency] protected readonly SharedAudioSystem _audio = default!;
    [Dependency] protected readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] protected readonly ItemToggleSystem _toggle = default!;
    [Dependency] protected readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] protected readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] protected readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] protected readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] protected readonly SharedBloodstreamSystem _bloodstreamSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<HealthAnalyzerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<HealthAnalyzerComponent, HealthAnalyzerDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<HealthAnalyzerComponent, EntGotInsertedIntoContainerMessage>(OnInsertedIntoContainer);
        SubscribeLocalEvent<HealthAnalyzerComponent, ItemToggledEvent>(OnToggled);
        SubscribeLocalEvent<HealthAnalyzerComponent, DroppedEvent>(OnDropped);
    }

    public override void Update(float frameTime)
    {
        var analyzerQuery = EntityQueryEnumerator<HealthAnalyzerComponent>();
        while (analyzerQuery.MoveNext(out var uid, out var component))
        {
            //Update rate limited to 0.25 seconds (it is all done client side so no need to worry about network spam
            if (component.NextUpdate > _timing.CurTime)
                continue;

            if (component.ScannedEntity is not { } patient)
                continue;

            if (Deleted(patient))
            {
                StopAnalyzingEntity((uid, component), patient);
                continue;
            }

            component.NextUpdate = _timing.CurTime + component.UpdateInterval;
            Dirty(uid, component);
            UpdateUi(uid);
        }
    }

    protected virtual void UpdateUi(EntityUid entity) {}

    /// <summary>
    /// Trigger the doafter for scanning
    /// </summary>
    private void OnAfterInteract(Entity<HealthAnalyzerComponent> uid, ref AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach || !HasComp<MobStateComponent>(args.Target) || !_cell.HasDrawCharge(uid.Owner, user: args.User))
            return;

        _audio.PlayPredicted(uid.Comp.ScanningBeginSound, uid.Owner, args.User);

        var doAfterCancelled = !_doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, uid.Comp.ScanDelay, new HealthAnalyzerDoAfterEvent(), uid, target: args.Target, used: uid)
        {
            NeedHand = true,
            BreakOnMove = true,
        });

        if (args.Target == args.User || doAfterCancelled || uid.Comp.Silent)
            return;

        var msg = Loc.GetString("health-analyzer-popup-scan-target", ("user", Identity.Entity(args.User, EntityManager)));
        _popupSystem.PopupEntity(msg, args.Target.Value, args.Target.Value, PopupType.Medium);
    }

    private void OnDoAfter(Entity<HealthAnalyzerComponent> uid, ref HealthAnalyzerDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target == null || !_cell.HasDrawCharge(uid.Owner, user: args.User))
            return;

        if (!uid.Comp.Silent)
            _audio.PlayPredicted(uid.Comp.ScanningEndSound, uid.Owner, args.User);

        OpenUserInterface(args.User, uid);
        BeginAnalyzingEntity(uid, args.Target.Value);
        args.Handled = true;
    }

    /// <summary>
    /// Turn off when placed into a storage item or moved between slots/hands
    /// </summary>
    private void OnInsertedIntoContainer(Entity<HealthAnalyzerComponent> uid, ref EntGotInsertedIntoContainerMessage args)
    {
        if (uid.Comp.ScannedEntity is { } patient)
            _toggle.TryDeactivate(uid.Owner);
    }

    /// <summary>
    /// Disable continuous updates once turned off
    /// </summary>
    private void OnToggled(Entity<HealthAnalyzerComponent> ent, ref ItemToggledEvent args)
    {
        if (!args.Activated && ent.Comp.ScannedEntity is { } patient)
        {
            StopAnalyzingEntity(ent, patient);
            CloseUserInterface(ent);
        }
    }

    /// <summary>
    /// Turn off the analyser when dropped
    /// </summary>
    private void OnDropped(Entity<HealthAnalyzerComponent> uid, ref DroppedEvent args)
    {
        _toggle.TryDeactivate(uid.Owner);
    }

    private void OpenUserInterface(EntityUid user, EntityUid analyzer)
    {
        if (!_uiSystem.HasUi(analyzer, HealthAnalyzerUiKey.Key))
            return;

        _uiSystem.OpenUi(analyzer, HealthAnalyzerUiKey.Key, user);
    }

    private void CloseUserInterface(EntityUid analyzer)
    {
        _uiSystem.CloseUi(analyzer, HealthAnalyzerUiKey.Key);
    }

    /// <summary>
    /// Mark the entity as having its health analyzed, and link the analyzer to it
    /// </summary>
    /// <param name="healthAnalyzer">The health analyzer that should receive the updates</param>
    /// <param name="target">The entity to start analyzing</param>
    /// <param name="part">Shitmed Change: The body part to analyze, if any</param>
    public void BeginAnalyzingEntity(Entity<HealthAnalyzerComponent> healthAnalyzer, EntityUid? target, EntityUid? part = null)
    {
        //Link the health analyzer to the scanned entity
        healthAnalyzer.Comp.ScannedEntity = target;
        healthAnalyzer.Comp.CurrentBodyPart = part; // Shitmed Change
        Dirty(healthAnalyzer);

        if (target is null)
            return;

        _toggle.TryActivate(healthAnalyzer.Owner);
    }

    /// <summary>
    /// Remove the analyzer from the active list, and remove the component if it has no active analyzers
    /// </summary>
    /// <param name="healthAnalyzer">The health analyzer that's receiving the updates</param>
    /// <param name="target">The entity to analyze</param>
    private void StopAnalyzingEntity(Entity<HealthAnalyzerComponent> healthAnalyzer, EntityUid target)
    {
        //Unlink the analyzer
        healthAnalyzer.Comp.ScannedEntity = null;
        healthAnalyzer.Comp.CurrentBodyPart = null; // Shitmed Change
        Dirty(healthAnalyzer);
        _toggle.TryDeactivate(healthAnalyzer.Owner);
    }

    /// <summary>
    /// Send an update for the target to the healthAnalyzer
    /// </summary>
    /// <param name="healthAnalyzer">The health analyzer</param>
    /// <param name="target">The entity being scanned</param>
    /// <param name="scanMode">True makes the UI show ACTIVE, False makes the UI show INACTIVE</param>
    /// <param name="part">Shitmed Change: The body part being scanned, if any</param>
    public HealthAnalyzerBaseState? GetState(EntityUid healthAnalyzer, EntityUid target, bool scanMode, HealthAnalyzerMode mode, EntityUid? part = null)
    {
        if (!_uiSystem.HasUi(healthAnalyzer, HealthAnalyzerUiKey.Key)
            || !TryComp<BodyComponent>(target, out var body))
            return null;

        var bodyTemperature = float.NaN;

        if (TryComp<TemperatureComponent>(target, out var temp))
            bodyTemperature = temp.CurrentTemperature;

        var bloodAmount = float.NaN;

        if (TryComp<BloodstreamComponent>(target, out var bloodstream) &&
            _solutionContainerSystem.ResolveSolution(target, bloodstream.BloodSolutionName,
                ref bloodstream.BloodSolution, out var bloodSolution))
            bloodAmount = _bloodstreamSystem.GetBloodLevel(target);

        var bodyStatus = _wound.GetDamageableStatesOnBody(target);
        Dictionary<TargetBodyPart, bool> bleeding; // Goobstation - removed unnecessary allocation

        // Goobstation start
        var vitalDamage = FixedPoint2.Zero;
        if (TryComp<DamageableComponent>(target, out var damageableComponent))
            vitalDamage = _threshold.CheckVitalDamage(target, damageableComponent);
        // Goobstation end

        switch (mode)
        {
            case HealthAnalyzerMode.Body:
                var unrevivable = false;
                FetchBodyData(target, body, out var traumas, out var pain, out bleeding);
                if (TryComp<UnrevivableComponent>(target, out var unrevivableComp) && unrevivableComp.Analyzable)
                    unrevivable = true;

                return new HealthAnalyzerBodyState(
                    GetNetEntity(target),
                    bodyTemperature,
                    bloodAmount,
                    scanMode,
                    unrevivable,
                    bodyStatus,
                    bleeding,
                    vitalDamage, // Goobstation
                    traumas,
                    pain,
                    part != null ? GetNetEntity(part) : null
                );

            case HealthAnalyzerMode.Organs:
                bleeding = FetchBleedData(body);
                var organs = FetchOrganData(target);
                return new HealthAnalyzerOrgansState(
                    GetNetEntity(target),
                    bodyTemperature,
                    bloodAmount,
                    scanMode,
                    bleeding,
                    vitalDamage, // Goobstation
                    bodyStatus,
                    organs
                );

            case HealthAnalyzerMode.Chemicals:
                bleeding = FetchBleedData(body);
                var chemicals = FetchChemicalData(target);
                return new HealthAnalyzerChemicalsState(
                    GetNetEntity(target),
                    bodyTemperature,
                    bloodAmount,
                    scanMode,
                    bleeding,
                    vitalDamage, // Goobstation
                    bodyStatus,
                    chemicals
                );
        }

        return null;
    }

    private void FetchBodyData(EntityUid target,
        BodyComponent body,
        out Dictionary<NetEntity, List<WoundableTraumaData>> traumas,
        out Dictionary<NetEntity, FixedPoint2> pain,
        out Dictionary<TargetBodyPart, bool> bleeding)
    {
        traumas = new();
        pain = new();
        bleeding = new();

        if (body.RootContainer.ContainedEntity is not { } rootPart)
            return;

        foreach (var (woundable, component) in _wound.GetAllWoundableChildren(rootPart))
        {
            traumas.Add(GetNetEntity(woundable), FetchTraumaData(woundable, component));
            pain.Add(GetNetEntity(woundable), FetchPainData(woundable, component));
            bleeding.Add(_body.GetTargetBodyPart(woundable), component.Bleeds > 0);
        }
    }

    private Dictionary<TargetBodyPart, bool> FetchBleedData(BodyComponent body)
    {
        var bleeding = new Dictionary<TargetBodyPart, bool>();

        if (body.RootContainer.ContainedEntity is not { } rootPart)
            return bleeding;

        foreach (var (woundable, component) in _wound.GetAllWoundableChildren(rootPart))
            bleeding.Add(_body.GetTargetBodyPart(woundable), component.Bleeds > 0);

        return bleeding;
    }

    private List<WoundableTraumaData> FetchTraumaData(EntityUid target,
        WoundableComponent woundable)
    {
        var traumasList = new List<WoundableTraumaData>();

        if (_trauma.TryGetWoundableTrauma(target, out var traumasFound))
        {
            foreach (var trauma in traumasFound)
            {
                if (trauma.Comp.TraumaType == TraumaType.BoneDamage
                    && trauma.Comp.TraumaTarget is { } boneWoundable
                    && TryComp(boneWoundable, out BoneComponent? boneComp))
                {
                    traumasList.Add(new WoundableTraumaData(ToPrettyString(target),
                        trauma.Comp.TraumaType.ToString(), trauma.Comp.TraumaSeverity, boneComp.BoneSeverity.ToString(), trauma.Comp.TargetType));

                    continue;
                }

                traumasList.Add(new WoundableTraumaData(ToPrettyString(trauma),
                        trauma.Comp.TraumaType.ToString(), trauma.Comp.TraumaSeverity, targetType: trauma.Comp.TargetType));
            }
        }

        return traumasList;
    }

    private FixedPoint2 FetchPainData(EntityUid target,
        WoundableComponent woundable)
    {
        var pain = FixedPoint2.Zero;

        if (!TryComp<NerveComponent>(target, out var nerve))
            return pain;

        return nerve.PainFeels;
    }

    private Dictionary<NetEntity, OrganTraumaData> FetchOrganData(EntityUid target)
    {
        var organs = new Dictionary<NetEntity, OrganTraumaData>();
        if (!TryComp<BodyComponent>(target, out var body))
            return organs;

        foreach (var (organId, organComp) in _body.GetBodyOrgans(target))
        {
            organs.Add(GetNetEntity(organId), new OrganTraumaData(organComp.OrganIntegrity,
                organComp.IntegrityCap,
                organComp.OrganSeverity,
                organComp.IntegrityModifiers
                    .Select(x => (x.Key.Item1, x.Value))
                    .ToList()));
        }

        return organs;
    }

    private Dictionary<NetEntity, Solution> FetchChemicalData(EntityUid target)
    {
        var solutionsList = new Dictionary<NetEntity, Solution>();

        if (!TryComp(target, out SolutionContainerManagerComponent? container) || container.Containers.Count == 0)
            return solutionsList;

        foreach (var (name, solution) in _solutionContainerSystem.EnumerateSolutions((target, container)))
        {
            if (name is null
                || name == BloodstreamComponent.DefaultBloodTemporarySolutionName
                || name == "print" // I hate this so fucking much.
                || !TryGetNetEntity(solution, out var netSolution))
                continue;

            solutionsList.Add(netSolution.Value, solution.Comp.Solution);
        }

        if (TryComp<BodyComponent>(target, out var body)
            && _body.TryGetBodyOrganEntityComps<StomachComponent>((target, body), out var stomachs))
        {
            foreach (var stomach in stomachs)
            {
                if (stomach.Comp1.Solution is null
                    || !TryGetNetEntity(stomach.Comp1.Solution, out var netSolution))
                    continue;

                solutionsList.Add(netSolution.Value, stomach.Comp1.Solution.Value.Comp.Solution); // This is horrible.
            }
        }

        return solutionsList;
    }
}
