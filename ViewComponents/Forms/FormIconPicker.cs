using Microsoft.AspNetCore.Mvc;

using WebWayCMS.Attributes;
using WebWayCMS.Forms;
using WebWayCMS.ViewComponents.Forms;

namespace WebWayCMS.TestHost.ViewComponents.Forms;

/// <summary>
/// Host-contributed form component. Name-only (no EditorType alias), selected with
/// [FormProperty(FormComponent = "IconPicker")]. Emits exactly one [data-prop] element (a &lt;select&gt;),
/// so it is safe on JSON-bound (widget/page) configuration forms as well as model-bound ones.
/// Font Awesome is loaded by the admin layout but NOT by the host's public Bootstrap layout, so the
/// select and its preview render correctly in the admin; on the public page only the rendered
/// &lt;i class="…"&gt; element and the widget's own data-cfg-* attribute are meaningful.
/// </summary>
[CMSFormComponent("IconPicker", DataTypes = new[] { typeof(string) },
                  IsDefaultForType = false,
                  Category = "Host Test", Order = 2, IconClass = "fa-icons",
                  DisplayName = "Icon Picker",
                  Description = "Font Awesome class picker with a server-rendered preview of the saved value (updates on reload, not as the select changes).")]
public sealed class FormIconPicker : FormFieldViewComponentBase
{
    private static readonly Dictionary<string, string> DefaultOptions = new()
    {
        ["fa-solid fa-circle-question"] = "Question mark",
        ["fa-solid fa-book"] = "Book",
        ["fa-solid fa-lightbulb"] = "Lightbulb",
        ["fa-solid fa-star"] = "Star",
    };

    public override IViewComponentResult Invoke(FormFieldContext field, string? viewName = null)
    {
        var name = viewName ?? (field.Mode == FormFieldMode.Read ? "Read" : "Write");
        if (field.Mode == FormFieldMode.Read)
            return View(name, field);

        var options = field.DropdownOptions.Count > 0 ? field.DropdownOptions : DefaultOptions;

        var viewModel = new IconPickerViewModel
        {
            Field = field,
            Options = options,
        };

        return View(name, viewModel);
    }
}

public sealed class IconPickerViewModel
{
    public FormFieldContext Field { get; set; } = null!;
    public Dictionary<string, string> Options { get; set; } = new();
}
