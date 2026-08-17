using Microsoft.AspNetCore.Mvc;

using WebWayCMS.Attributes;
using WebWayCMS.Controllers;

using WebWayCMS.TestHost.Pages;

namespace WebWayCMS.TestHost.Controllers;

/// <summary>
/// Proves the <c>ConfigurationJson</c> → <c>CMS:PageConfig</c> round-trip. The single view echoes
/// every configuration value as a <c>data-cfg-*</c> attribute so one assertion can verify the whole
/// admin-form → database → deserialization path.
/// </summary>
[PageController(
    DisplayName = "Configured Page",
    Description = "Single view echoing every configuration value to prove the ConfigurationJson round-trip.",
    Category = "Page Tests",
    ConfigurationType = typeof(ConfiguredPageConfiguration),
    IconClass = "fa-sliders",
    Order = 20)]
public class ConfiguredPageController : PageControllerBase<ConfiguredPageConfiguration>
{
    public override Task<IActionResult> Index()
    {
        return Task.FromResult<IActionResult>(View(PageConfig));
    }
}
