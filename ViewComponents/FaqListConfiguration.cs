using WebWayCMS.Attributes;

namespace WebWayCMS.TestHost.ViewComponents;

/// <summary>
/// Configuration for the <c>FaqList</c> widget. One property per <see cref="EditorType"/> so the
/// admin "Add Widget" modal exercises every editor in the form system; the entity picker targets the
/// host <c>faqs</c> content type via the generic entity-picker endpoint fallback.
/// </summary>
public class FaqListConfiguration
{
    [FormProperty(
        Label = "Heading",
        HelpText = "Shown above the FAQ list.",
        EditorType = EditorType.Text,
        IsRequired = true,
        MaxLength = 60,
        Order = 1,
        FormComponent = "Text"
    )]
    public string Heading { get; set; } = string.Empty;

    [FormProperty(
        Label = "Intro",
        EditorType = EditorType.TextArea,
        Group = "Copy",
        Order = 2,
        FormComponent = "TextArea"
    )]
    public string? Intro { get; set; }

    [FormProperty(
        Label = "Footnote",
        EditorType = EditorType.RichText,
        Group = "Copy",
        Order = 3,
        FormComponent = "RichText"
    )]
    public string? Footnote { get; set; }

    [FormProperty(
        Label = "Max Items",
        EditorType = EditorType.Number,
        Min = 1,
        Max = 20,
        Order = 4,
        FormComponent = "Number"
    )]
    // Nullable deliberately: the admin form serializer writes null for an empty number field
    // (form-components.js), and System.Text.Json rejects null for a non-nullable int — which would
    // make ContentZoneModel.DeserializePropertiesToConfigType swallow the exception and drop the
    // *entire* saved configuration, not just this value.
    public int? MaxItems { get; set; } = 5;

    [FormProperty(
        Label = "Show Answers",
        EditorType = EditorType.Checkbox,
        GroupWithNext = true,
        Order = 5,
        FormComponent = "Checkbox"
    )]
    public bool ShowAnswers { get; set; }

    [FormProperty(
        Label = "Sort Order",
        EditorType = EditorType.Dropdown,
        DropdownOptions = "title-asc:Title A-Z,title-desc:Title Z-A,newest:Newest first",
        Order = 6,
        FormComponent = "Dropdown"
    )]
    public string? SortOrder { get; set; }

    [FormProperty(
        Label = "Anchor Slug",
        HelpText = "Optional URL anchor for the list wrapper.",
        EditorType = EditorType.Text,
        Pattern = "^[a-z0-9-]*$",
        PatternErrorMessage = "Anchor slug may contain only lowercase letters, numbers and hyphens.",
        Order = 7,
        FormComponent = "Text"
    )]
    public string? AnchorSlug { get; set; }

    [FormProperty(
        Label = "Accent Color",
        EditorType = EditorType.Color,
        Order = 8,
        FormComponent = "Color"
    )]
    public string? AccentColor { get; set; }

    [FormProperty(
        Label = "More URL",
        EditorType = EditorType.Url,
        Order = 9,
        FormComponent = "Url"
    )]
    public string? MoreUrl { get; set; }

    [FormProperty(
        Label = "Contact Email",
        EditorType = EditorType.Email,
        Order = 10,
        FormComponent = "Email"
    )]
    public string? ContactEmail { get; set; }

    [FormProperty(
        Label = "Effective Date",
        EditorType = EditorType.Date,
        Order = 11,
        FormComponent = "Date"
    )]
    public DateOnly? EffectiveDate { get; set; }

    [FormProperty(
        Label = "Reviewed At",
        EditorType = EditorType.DateTime,
        Order = 12,
        FormComponent = "DateTime"
    )]
    public DateTime? ReviewedAt { get; set; }

    [FormProperty(
        Label = "Featured FAQ",
        HelpText = "A single FAQ promoted above the list.",
        EditorType = EditorType.Guid,
        EntityType = "faqs",
        Order = 13,
        FormComponent = "EntityPicker"
    )]
    public Guid? FeaturedFaqId { get; set; }

    [FormProperty(
        Label = "View",
        EditorType = EditorType.ViewPicker,
        ViewComponentName = "FaqList",
        Order = 14,
        FormComponent = "ViewPicker"
    )]
    public string? ViewName { get; set; }

    [FormProperty(
        Label = "Detail Page Controller",
        EditorType = EditorType.PageControllerPicker,
        Order = 15,
        FormComponent = "PageControllerPicker"
    )]
    public string? DetailPageController { get; set; }

    [FormProperty(
        Label = "Test Marker",
        EditorType = EditorType.Hidden,
        Order = 16,
        FormComponent = "Hidden"
    )]
    public string? TestMarker { get; set; }
}
