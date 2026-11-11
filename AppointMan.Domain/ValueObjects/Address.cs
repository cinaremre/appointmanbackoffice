using AppointMan.Domain.Exceptions;

namespace AppointMan.Domain.ValueObjects
{
    public class Address : IEquatable<Address>
    {
        public int CityId { get; private set; }
        public int? DistrictId { get; private set; }
        public int? LocalityId { get; private set; }
        public int? NeighborhoodId { get; private set; }
        public string Street { get; private set; }
        public string? BuildingNumber { get; private set; }
        public string? ApartmentNumber { get; private set; }
        public string PostalCode { get; private set; }
        public Coordinates? Coordinates { get; private set; }

        private Address() { } // EF Core

        public Address(
            int cityId,
            int? districtId,
            string street,
            string postalCode,
            string? buildingNumber = null,
            string? apartmentNumber = null,
            Coordinates? coordinates = null,
            int? localityId = null,
            int? neighborhoodId = null)
        {
            if (cityId <= 0)
                throw new DomainException("Address.CityRequired");

            if (districtId.HasValue && districtId.Value <= 0)
                throw new DomainException("Address.DistrictIdInvalid");

            if (localityId.HasValue && localityId.Value <= 0)
                throw new DomainException("Address.LocalityIdInvalid");

            if (neighborhoodId.HasValue && neighborhoodId.Value <= 0)
                throw new DomainException("Address.NeighborhoodIdInvalid");

            if (string.IsNullOrWhiteSpace(street))
                throw new DomainException("Address.StreetRequired");

            if (street.Length > 100)
                throw new DomainException("Address.StreetMaxLength");

            if (string.IsNullOrWhiteSpace(postalCode))
                throw new DomainException("Address.PostalCodeRequired");

            if (postalCode.Length > 20)
                throw new DomainException("Address.PostalCodeMaxLength");

            CityId = cityId;
            DistrictId = districtId;
            LocalityId = localityId;
            NeighborhoodId = neighborhoodId;
            Street = street.Trim();
            PostalCode = postalCode.Trim();
            BuildingNumber = buildingNumber;
            ApartmentNumber = apartmentNumber;
            Coordinates = coordinates;
        }

        public void SetCity(int cityId)
        {
            if (cityId <= 0)
                throw new DomainException("Address.CityIdInvalid");

            CityId = cityId;
        }

        public void SetDistrict(int? districtId)
        {
            if (districtId.HasValue && districtId.Value <= 0)
                throw new DomainException("Address.DistrictIdInvalid");

            DistrictId = districtId;
        }

        public void SetLocality(int? localityId)
        {
            if (localityId.HasValue && localityId.Value <= 0)
                throw new DomainException("Address.LocalityIdInvalid");

            LocalityId = localityId;
        }

        public void SetNeighborhood(int? neighborhoodId)
        {
            if (neighborhoodId.HasValue && neighborhoodId.Value <= 0)
                throw new DomainException("Address.NeighborhoodIdInvalid");

            NeighborhoodId = neighborhoodId;
        }

        public void SetStreet(string street)
        {
            if (string.IsNullOrWhiteSpace(street))
                throw new DomainException("Address.StreetRequired");

            if (street.Length > 100)
                throw new DomainException("Address.StreetMaxLength");

            Street = street.Trim();
        }

        public void SetBuildingInfo(string? buildingNumber, string? apartmentNumber)
        {
            BuildingNumber = buildingNumber;
            ApartmentNumber = apartmentNumber;
        }

        public void SetPostalCode(string postalCode)
        {
            if (string.IsNullOrWhiteSpace(postalCode))
                throw new DomainException("Address.PostalCodeRequired");

            if (postalCode.Length > 20)
                throw new DomainException("Address.PostalCodeMaxLength");

            PostalCode = postalCode.Trim();
        }

        public void SetCoordinates(Coordinates? coordinates)
        {
            Coordinates = coordinates;
        }

        public string GetFullAddress()
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(Street))
                parts.Add(Street);

            if (!string.IsNullOrWhiteSpace(BuildingNumber))
                parts.Add($"No: {BuildingNumber}");

            if (!string.IsNullOrWhiteSpace(ApartmentNumber))
                parts.Add($"D: {ApartmentNumber}");

            if (!string.IsNullOrWhiteSpace(PostalCode))
                parts.Add(PostalCode);

            return string.Join(", ", parts);
        }

        public bool Equals(Address? other)
        {
            if (other is null) return false;

            return CityId == other.CityId &&
                   DistrictId == other.DistrictId &&
                   LocalityId == other.LocalityId &&
                   NeighborhoodId == other.NeighborhoodId &&
                   Street == other.Street &&
                   BuildingNumber == other.BuildingNumber &&
                   ApartmentNumber == other.ApartmentNumber &&
                   PostalCode == other.PostalCode &&
                   Equals(Coordinates, other.Coordinates);
        }

        public override bool Equals(object? obj) => Equals(obj as Address);

        public override int GetHashCode()
        {
            // HashCode.Combine en fazla 8 arguman aldigi icin
            // alan sayisi arttiginda HashCode ornegi kullanilir.
            var hash = new HashCode();
            hash.Add(CityId);
            hash.Add(DistrictId);
            hash.Add(LocalityId);
            hash.Add(NeighborhoodId);
            hash.Add(Street);
            hash.Add(BuildingNumber);
            hash.Add(ApartmentNumber);
            hash.Add(PostalCode);
            hash.Add(Coordinates);
            return hash.ToHashCode();
        }
    }
}