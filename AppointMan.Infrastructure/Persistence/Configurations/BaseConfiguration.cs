using AppointMan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppointMan.Infrastructure.Persistence.Configurations
{
    public abstract class BaseConfiguration<T> : IEntityTypeConfiguration<T> where T : BaseEntity
    {
        public virtual void Configure(EntityTypeBuilder<T> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.UpdatedAt).IsRequired(false);
            builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
            builder.Property(x => x.DeletedAt).IsRequired(false);

            // Optimistic concurrency: SQL Server rowversion kolonu.
            // Es zamanli iki guncellemeden ikincisi DbUpdateConcurrencyException alir;
            // sessiz veri kaybi (lost update) onlenir.
            builder.Property(x => x.RowVersion).IsRowVersion();

            // Global query filter for soft delete
            builder.HasQueryFilter(x => !x.IsDeleted);
        }
    }
}