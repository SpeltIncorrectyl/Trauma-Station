using Content.Client.HealthAnalyzer.UI;
using Content.Shared.Medical;
using Content.Shared.MedicalScanner;
using Robust.Client.GameObjects;

namespace Content.Client.Medical;

public sealed class HealthAnalyzerSystem : SharedHealthAnalyzerSystem
{

    protected override void UpdateUi(Entity<HealthAnalyzerComponent> entity)
    {
        if (!_uiSystem.TryGetOpenUi(entity.Owner, HealthAnalyzerUiKey.Key, out var bui) || entity.Comp.ScannedEntity == null || !_timing.IsFirstTimePredicted)
            return;
        var patientCoordinates = Transform(entity.Comp.ScannedEntity.Value).Coordinates;
        var scannerCoordinates = Transform(entity.Owner).Coordinates;
        var inRange = entity.Comp.MaxScanRange == null || _transformSystem.InRange(patientCoordinates, scannerCoordinates, entity.Comp.MaxScanRange.Value);
        ((HealthAnalyzerBoundUserInterface)bui).UpdateScanner(inRange);
    }

    protected override void Retarget(Entity<HealthAnalyzerComponent> entity, EntityUid target)
    {
        if (!_uiSystem.TryGetOpenUi(entity.Owner, HealthAnalyzerUiKey.Key, out var bui) || entity.Comp.ScannedEntity == null)
            return;
        ((HealthAnalyzerBoundUserInterface)bui).SetTarget(target);
    }
}
