using Rezervise.Domain.Entities;
using Rezervise.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Rezervise.Infrastructure.Persistence.SeedData
{
    public static class EmployeeServiceSeed
    {
        public static async Task SeedAsync(AppDbContext context, ILogger logger)
        {
            if (await context.EmployeeServices.AnyAsync())
            {
                logger.LogInformation("EmployeeServices zaten mevcut, atlanıyor.");
                return;
            }

            var assignments = new List<(Guid EmployeeId, Guid ServiceId)>
            {
                // Hasan Usta (Emp1)
                (EmployeeSeed.Emp1Id, ServiceSeed.Svc1Id),
                (EmployeeSeed.Emp1Id, ServiceSeed.Svc2Id),
                (EmployeeSeed.Emp1Id, ServiceSeed.Svc3Id),

                // Emre Genç (Emp2)
                (EmployeeSeed.Emp2Id, ServiceSeed.Svc1Id),
                (EmployeeSeed.Emp2Id, ServiceSeed.Svc4Id),

                // Murat Kıdemli (Emp3)
                (EmployeeSeed.Emp3Id, ServiceSeed.Svc1Id),
                (EmployeeSeed.Emp3Id, ServiceSeed.Svc2Id),
                (EmployeeSeed.Emp3Id, ServiceSeed.Svc4Id),

                // Selin Saç (Emp4)
                (EmployeeSeed.Emp4Id, ServiceSeed.Svc5Id),
                (EmployeeSeed.Emp4Id, ServiceSeed.Svc6Id),
                (EmployeeSeed.Emp4Id, ServiceSeed.Svc7Id),
                (EmployeeSeed.Emp4Id, ServiceSeed.Svc8Id),

                // Deniz Stil (Emp5)
                (EmployeeSeed.Emp5Id, ServiceSeed.Svc5Id),
                (EmployeeSeed.Emp5Id, ServiceSeed.Svc7Id),
                (EmployeeSeed.Emp5Id, ServiceSeed.Svc8Id),

                // Kaan Dövmeci (Emp6)
                (EmployeeSeed.Emp6Id, ServiceSeed.Svc9Id),
                (EmployeeSeed.Emp6Id, ServiceSeed.Svc10Id),
                (EmployeeSeed.Emp6Id, ServiceSeed.Svc11Id),

                // Buse Sanat (Emp7)
                (EmployeeSeed.Emp7Id, ServiceSeed.Svc9Id),
                (EmployeeSeed.Emp7Id, ServiceSeed.Svc12Id),

                // İbrahim Usta (Emp9)
                (EmployeeSeed.Emp9Id, ServiceSeed.Svc13Id),
                (EmployeeSeed.Emp9Id, ServiceSeed.Svc14Id),

                // Dr. Aylın (Emp10)
                (EmployeeSeed.Emp10Id, ServiceSeed.Svc15Id),
                (EmployeeSeed.Emp10Id, ServiceSeed.Svc16Id),
                (EmployeeSeed.Emp10Id, ServiceSeed.Svc18Id),

                // Dr. Cem (Emp11)
                (EmployeeSeed.Emp11Id, ServiceSeed.Svc15Id),
                (EmployeeSeed.Emp11Id, ServiceSeed.Svc17Id),
                (EmployeeSeed.Emp11Id, ServiceSeed.Svc19Id),

                // Fatma Uzman (Emp12)
                (EmployeeSeed.Emp12Id, ServiceSeed.Svc20Id),
                (EmployeeSeed.Emp12Id, ServiceSeed.Svc21Id),
                (EmployeeSeed.Emp12Id, ServiceSeed.Svc22Id)
            };

            var employeeServices = assignments
                .Select(a => new EmployeeService(Guid.NewGuid(), a.EmployeeId, a.ServiceId))
                .ToList();

            await context.EmployeeServices.AddRangeAsync(employeeServices);
            await context.SaveChangesAsync();

            logger.LogInformation("EmployeeServices seed edildi: {Count} atama", employeeServices.Count);
        }
    }
}