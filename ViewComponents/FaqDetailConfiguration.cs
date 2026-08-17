using WebWayCMS.Attributes;

namespace WebWayCMS.TestHost.ViewComponents;

/// <summary>
/// Configuration for the routable <c>FaqDetail</c> widget.
/// </summary>
public class FaqDetailConfiguration
{
    [FormProperty(
        Label = "View",
        EditorType = EditorType.ViewPicker,
        ViewComponentName = "FaqDetail",
        Order = 1,
        FormComponent = "ViewPicker"
    )]
    public string? ViewName { get; set; }

    [FormProperty(
        Label = "FAQ",
        HelpText = "Fallback FAQ shown when the URL has no slug.",
        EditorType = EditorType.Guid,
        EntityType = "faqs",
        Order = 2,
        FormComponent = "EntityPicker"
    )]
    public Guid? FaqId { get; set; }

    [FormProperty(
        Label = "Show Back Link",
        EditorType = EditorType.Checkbox,
        Order = 3,
        FormComponent = "Checkbox"
    )]
    public bool ShowBackLink { get; set; }

    [FormProperty(
        Label = "Back Link Text",
        EditorType = EditorType.Text,
        Order = 4,
        FormComponent = "Text"
    )]
    public string? BackLinkText { get; set; }
}
