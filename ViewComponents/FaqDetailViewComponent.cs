using Microsoft.AspNetCore.Mvc;

using WebWayCMS.Attributes;
using WebWayCMS.Data.Models;
using WebWayCMS.Data.Services;
using WebWayCMS.Interfaces;
using WebWayCMS.Mapping;

using WebWayCMS.TestHost.Data;
using WebWayCMS.TestHost.Models.Faq;

namespace WebWayCMS.TestHost.ViewComponents;

/// <summary>
/// Host-owned routable widget. Generates one widget route (<c>{slug}</c>) that, when matched, renders
/// the single FAQ identified by the URL slug.
/// </summary>
[ContentZoneComponent(
    DisplayName = "FAQ Detail",
    Description = "Renders a single FAQ, resolved from the URL slug or the configured FAQ.",
    Category = "FAQ Test Routing",
    IconClass = "fa-file-text",
    Order = 10,
    ConfigurationType = typeof(FaqDetailConfiguration)
)]
public class FaqDetailViewComponent : ViewComponent, IRoutableViewComponent
{
    private readonly IContentStore<FaqDTO> _store;
    private readonly IMapper _mapper;

    public FaqDetailViewComponent(IContentStore<FaqDTO> store, IMapper mapper)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<IViewComponentResult> InvokeAsync(FaqDetailConfiguration config)
    {
        config ??= new FaqDetailConfiguration();

        var ct = HttpContext.RequestAborted;

        FaqDTO? dto = null;
        var source = "none";
        var slug = string.Empty;

        if (ViewContext.RouteData.Values.TryGetValue("slug", out var slugObj)
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
        // sharing a zone with the list widgets on the plain page, so render nothing rather than a
        // "not found" box. "NotFound" is reserved for a resolution that was attempted and failed.
        if (dto == null && source == "none")
            return Content(string.Empty);

        var vm = new FaqDetailViewModel
        {
            Configuration = config,
            Faq = dto == null ? null : _mapper.Map<FaqViewModel>(dto),
            Source = source,
            Slug = slug,
        };

        if (dto == null)
            return View("NotFound", vm);

        return View(config.ViewName ?? "Default", vm);
    }

    string IRoutableViewComponent.ComponentName => "FaqDetail";

    Task<IReadOnlyList<CMSRouteDTO>> IRoutableViewComponent.GenerateRoutesAsync(
        string parentRoute, Guid contentZoneItemNodeId, CancellationToken ct)
    {
        // The constraint must be *inline* in the pattern: CMSRouteService.TryMatchPattern parses
        // constraints out of the pattern text and never reads CMSRouteDTO.ConstraintsJson, so a
        // constraint declared there would be silently unenforced. Keep the regex lowercase —
        // RouteRegistrationService.NormalizePattern lowercases the whole pattern.
        var route = new CMSRouteDTO
        {
            Pattern = "{slug:regex(^[a-z0-9-]+$)}",
            OwningContentNodeId = contentZoneItemNodeId,
            OwningContentType = "FaqDetailWidget",
            Order = 5
        };

        return Task.FromResult<IReadOnlyList<CMSRouteDTO>>(new List<CMSRouteDTO> { route }.AsReadOnly());
    }
}
