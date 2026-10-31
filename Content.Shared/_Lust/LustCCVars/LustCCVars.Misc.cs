using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared._Lust.LustCCVars;

[CVarDefs]
public sealed partial class LustCCVars : CVars
{
    // <summary>
    //  Отключение или включение выдачи подарков при спавне на праздники
    // По умолчанию отключено, для того что бы не крашились ебаные тесты
    // Вигер с днём рождения
    // </summary>
    public static readonly CVarDef<bool> LustAllowHolidayGifts =
        CVarDef.Create("lust.allow_holiday_gifts", false, CVar.SERVERONLY);
}
