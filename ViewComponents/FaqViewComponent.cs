using Microsoft.AspNetCore.Mvc;

using WebWayCMS.Attributes;
using WebWayCMS.Data.Models;
using WebWayCMS.Data.Services;
using WebWayCMS.Interfaces;
using WebWayCMS.Mapping;
using WebWayCMS.ViewComponents;

using WebWayCMS.TestHost.Data;
using WebWayCMS.TestHost.Models.Faq;

namespace WebWayCMS.TestHost.ViewComponents;

/// <summary>
/// Host-owned, list-capable, routable content-zone widget, read through
/// <see cref="IContentStore{FaqDTO}"/> (read-context aware). The item's display mode picks the
/// behaviour:
///
/// <list type="bullet">
/// <item><b>Single</b> renders one FAQ, resolved from the URL slug (the widget's <c>{slug}</c>
/// route) or the configured <see cref="FaqConfiguration.FaqId"/>; <c>view.*</c> views.</item>
/// <item><b>List</b> renders a sorted, limited list exercising every <see cref="EditorType"/> via
/// <see cref="FaqConfiguration"/>; <c>list.*</c> views.</item>
/// </list>
/// </summary>
[ContentZoneComponent(
    DisplayName = "FAQ",
    Description = "Renders a single FAQ (from the URL slug or the configured FAQ) or a list of FAQs.",
    Category = "FAQ Test",
    IconClass = "fa-circle-question",
    Order = 10,
    SupportsList = true,
    ConfigurationType = typeof(FaqConfiguration)
)]
public class FaqViewComponent : WidgetViewComponent, IRoutableViewComponent
{
    private readonly IContentStore<FaqDTO> _store;
    private readonly IMapper _mapper;

    public FaqViewComponent(IContentStore<FaqDTO> store, IMapper mapper)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<IViewComponentResult> InvokeAsync(FaqConfiguration config)
    {
        config ??= new FaqConfiguration();

        return DisplayModeFor(config) == WidgetDisplayMode.List
            ? await InvokeListAsync(config)
            : await InvokeSingleAsync(config);
    }

    private async Task<IViewComponentResult> InvokeListAsync(FaqConfiguration config)
    {
        // The same page zone renders on the routing widgets' own URLs (/page/{slug}, /page/topics/...),
        // so a list yields on a widget route and lets the single-mode Faq/FaqTopic take over. Every CMS
        // request carries a "slug" route value from the {**slug} catch-all, so its presence is no signal.
        // The zone editor always renders it so it stays visible.
        var isEditMode = RenderModeFor(config) == WidgetRenderMode.Edit;
        if (!isEditMode && IsWidgetRouteRequest())
            return Content(string.Empty);

        var ct = HttpContext.RequestAborted;

        var faqs = SortFaqs(await _store.GetAllAsync(ct), config.SortOrder);

        var limited = faqs
            .Take(config.MaxItems is > 0 ? config.MaxItems.Value : int.MaxValue)
            .Select(f => _mapper.Map<FaqViewModel>(f))
            .ToList();

        FaqViewModel? featured = null;
        if (config.FeaturedFaqId.HasValue && config.FeaturedFaqId.Value != Guid.Empty)
        {
            var featuredDto = await _store.GetAsync(config.FeaturedFaqId.Value, ct);
            if (featuredDto != null)
                featured = _mapper.Map<FaqViewModel>(featuredDto);
        }

        var vm = new FaqListViewModel
        {
            Configuration = config,
            Faqs = limited,
            FeaturedFaq = featured,
        };

        return ModeView(config, vm);
    }

    private async Task<IViewComponentResult> InvokeSingleAsync(FaqConfiguration config)
    {
        var ct = HttpContext.RequestAborted;

        FaqDTO? dto = null;
        var source = "none";
        var slug = string.Empty;

        // Only this item's own {slug} route addresses a slug to it; on a plain page request "slug" is the
        // page path itself.
        if (IsOwnWidgetRoute(config)
            && ViewContext.RouteData.Values.TryGetValue("slug", out var slugObj)
            && slugObj is string slugValue && !string.IsNullOrEmpty(slugValue))
        {
            slug = slugValue;
            dto = await _store.GetBySlugAsync(slugValue, ct);
            source = dto != null ? "route" : "missing";
        }

        var hasConfiguredFaq = config.FaqId.HasValue && config.FaqId.Value != Guid.Empty;
        if (dto == null && hasConfiguredFaq)
        {
            dto = await _store.GetAsync(config.FaqId!.Value, ct);
            source = dto != null ? "config" : "missing";
        }

        // Nothing was even attempted — no slug in the URL and no configured fallback. The widget is
        // sharing a zone with list widgets on the plain page, so render nothing rather than a
        // "not found" box. "NotFound" is reserved for a resolution that was attempted and failed.
        // The zone editor always renders the widget (edit view or generic card) so it stays visible.
        var isEditMode = RenderModeFor(config) == WidgetRenderMode.Edit;
        if (dto == null && source == "none" && !isEditMode)
            return Content(string.Empty);

        var vm = new FaqDetailViewModel
        {
            Configuration = config,
            Faq = dto == null ? null : _mapper.Map<FaqViewModel>(dto),
            Source = source,
            Slug = slug,
        };

        // "_NotFound" is a state, not a selectable variant: the underscore keeps it out of the view
        // picker.
        if (dto == null && !isEditMode)
            return View("_NotFound", vm);

        return ModeView(config, vm);
    }

    private static List<FaqDTO> SortFaqs(List<FaqDTO> faqs, string? sortOrder)
    {
        return sortOrder switch
        {
            "title-desc" => faqs.OrderByDescending(f => f.Version.Title).ToList(),
            "newest" => faqs.OrderByDescending(f => f.Version.Node.CreatedUtc).ToList(),
            _ => faqs.OrderBy(f => f.Version.Title).ToList(),
        };
    }

    string IRoutableViewComponent.ComponentName => "Faq";

    Task<IReadOnlyList<CMSRouteDTO>> IRoutableViewComponent.GenerateRoutesAsync(
        string parentRoute, Guid contentZoneItemNodeId, WidgetDisplayMode displayMode, CancellationToken ct)
    {
        // A list owns no routes: a {slug} route on it would answer every unknown URL under its page.
        if (displayMode == WidgetDisplayMode.List)
            return Task.FromResult<IReadOnlyList<CMSRouteDTO>>([]);

        //
        // The constraint must be *inline* in the pattern: CMSRouteService.TryMatchPattern parses
        // constraints out of the pattern text and never reads CMSRouteDTO.ConstraintsJson, so a
        // constraint declared there would be silently unenforced. Keep the regex lowercase —
        // RouteRegistrationService.NormalizePattern lowercases the whole pattern.
        var route = new CMSRouteDTO
        {
            Pattern = "{slug:regex(^[a-z0-9-]+$)}",
            OwningContentNodeId = contentZoneItemNodeId,
            OwningContentType = "FaqWidget",
            Order = 5
        };

        return Task.FromResult<IReadOnlyList<CMSRouteDTO>>(new List<CMSRouteDTO> { route }.AsReadOnly());
    }
}
