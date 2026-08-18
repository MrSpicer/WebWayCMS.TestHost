using WebWayCMS.Attributes;
using WebWayCMS.ViewComponents.Forms;

namespace WebWayCMS.TestHost.ViewComponents.Forms;

/// <summary>
/// Host-contributed form component. Name-only (no EditorType alias), selected with
/// [FormProperty(FormComponent = "StarRating")].
/// Model-bound forms only: it emits one radio per value, all sharing the field's name, so it must
/// NOT be used on a JSON-bound configuration form (form-components.js serializes every [data-prop]
/// element and the last one would win rather than the checked one).
/// </summary>
[CMSFormComponent("StarRating", DataTypes = new[] { typeof(int) },
                  IsDefaultForType = false,
                  Category = "Host Test", Order = 1, IconClass = "fa-star",
                  DisplayName = "Star Rating",
                  Description = "Zero-to-five star rating, rendered as a radio group (model-bound forms only).")]
public sealed class FormStarRating : FormFieldViewComponentBase { }
