using Microsoft.AspNetCore.Mvc;

using WebWayCMS.Attributes;
using WebWayCMS.Controllers;

using WebWayCMS.TestHost.Pages;

namespace WebWayCMS.TestHost.Controllers;

/// <summary>
/// The multi-view, multi-zone page type. Mirrors <c>GenericAdminPageController</c>'s view-override
/// shape — honours any non-empty <c>ViewName</c> (unlike <c>GenericPageController</c>, which
/// special-cases <c>"Default"</c>). Each view places a different set of content zones.
/// </summary>
[PageController(
    DisplayName = "Zone Showcase Page",
    Description = "Four views over one page, exercising page-scoped, global and nested content zones.",
    Category = "Page Tests",
    ConfigurationType = typeof(ZoneShowcasePageConfiguration),
    IconClass = "fa-table-columns",
    Order = 10)]
public class ZoneShowcasePageController : PageControllerBase<ZoneShowcasePageConfiguration>
{
    private readonly Serilog.ILogger _logger = Serilog.Log.ForContext<ZoneShowcasePageController>();

    public override Task<IActionResult> Index()
    {
        var viewName = CurrentPage?.ViewName;
        var effective = string.IsNullOrWhiteSpace(viewName) ? "Index" : viewName;
        _logger.Information("Rendering zone showcase page {NodeId} with view {View}",
            CurrentPage?.Version.Node.Id, effective);
        ViewData["ActiveView"] = effective;
        return Task.FromResult<IActionResult>(View(effective, PageConfig));
    }
}
