using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared._Lust.ErpStatus
{
    public sealed partial class ErpStatusSystem : EntitySystem
    {
        [Dependency] private ExamineSystemShared _examineSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ErpStatusComponent, GetVerbsEvent<ExamineVerb>>(OnGetExamineVerbs);
        }

        private void OnGetExamineVerbs(EntityUid uid, ErpStatusComponent component, GetVerbsEvent<ExamineVerb> args)
        {
            if (Identity.Name(args.Target, EntityManager) != MetaData(args.Target).EntityName)
                return;

            var detailsRange = _examineSystem.IsInDetailsRange(args.User, uid);

            var verb = new ExamineVerb()
            {
                Act = () =>
                {
                    var markup = new FormattedMessage();
                    markup.AddMarkupOrThrow("\n");
                    markup.AddMarkupOrThrow(Loc.GetString($"detail-examinable-erp-{component.Erp.ToString().ToLowerInvariant()}-text"));
                    _examineSystem.SendExamineTooltip(args.User, uid, markup, false, false);
                },
                Text = Loc.GetString("erp-status-verb-text"),
                Category = VerbCategory.Examine,
                Disabled = !detailsRange,
                Message = detailsRange ? null : Loc.GetString("erp-status-verb-disabled"),
                Icon = new SpriteSpecifier.Texture(new ("/Textures/_Lust/Interactions/heart.png"))
            };

            args.Verbs.Add(verb);
        }
    }
}
