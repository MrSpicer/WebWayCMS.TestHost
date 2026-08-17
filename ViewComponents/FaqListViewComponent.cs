using Microsoft.AspNetCore.Mvc;

using WebWayCMS.Attributes;

namespace WebWayCMS.TestHost.ViewComponents;

/// <summary>
/// Host-owned content-zone component. Its <c>[ContentZoneComponent]</c> attribute is what the
/// widget-registration seeder discovers, proving a host assembly reaches the seeders.
/// </summary>
[ContentZoneComponent(
    DisplayName = "FAQ List",
    Description = "Renders a list of frequently asked questions.",
    Category = "Content",
    IconClass = "fa-question-circle",
    Order = 100
)]
public class FaqListViewComponent : ViewComponent
{
    public IViewComponentResult Invoke() => Content("FAQ List");
}
