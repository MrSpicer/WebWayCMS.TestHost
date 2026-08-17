using Microsoft.AspNetCore.Mvc;

using WebWayCMS.Attributes;
using WebWayCMS.Data.Services;
using WebWayCMS.Mapping;

using WebWayCMS.TestHost.Data;
using WebWayCMS.TestHost.Models.Faq;

namespace WebWayCMS.TestHost.ViewComponents;

/// <summary>
/// Host-owned content-zone widget. Renders a configurable list of FAQs read through
/// <see cref="IContentStore{FaqDTO}"/> (read-context aware), exercising every
/// <see cref="EditorType"/> via <see cref="FaqListConfiguration"/>.
/// </summary>
[ContentZoneComponent(
    DisplayName = "FAQ List",
    Description = "Renders a list of frequently asked questions.",
    Category = "FAQ Test",
    IconClass = "fa-list-ul",
    Order = 10,
    ConfigurationType = typeof(FaqListConfiguration)
)]
public class FaqListViewComponent : ViewComponent
{
    private readonly IContentStore<FaqDTO> _store;
    private readonly IMapper _mapper;

    public FaqListViewComponent(IContentStore<FaqDTO> store, IMapper mapper)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<IViewComponentResult> InvokeAsync(FaqListConfiguration config)
    {
        config ??= new FaqListConfiguration();

        // The same page zone renders on the routing widgets' own URLs, so a non-routing list yields
        // when a routing parameter is present and lets FaqDetail/FaqTopic take over.
        var routeValues = ViewContext.RouteData.Values;
        if (routeValues.ContainsKey("slug") || routeValues.ContainsKey("topic"))
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

        return View(config.ViewName ?? "Default", vm);
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
}
