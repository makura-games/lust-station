using Robust.Shared.GameStates;

namespace Content.Shared._Lust.Storage;

/// <summary>
/// Компонент для сущностей, которые слепнут при нахождении внутри сумок/хранилищ (StorageComponent).
/// Слепота не наносит урона глазам и не может быть вылечена окулином, пока сущность внутри.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BlindInStorageComponent : Component
{
    /// <summary>
    /// Находится ли сущность в данный момент внутри хранилища с предметами.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool InStorage;
}
