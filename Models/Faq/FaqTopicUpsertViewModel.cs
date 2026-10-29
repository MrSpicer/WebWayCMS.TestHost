using WebWayCMS.Attributes;
using WebWayCMS.Models;

namespace WebWayCMS.TestHost.Models.Faq;

public sealed class FaqTopicUpsertViewModel : BaseContentViewModel
{
    [FormProperty(Label = "Description", EditorType = EditorType.TextArea, MaxLength = 1000, Order = 3, FormComponent = "TextArea")]
    public string Description { get; init; } = string.Empty;
}
