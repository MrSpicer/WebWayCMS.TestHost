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
/// Host-owned routable widget. The <c>{topic}</c> segment names a <c>faqtopics</c> item by slug, and the
/// widget lists that topic's FAQs through the FAQ → topic containment
/// (<see cref="FaqModel.TopicRelation"/>), so an unknown or unpublished topic lists nothing. Generates two widget routes — <c>topics/{topic}</c> and
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
    private readonly IContentStore<FaqTopicDTO> _topicStore;
    private readonly IMapper _mapper;

    public FaqTopicViewComponent(IContentStore<FaqDTO> store, IContentStore<FaqTopicDTO> topicStore, IMapper mapper)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _topicStore = topicStore ?? throw new ArgumentNullException(nameof(topicStore));
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

        // Both reads follow the read context: a draft topic (or a FAQ under one) is visible only in preview.
        var topicDto = await _topicStore.GetBySlugAsync(topic, ct);
        var matched = topicDto == null
            ? new List<FaqDTO>()
            : (await _store.FindRelatedAsync(FaqModel.TopicRelation, topicDto.Version.Node.Id, ct))
                .OrderBy(f => f.Version.Title, StringComparer.OrdinalIgnoreCase)
                .ToList();

        var pageSize = config.PageSize is > 0 ? config.PageSize.Value : 10;
        var paged = matched.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        var vm = new FaqTopicViewModel
        {
            Configuration = config,
            Faqs = paged.Select(f => _mapper.Map<FaqViewModel>(f)).ToList(),
            Topic = topic,
            TopicTitle = topicDto?.Version.Title ?? string.Empty,
            Page = page,
            PageSize = pageSize,
            MatchCount = matched.Count,
        };

        return ModeView(config, vm);
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
