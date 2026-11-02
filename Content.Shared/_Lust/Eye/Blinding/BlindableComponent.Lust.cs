// Lust added start - Слепота внутри хранилищ предметов

using Content.Shared.Eye.Blinding.Systems;

namespace Content.Shared.Eye.Blinding.Components;

public sealed partial class BlindableComponent
{
    /// <summary>
    /// Находится ли существо в данный момент внутри предмета-хранилища (StorageComponent).
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool InStorage;
}
// Lust added end
