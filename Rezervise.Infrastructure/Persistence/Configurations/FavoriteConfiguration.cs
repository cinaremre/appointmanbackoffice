using Rezervise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rezervise.Infrastructure.Persistence.Configurations
{
    public class FavoriteConfiguration : BaseConfiguration<Favorite>
    {
        public override void Configure(EntityTypeBuilder<Favorite> builder)
        {
            base.Configure(builder);

            builder.ToTable("Favorites");

            builder.Property(x => x.UserId)
                .IsRequired();

            builder.Property(x => x.BusinessId)
                .IsRequired();

            builder.Property(x => x.AddedAt)
                .IsRequired();

            builder.Property(x => x.Note)
                .IsRequired(false)
                .HasMaxLength(500);

            builder.HasOne(x => x.User)
                .WithMany(x => x.Favorites)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Business)
                .WithMany(x => x.Favorites)
                .HasForeignKey(x => x.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.BusinessId);

            // Filtreli unique index: soft-delete edilmiş kayıtlar anahtarı işgal etmez.
            // Filtresiz index, favori çıkarıldıktan sonra yeniden eklemeyi
            // "zaten favorilerinizde" hatasına düşürüyordu.
            builder.HasIndex(x => new { x.UserId, x.BusinessId })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
        }
    }
}