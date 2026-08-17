using Microsoft.AspNetCore.Mvc;

using WebWayCMS.Attributes;
using WebWayCMS.Controllers;

using WebWayCMS.TestHost.Pages;

namespace WebWayCMS.TestHost.Controllers;

/// <summary>
/// The hub page type — an index of the whole page-type test suite. It is a page *type*, not a
/// wired-up route: there is deliberately no navbar link and no seeder, so it only exists once an
/// operator creates a page with it (slug <c>page-test</c>).
/// </summary>
[PageController(
    DisplayName = "Page Test Hub",
    Description = "Index of the host's page-type tests and the admin steps that light them up.",
    Category = "Page Tests",
    ConfigurationType = typeof(PageTestHubConfiguration),
    IconClass = "fa-compass",
    Order = 0)]
public class PageTestHubController : PageControllerBase<PageTestHubConfiguration>
{
    public override Task<IActionResult> Index()
    {
        return Task.FromResult<IActionResult>(View(PageConfig));
    }
}
