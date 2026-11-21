using Robust.Shared.GameStates;
using Content.Shared.Humanoid;

namespace Content.Shared._Lust.ErpStatus
{
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class ErpStatusComponent : Component
    {
        [ViewVariables]
        public Erp? DefaultErp { get; set; } = Erp.Ask;

        [DataField(required: true), AutoNetworkedField]
        public Erp Erp = Erp.Ask;

        /// <summary>
        ///     ERP временно отключён кодом тревоги. Собственный выбор игрока в <see cref="Erp"/> при этом не меняется.
        /// </summary>
        [ViewVariables, AutoNetworkedField]
        public bool Disabled;
    }
}
