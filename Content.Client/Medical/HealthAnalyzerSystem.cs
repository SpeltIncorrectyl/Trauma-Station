using Content.Shared.Medical;
using Content.Shared.MedicalScanner;
using Robust.Client.GameObjects;

namespace Content.Client.Medical;

public sealed class HealthAnalyzerSystem : SharedHealthAnalyzerSystem
{
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        var query = EntityQueryEnumerator<HealthAnalyzerComponent>();
        while (query.MoveNext(out var entity, out _))
        {
            if (!_uiSystem.TryGetOpenUi(entity, HealthAnalyzerUiKey.Key, out var bui))
                continue;
            bui.Update();
        }
    }
}
