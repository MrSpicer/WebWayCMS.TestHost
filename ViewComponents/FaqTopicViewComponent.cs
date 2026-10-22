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
/// Host-owned routable widget. Generates two widget routes — <c>topics/{topic}</c> and
/// <c>topics/{topic}/page/{pageNumber:int}</c> — proving multi-route generation, literal segments inside a
/// widget pattern, and inline-constraint matching. The parameter is not named <c>page</c>: that is an
/// ASP.NET Core reserved routing name, and the CMS refuses to register a route that uses it.
/// </summary>
[ContentZoneComponent(
    DisplayName = "FAQ Topic",
    Description = "Renders FAQs filtered by a URL topic segment.",
    Category = "FAQ Test Routing",
    IconClass = "fa-tags",
    Order = 20,
    ConfigurationType = typeof(FaqTopicConfiguration)
)]
public class FaqTopicViewComponent : WidgetViewComponent, IRoutableViewComponent
{
    private readonly IContentStore<FaqDTO> _store;
    private readonly IMapper _mapper;

    public FaqTopicViewComponent(IContentStore<FaqDTO> store, IMapper mapper)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<IViewComponentResult> InvokeAsync(FaqTopicConfiguration config)
    {
        config ??= new FaqTopicConfiguration();

        var ct = HttpContext.RequestAborted;

        var topic = ViewContext.RouteData.Values.TryGetValue("topic", out var topicObj)
            ? topicObj?.ToString() ?? string.Empty
            : string.Empty;

        var page = 1;
        if (ViewContext.RouteData.Values.TryGetValue("pageNumber", out var pageObj)
            && int.TryParse(pageObj?.ToString(), out var parsedPage) && parsedPage > 0)
        {
            page = parsedPage;
        }

        var faqs = await _store.GetAllAsync(ct);
        var matched = faqs.Where(f => MatchesTopic(f, topic)).ToList();

        var pageSize = config.PageSize is > 0 ? config.PageSize.Value : 10;
        var paged = matched.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        var vm = new FaqTopicViewModel
        {
            Configuration = config,
            Faqs = paged.Select(f => _mapper.Map<FaqViewModel>(f)).ToList(),
            Topic = topic,
            Page = page,
            PageSize = pageSize,
            MatchCount = matched.Count,
        };

        return ModeView(config, vm);
    }

    private static bool MatchesTopic(FaqDTO faq, string topic)
    {
        if (string.IsNullOrEmpty(topic))
            return false;

        var slug = faq.Version.Slug ?? string.Empty;
        return slug.StartsWith(topic, StringComparison.OrdinalIgnoreCase)
            || faq.Question.Contains(topic, StringComparison.OrdinalIgnoreCase);
    }

    string IRoutableViewComponent.ComponentName => "FaqTopic";

    Task<IReadOnlyList<CMSRouteDTO>> IRoutableViewComponent.GenerateRoutesAsync(
        string parentRoute, Guid contentZoneItemNodeId, WidgetDisplayMode displayMode, CancellationToken ct)
    {
        var routes = new List<CMSRouteDTO>
        {
            new()
            {
                Pattern = "topics/{topic}",
                OwningContentNodeId = contentZoneItemNodeId,
                OwningContentType = "FaqTopicWidget",
                Order = 0
            },
            new()
            {
                Pattern = "topics/{topic}/page/{pageNumber:int}",
                OwningContentNodeId = contentZoneItemNodeId,
                OwningContentType = "FaqTopicWidget",
                Order = 1
            }
        };

        return Task.FromResult<IReadOnlyList<CMSRouteDTO>>(routes.AsReadOnly());
    }
}
