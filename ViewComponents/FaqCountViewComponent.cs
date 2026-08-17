using Microsoft.AspNetCore.Mvc;

using WebWayCMS.Attributes;
using WebWayCMS.Data.Services;

using WebWayCMS.TestHost.Data;

namespace WebWayCMS.TestHost.ViewComponents;

/// <summary>
/// Host-owned content-zone widget with <b>no</b> configuration type. Pins the zero-config path:
/// <c>PropertyDefinitionsJson == "[]"</c>, <c>HasConfiguration == false</c>,
/// <c>CreateDefaultConfiguration</c> returns null, and the add-widget modal renders with no fields.
/// </summary>
[ContentZoneComponent(
    DisplayName = "FAQ Count",
    Description = "Renders the number of published FAQs.",
    Category = "FAQ Test",
    IconClass = "fa-hashtag",
    Order = 20
)]
public class FaqCountViewComponent : ViewComponent
{
    private readonly IContentStore<FaqDTO> _store;

    public FaqCountViewComponent(IContentStore<FaqDTO> store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var faqs = await _store.GetAllAsync(HttpContext.RequestAborted);
        return View("Default", faqs.Count);
    }
}
