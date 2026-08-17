using System.ComponentModel.DataAnnotations;

using WebWayCMS.Attributes;
using WebWayCMS.Models;

namespace WebWayCMS.TestHost.Models.Faq;

public sealed class FaqUpsertViewModel : BaseContentViewModel
{
    [Required]
    [FormProperty(Label = "Question", EditorType = EditorType.Text, IsRequired = true, Order = 3, FormComponent = "Text")]
    public string Question { get; init; } = string.Empty;

    [Required]
    [FormProperty(Label = "Answer", EditorType = EditorType.RichText, IsRequired = true, Order = 4, FormComponent = "RichText")]
    public string Answer { get; init; } = string.Empty;
}
