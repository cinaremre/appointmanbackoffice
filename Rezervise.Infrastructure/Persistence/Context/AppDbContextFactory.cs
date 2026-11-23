using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Rezervise.Infrastructure.Persistence.Context
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

            // Bağlantı dizesini burada doğrudan belirtiyoruz.
            // Daha sonra API katmanındaki appsettings.json'dan okumak daha iyi olacaktır.
            optionsBuilder.UseSqlServer("Server=.;Database=RezerviseDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True");

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}