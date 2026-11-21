using System.Linq;
using Content.Shared.AlertLevel;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared._Sunrise.InteractionsPanel.Data.Components;
using Content.Shared.Verbs;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Lust.ErpStatus
{
    public sealed partial class ErpStatusSystem : EntitySystem
    {
        [Dependency] private ExamineSystemShared _examine = default!;
        [Dependency] private INetManager _net = default!;
        [Dependency] private IPrototypeManager _proto = default!;
        [Dependency] private IConfigurationManager _cfg = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ErpStatusComponent, GetVerbsEvent<ExamineVerb>>(OnGetExamineVerbs);
            SubscribeLocalEvent<AlertLevelChangedEvent>(OnAlertLevelChanged);
            SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
        }

        private void OnGetExamineVerbs(EntityUid uid, ErpStatusComponent component, GetVerbsEvent<ExamineVerb> args)
        {
            if (Identity.Name(args.Target, EntityManager) != MetaData(args.Target).EntityName)
                return;

            var detailsRange = _examine.IsInDetailsRange(args.User, uid);

            var verb = new ExamineVerb()
            {
                Act = () =>
                {
                    var markup = new FormattedMessage();
                    markup.AddMarkupOrThrow("\n");
                    markup.AddMarkupOrThrow(Loc.GetString(GetStatusLocKey(component)));
                    _examine.SendExamineTooltip(args.User, uid, markup, false, false);
                },
                Text = Loc.GetString("erp-status-verb-text"),
                Category = VerbCategory.Examine,
                Disabled = !detailsRange,
                Message = detailsRange ? null : Loc.GetString("erp-status-verb-disabled"),
                Icon = new SpriteSpecifier.Texture(new ("/Textures/_Lust/Interactions/heart.png"))
            };

            args.Verbs.Add(verb);
        }

        private void OnAlertLevelChanged(ref AlertLevelChangedEvent ev)
        {
            if (!_net.IsServer)
                return;

            if (!_cfg.GetCVar(LustCCVars.StationAlertErpRestriction))
                return;

            if (!_proto.Resolve(ev.AlertLevel, out AlertLevelPrototype? level))
                return;

            var query = EntityQueryEnumerator<ErpStatusComponent>();
            while (query.MoveNext(out var uid, out var erp))
            {
                Apply(uid, erp, level);
            }
        }

        private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
        {
            if (!_net.IsServer)
                return;

            if (!TryComp<AlertLevelComponent>(ev.Station, out var alertLevel))
                return;

            if (!_proto.Resolve(alertLevel.CurrentAlertLevel, out AlertLevelPrototype? level))
                return;

            if (!TryComp<ErpStatusComponent>(ev.Mob, out var erp))
                return;

            Apply(ev.Mob, erp, level);
        }

        private void Apply(EntityUid uid, ErpStatusComponent erp, AlertLevelPrototype level)
        {
            var disabled = level.ErpStatusRules.Any(rule
                => rule.Matches(uid, EntityManager));

            SetDisabled(uid, erp, disabled);
        }

        private void SetDisabled(EntityUid uid, ErpStatusComponent erp, bool disabled)
        {
            if (erp.Disabled != disabled)
            {
                erp.Disabled = disabled;
                Dirty(uid, erp);
            }

            var allowed = !disabled && erp.Erp != Erp.No;

            if (!TryComp<InteractionsComponent>(uid, out var interactions))
                return;

            if (!interactions.Erp == allowed)
                return;

            interactions.Erp = allowed;
            Dirty(uid, interactions);
        }

        private static string GetStatusLocKey(ErpStatusComponent component)
        {
            return component.Disabled
                ? "detail-examinable-erp-disabled-text"
                : $"detail-examinable-erp-{component.Erp.ToString().ToLowerInvariant()}-text";
        }
    }
}
