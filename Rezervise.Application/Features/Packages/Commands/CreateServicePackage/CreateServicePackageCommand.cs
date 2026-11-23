using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Packages.Commands.CreateServicePackage
{
    /// <summary>
    /// İşletme için yeni seans paketi tanımı oluşturur (doküman Modül 5).
    /// Yetki: işletme sahibi; kimlik JWT'den çözülür.
    /// </summary>
    public class CreateServicePackageCommand : IRequest<Result<Guid>>
    {
        public Guid BusinessId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int ValidityDays { get; set; }

        /// <summary>Paketin içindeki hizmet kalemleri.</summary>
        public List<ServicePackageItemInput> Items { get; set; } = new();
    }

    /// <summary>Tek paket kalemi: hizmet ve dahil seans sayısı.</summary>
    public class ServicePackageItemInput
    {
        public Guid ServiceId { get; set; }
        public int IncludedSessions { get; set; }
    }
}
