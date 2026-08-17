using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using WebWayCMS.Data.DbContexts;

namespace WebWayCMS.TestHost.Data;

public sealed class FaqDTOEntityConfiguration : IEntityTypeConfiguration<FaqDTO>
{
    public void Configure(EntityTypeBuilder<FaqDTO> entity)
    {
        entity.ConfigureContentLink();
        entity.Property(e => e.Question).IsRequired().HasMaxLength(500);
        entity.Property(e => e.Answer).IsRequired();
        entity.ToTable("Faqs");
    }
}
