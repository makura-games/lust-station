using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared._Lust;

[CVarDefs]
public sealed partial class LustCCVars : CVars
{
    public static readonly CVarDef<bool> StationAlertErpRestriction =
        CVarDef.Create("erp.alert_level_restriction", false, CVar.SERVER | CVar.REPLICATED);
}
