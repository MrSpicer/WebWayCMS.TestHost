using WebWayCMS.Attributes;
using WebWayCMS.ContentZones;

namespace WebWayCMS.TestHost.ViewComponents;

/// <summary>
/// Configuration for the routable <c>FaqTopic</c> widget. The view pickers come from
/// <see cref="WidgetConfiguration"/>.
/// </summary>
public class FaqTopicConfiguration : WidgetConfiguration
{
    [FormProperty(
        Label = "Page Size",
        EditorType = EditorType.Number,
        Min = 1,
        Max = 50,
        Order = 1,
        FormComponent = "Number"
    )]
    // Nullable deliberately — see the note on FaqConfiguration.MaxItems: a null from an empty
    // number field would otherwise fail deserialization and drop the whole configuration.
    public int? PageSize { get; set; } = 10;

    [FormProperty(
        Label = "Empty Message",
        EditorType = EditorType.Text,
        Order = 3,
        FormComponent = "Text"
    )]
    public string? EmptyMessage { get; set; }
}
