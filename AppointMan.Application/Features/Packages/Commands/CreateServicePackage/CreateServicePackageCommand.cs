using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Packages.Commands.CreateServicePackage
{
    /// <summary>
    /// Isletme icin yeni seans paketi tanimi olusturur (dokuman Modul 5).
    /// Yetki: isletme sahibi; kimlik JWT'den cozulur.
    /// </summary>
    public class CreateServicePackageCommand : IRequest<Result<Guid>>
    {
        public Guid BusinessId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int ValidityDays { get; set; }

        /// <summary>Paketin icindeki hizmet kalemleri.</summary>
        public List<ServicePackageItemInput> Items { get; set; } = new();
    }

    /// <summary>Tek paket kalemi: hizmet ve dahil seans sayisi.</summary>
    public class ServicePackageItemInput
    {
        public Guid ServiceId { get; set; }
        public int IncludedSessions { get; set; }
    }
}
