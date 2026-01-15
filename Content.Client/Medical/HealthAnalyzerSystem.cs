using Content.Shared.Medical;
using Content.Shared.MedicalScanner;
using Robust.Client.GameObjects;

namespace Content.Client.Medical;

public sealed class HealthAnalyzerSystem : SharedHealthAnalyzerSystem
{
    protected override void UpdateUi(EntityUid entity)
    {
        if (!_uiSystem.TryGetOpenUi(entity, HealthAnalyzerUiKey.Key, out var bui))
            return;
        bui.Update();
    }
}
