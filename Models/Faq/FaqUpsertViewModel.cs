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

    [Range(0, 5)]
    [FormProperty(Label = "Helpfulness Rating", HelpText = "Zero-to-five editorial helpfulness signal.", Order = 5, Min = 0, Max = 5, FormComponent = "StarRating")]
    public int HelpfulnessRating { get; init; }

    /// <summary>
    /// The topic this FAQ sits under: a containment relation, so the FAQ is hidden while its topic is not
    /// live, the topic's edit screen lists it, and deleting the topic deletes it. Optional — a FAQ with no
    /// topic is a root. The mapper moves the value to and from the version's relations.
    /// </summary>
    [ContentRelation("faqtopics", Kind = ContentRelationKind.Containment)]
    [FormProperty(Label = "Topic", HelpText = "The FAQ topic this question belongs to.", Order = 6)]
    public Guid? FaqTopicId { get; init; }
}
