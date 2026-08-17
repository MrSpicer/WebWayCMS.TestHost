using WebWayCMS.Data.Models;
using WebWayCMS.Mapping;

using WebWayCMS.TestHost.Data;

namespace WebWayCMS.TestHost.Models.Faq;

/// <summary>
/// Host-owned mapping profile, contributed to the shared <see cref="IMapper"/> via
/// <c>cms.AddMappingProfile(new FaqMappingProfile())</c>.
/// </summary>
public sealed class FaqMappingProfile : Profile
{
    public FaqMappingProfile()
    {
        CreateMap<FaqDTO, FaqViewModel>(s => new FaqViewModel
        {
            NodeId = s.Version.Node.Id,
            Title = s.Version.Title ?? string.Empty,
            Slug = s.Version.Slug ?? string.Empty,
            Question = s.Question,
            Answer = s.Answer,
            PublicationDate = s.Version.PublishStartUtc,
            PublicationEndDate = s.Version.PublishEndUtc,
            IsHidden = s.Version.Node.IsHidden,
            CreationDate = s.Version.Node.CreatedUtc,
            ModificationDate = s.Version.CreatedUtc,
        });

        CreateMap<FaqDTO, FaqUpsertViewModel>(s => new FaqUpsertViewModel
        {
            NodeId = s.Version.Node.Id,
            ExpectedVersionNumber = s.Version.VersionNumber,
            Title = s.Version.Title ?? string.Empty,
            Slug = s.Version.Slug ?? string.Empty,
            Question = s.Question,
            Answer = s.Answer,
            PublicationDate = s.Version.PublishStartUtc,
            PublicationEndDate = s.Version.PublishEndUtc,
            IsHidden = s.Version.Node.IsHidden,
            IsPublished = s.Version.State == ContentVersionState.Published,
            CreationDate = s.Version.Node.CreatedUtc,
            ModificationDate = s.Version.CreatedUtc,
        });

        CreateMap<FaqUpsertViewModel, FaqDTO>(s => new FaqDTO
        {
            Question = s.Question,
            Answer = s.Answer,
            Version = new ContentVersion
            {
                Node = new ContentNode { Id = s.NodeId ?? Guid.Empty, IsHidden = s.IsHidden },
                Title = s.Title ?? string.Empty,
                Slug = s.Slug ?? string.Empty,
                PublishStartUtc = s.PublicationDate,
                PublishEndUtc = s.PublicationEndDate,
            },
        });

        CreateMap<FaqDTO, FaqItemViewModel>(s => new FaqItemViewModel
        {
            NodeId = s.Version.Node.Id,
            Title = s.Version.Title ?? string.Empty,
            Slug = s.Version.Slug ?? string.Empty,
            IsPublished = s.Version.State == ContentVersionState.Published,
            CreationDate = s.Version.Node.CreatedUtc,
            ModificationDate = s.Version.CreatedUtc,
        });
    }
}
