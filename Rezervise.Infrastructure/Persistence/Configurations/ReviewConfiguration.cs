using Rezervise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rezervise.Infrastructure.Persistence.Configurations
{
    public class ReviewConfiguration : BaseConfiguration<Review>
    {
        public override void Configure(EntityTypeBuilder<Review> builder)
        {
            base.Configure(builder);

            builder.ToTable("Reviews");

            builder.Property(x => x.Rating)
                .IsRequired();

            builder.Property(x => x.Comment)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(x => x.ReviewDate)
                .IsRequired();

            builder.Property(x => x.IsVisible)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(x => x.UserId)
                .IsRequired();

            builder.Property(x => x.BusinessId)
                .IsRequired();

            builder.Property(x => x.AppointmentId)
                .IsRequired(false);

            // User ilişkisi - User'da Reviews var
            builder.HasOne(x => x.User)
                .WithMany(x => x.Reviews)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Business ilişkisi - Business'da Reviews var
            builder.HasOne(x => x.Business)
                .WithMany(x => x.Reviews)
                .HasForeignKey(x => x.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);

            // Appointment ilişkisi - Appointment'da Reviews yok, bu yüzden WithMany() boş
            builder.HasOne(x => x.Appointment)
                .WithMany()
                .HasForeignKey(x => x.AppointmentId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.Rating);
            builder.HasIndex(x => x.ReviewDate);

            // Randevu basına yalnızca bir yorum yapılabilir.
            // AppointmentId null olan yorumlar filtre dışında kalır.
            builder.HasIndex(x => x.AppointmentId)
                .IsUnique()
                .HasFilter("[AppointmentId] IS NOT NULL")
                .HasDatabaseName("UX_Reviews_AppointmentId");
        }
    }
}