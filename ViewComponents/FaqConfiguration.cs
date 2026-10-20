using WebWayCMS.Attributes;
using WebWayCMS.ContentZones;

namespace WebWayCMS.TestHost.ViewComponents;

/// <summary>
/// Configuration for the list-capable <c>Faq</c> widget. Fields are tagged with the display mode they
/// apply to (<see cref="FormPropertyAttribute.DisplayModes"/>), so the zone editor shows a different
/// configurator for Single (one FAQ: from the URL slug or <see cref="FaqId"/>) and List (a sorted,
/// limited list); untagged fields appear in both.
///
/// List mode keeps one property per <see cref="EditorType"/> so the admin "Add Widget" modal
/// exercises every editor in the form system; the entity pickers target the host <c>faqs</c> content
/// type via the generic entity-picker endpoint fallback. The trailing <see cref="HeadingIcon"/>
/// property exercises a host-contributed form component instead. The view pickers
/// (<c>ViewName</c> for single, <c>ListViewName</c> for list) come from <see cref="WidgetConfiguration"/>.
/// </summary>
public class FaqConfiguration : WidgetConfiguration
{
    // ─── Single ──────────────────────────────────────────────────────────────

    [FormProperty(
        Label = "FAQ",
        HelpText = "Fallback FAQ shown when the URL has no slug.",
        EditorType = EditorType.Guid,
        EntityType = "faqs",
        DisplayModes = WidgetDisplayMode.Single,
        Order = 1,
        FormComponent = "EntityPicker"
    )]
    public Guid? FaqId { get; set; }

    [FormProperty(
        Label = "Show Back Link",
        EditorType = EditorType.Checkbox,
        DisplayModes = WidgetDisplayMode.Single,
        Order = 2,
        FormComponent = "Checkbox"
    )]
    public bool ShowBackLink { get; set; }

    [FormProperty(
        Label = "Back Link Text",
        EditorType = EditorType.Text,
        DisplayModes = WidgetDisplayMode.Single,
        Order = 3,
        FormComponent = "Text"
    )]
    public string? BackLinkText { get; set; }

    // ─── List ────────────────────────────────────────────────────────────────

    [FormProperty(
        Label = "Heading",
        HelpText = "Shown above the FAQ list.",
        EditorType = EditorType.Text,
        IsRequired = true,
        MaxLength = 60,
        DisplayModes = WidgetDisplayMode.List,
        Order = 11,
        FormComponent = "Text"
    )]
    public string Heading { get; set; } = string.Empty;

    [FormProperty(
        Label = "Intro",
        EditorType = EditorType.TextArea,
        Group = "Copy",
        DisplayModes = WidgetDisplayMode.List,
        Order = 12,
        FormComponent = "TextArea"
    )]
    public string? Intro { get; set; }

    [FormProperty(
        Label = "Footnote",
        EditorType = EditorType.RichText,
        Group = "Copy",
        DisplayModes = WidgetDisplayMode.List,
        Order = 13,
        FormComponent = "RichText"
    )]
    public string? Footnote { get; set; }

    [FormProperty(
        Label = "Max Items",
        EditorType = EditorType.Number,
        Min = 1,
        Max = 20,
        DisplayModes = WidgetDisplayMode.List,
        Order = 14,
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
        DisplayModes = WidgetDisplayMode.List,
        Order = 15,
        FormComponent = "Checkbox"
    )]
    public bool ShowAnswers { get; set; }

    [FormProperty(
        Label = "Sort Order",
        EditorType = EditorType.Dropdown,
        DropdownOptions = "title-asc:Title A-Z,title-desc:Title Z-A,newest:Newest first",
        DisplayModes = WidgetDisplayMode.List,
        Order = 16,
        FormComponent = "Dropdown"
    )]
    public string? SortOrder { get; set; }

    [FormProperty(
        Label = "Anchor Slug",
        HelpText = "Optional URL anchor for the list wrapper.",
        EditorType = EditorType.Text,
        Pattern = "^[a-z0-9-]*$",
        PatternErrorMessage = "Anchor slug may contain only lowercase letters, numbers and hyphens.",
        DisplayModes = WidgetDisplayMode.List,
        Order = 17,
        FormComponent = "Text"
    )]
    public string? AnchorSlug { get; set; }

    [FormProperty(
        Label = "Accent Color",
        EditorType = EditorType.Color,
        Order = 18,
        FormComponent = "Color"
    )]
    public string? AccentColor { get; set; }

    [FormProperty(
        Label = "More URL",
        EditorType = EditorType.Url,
        Order = 19,
        FormComponent = "Url"
    )]
    public string? MoreUrl { get; set; }

    [FormProperty(
        Label = "Contact Email",
        EditorType = EditorType.Email,
        Order = 20,
        FormComponent = "Email"
    )]
    public string? ContactEmail { get; set; }

    [FormProperty(
        Label = "Effective Date",
        EditorType = EditorType.Date,
        Order = 21,
        FormComponent = "Date"
    )]
    public DateOnly? EffectiveDate { get; set; }

    [FormProperty(
        Label = "Reviewed At",
        EditorType = EditorType.DateTime,
        Order = 22,
        FormComponent = "DateTime"
    )]
    public DateTime? ReviewedAt { get; set; }

    [FormProperty(
        Label = "Featured FAQ",
        HelpText = "A single FAQ promoted above the list.",
        EditorType = EditorType.Guid,
        EntityType = "faqs",
        DisplayModes = WidgetDisplayMode.List,
        Order = 23,
        FormComponent = "EntityPicker"
    )]
    public Guid? FeaturedFaqId { get; set; }

    [FormProperty(
        Label = "Detail Page Controller",
        EditorType = EditorType.PageControllerPicker,
        DisplayModes = WidgetDisplayMode.List,
        Order = 25,
        FormComponent = "PageControllerPicker"
    )]
    public string? DetailPageController { get; set; }

    [FormProperty(
        Label = "Test Marker",
        EditorType = EditorType.Hidden,
        Order = 26,
        FormComponent = "Hidden"
    )]
    public string? TestMarker { get; set; }

    [FormProperty(
        Label = "Heading Icon",
        HelpText = "Font Awesome class rendered before the heading.",
        Order = 27,
        FormComponent = "IconPicker",
        DropdownOptions = "fa-solid fa-circle-question:Question mark,fa-solid fa-book:Book,fa-solid fa-lightbulb:Lightbulb,fa-solid fa-star:Star"
    )]
    public string? HeadingIcon { get; set; }
}
