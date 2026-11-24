using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using Rezervise.Domain.ValueObjects;
using MediatR;

namespace Rezervise.Application.Features.Businesses.Queries.GetBusinessById
{
    public class GetBusinessByIdQueryHandler : IRequestHandler<GetBusinessByIdQuery, Result<BusinessDetailDto>>
    {
        private readonly IRepository<Business> _businessRepository;
        private readonly IRepository<City> _cityRepository;
        private readonly IRepository<District> _districtRepository;

        public GetBusinessByIdQueryHandler(
            IRepository<Business> businessRepository,
            IRepository<City> cityRepository,
            IRepository<District> districtRepository)
        {
            _businessRepository = businessRepository;
            _cityRepository = cityRepository;
            _districtRepository = districtRepository;
        }

        public async Task<Result<BusinessDetailDto>> Handle(GetBusinessByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. İşletmeyi ID'ye göre getir
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);

                // 2. İşletme bulunamadıysa hata döndür
                if (business is null)
                    return Result<BusinessDetailDto>.Failure(Error.NotFound("İşletme", request.BusinessId.ToString()));

                // 3. Adres nesnesini kur: kaydederken gönderilen alanlar (cityId, street,
                //    postalCode, district, city, country) geri döner; uygulama düzenleme
                //    ekranında cityId'yi buradan okur.
                var addressDto = await BuildAddressDtoAsync(business.Address, cancellationToken);

                // 4. Verileri DTO'ya dönüştür
                var dto = new BusinessDetailDto
                {
                    Id = business.Id,
                    Name = business.Name,
                    Description = business.Description,
                    BusinessType = business.BusinessType,
                    Address = addressDto,
                    PhoneNumber = business.PhoneNumber is null
                        ? null
                        : new ContactValueDto(business.PhoneNumber.Value),
                    Email = business.Email is null
                        ? null
                        : new ContactValueDto(business.Email.Value),
                    WebsiteUrl = business.WebsiteUrl,
                    LogoUrl = business.LogoUrl,
                    IsActive = business.IsActive,
                    IsVerified = business.IsVerified,
                    IsApproved = business.IsApproved,
                    Rating = business.Rating,
                    TotalReviews = business.TotalReviews,
                    OwnerId = business.OwnerId,
                    CreatedAt = business.CreatedAt
                };

                return Result<BusinessDetailDto>.Success(dto);
            }
            catch (Exception ex)
            {
                return Result<BusinessDetailDto>.Failure(Error.Internal("İşletme bilgileri getirilirken bir hata oluştu.", ex.Message));
            }
        }

        private async Task<AddressDto?> BuildAddressDtoAsync(
            Address? address,
            CancellationToken cancellationToken)
        {
            if (address is null)
                return null;

            // İl/ilçe adlarını çözümle (id'ler Address içinde tutulur).
            var city = (await _cityRepository.FindAsync(c => c.Id == address.CityId, cancellationToken))
                .FirstOrDefault();

            District? district = null;
            if (address.DistrictId.HasValue)
            {
                district = (await _districtRepository.FindAsync(
                    d => d.Id == address.DistrictId.Value, cancellationToken))
                    .FirstOrDefault();
            }

            return new AddressDto
            {
                CityId = address.CityId,
                City = city?.Name,
                DistrictId = address.DistrictId,
                District = district?.Name,
                Country = ResolveCountryName(city),
                Street = address.Street,
                PostalCode = address.PostalCode,
                BuildingNumber = address.BuildingNumber,
                ApartmentNumber = address.ApartmentNumber,
                Coordinates = address.Coordinates is null
                    ? null
                    : new CoordinatesDto
                    {
                        Latitude = address.Coordinates.Latitude,
                        Longitude = address.Coordinates.Longitude
                    },
                FullAddress = address.GetFullAddress()
            };
        }

        /// <summary>
        /// Ülke adı: sistem tek ülke (TR) için kullandığından CountryCode → "Türkiye"
        /// eşlemesi yapılır; başka kodlar olduğu gibi döner.
        /// </summary>
        private static string? ResolveCountryName(City? city)
        {
            if (city is null)
                return null;

            return city.CountryCode == "TR" ? "Türkiye" : city.CountryCode;
        }
    }
}
