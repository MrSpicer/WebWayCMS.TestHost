using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using WebWayCMS.Data.DbContexts;

namespace WebWayCMS.TestHost.Data;

public sealed class FaqTopicDTOEntityConfiguration : IEntityTypeConfiguration<FaqTopicDTO>
{
    public void Configure(EntityTypeBuilder<FaqTopicDTO> entity)
    {
        entity.ConfigureContentLink();
        entity.Property(e => e.Description).IsRequired().HasMaxLength(1000);
        entity.ToTable("FaqTopics");
    }
}
