using System;
using System.Collections.Generic;
using AppointMan.Domain.Exceptions;

namespace AppointMan.Domain.ValueObjects
{
    public class Address : IEquatable<Address>
    {
        public string Street { get; private set; }
        public string? BuildingNumber { get; private set; }
        public string? ApartmentNumber { get; private set; }
        public string District { get; private set; }
        public string City { get; private set; }
        public string? State { get; private set; }
        public string Country { get; private set; }
        public string PostalCode { get; private set; }
        public Coordinates? Coordinates { get; private set; }

        private Address() { } // EF Core

        public Address(
            string street,
            string district,
            string city,
            string country,
            string postalCode,
            string? buildingNumber = null,
            string? apartmentNumber = null,
            string? state = null,
            Coordinates? coordinates = null)
        {
            SetStreet(street);
            SetDistrict(district);
            SetCity(city);
            SetCountry(country);
            SetPostalCode(postalCode);

            BuildingNumber = buildingNumber;
            ApartmentNumber = apartmentNumber;
            State = state;
            Coordinates = coordinates;
        }

        public void SetStreet(string street)
        {
            if (string.IsNullOrWhiteSpace(street))
                throw new DomainException("Sokak adi bos olamaz.");

            if (street.Length > 100)
                throw new DomainException("Sokak adi 100 karakterden uzun olamaz.");

            Street = street.Trim();
        }

        public void SetDistrict(string district)
        {
            if (string.IsNullOrWhiteSpace(district))
                throw new DomainException("Ilce adi bos olamaz.");

            if (district.Length > 50)
                throw new DomainException("Ilce adi 50 karakterden uzun olamaz.");

            District = district.Trim();
        }

        public void SetCity(string city)
        {
            if (string.IsNullOrWhiteSpace(city))
                throw new DomainException("Sehir adi bos olamaz.");

            if (city.Length > 50)
                throw new DomainException("Sehir adi 50 karakterden uzun olamaz.");

            City = city.Trim();
        }

        public void SetCountry(string country)
        {
            if (string.IsNullOrWhiteSpace(country))
                throw new DomainException("Ulke adi bos olamaz.");

            if (country.Length > 50)
                throw new DomainException("Ulke adi 50 karakterden uzun olamaz.");

            Country = country.Trim();
        }

        public void SetPostalCode(string postalCode)
        {
            if (string.IsNullOrWhiteSpace(postalCode))
                throw new DomainException("Posta kodu bos olamaz.");

            if (postalCode.Length > 20)
                throw new DomainException("Posta kodu 20 karakterden uzun olamaz.");

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

            if (!string.IsNullOrWhiteSpace(District))
                parts.Add(District);

            if (!string.IsNullOrWhiteSpace(City))
                parts.Add(City);

            if (!string.IsNullOrWhiteSpace(State))
                parts.Add(State);

            if (!string.IsNullOrWhiteSpace(Country))
                parts.Add(Country);

            if (!string.IsNullOrWhiteSpace(PostalCode))
                parts.Add(PostalCode);

            return string.Join(", ", parts);
        }

        public double? GetLatitude()
        {
            return Coordinates?.Latitude;
        }

        public double? GetLongitude()
        {
            return Coordinates?.Longitude;
        }

        public double? DistanceTo(Address other)
        {
            if (other is null || Coordinates is null || other.Coordinates is null)
                return null;

            return Coordinates.DistanceTo(other.Coordinates);
        }

        // Immutable guncelleme metotlari
        public Address UpdateCoordinates(Coordinates? coordinates)
        {
            return new Address(
                Street, District, City, Country, PostalCode,
                BuildingNumber, ApartmentNumber, State,
                coordinates);
        }

        public Address UpdateBuildingInfo(string? buildingNumber, string? apartmentNumber)
        {
            return new Address(
                Street, District, City, Country, PostalCode,
                buildingNumber ?? BuildingNumber,
                apartmentNumber ?? ApartmentNumber,
                State, Coordinates);
        }

        public Address UpdateStreet(string street)
        {
            return new Address(
                street, District, City, Country, PostalCode,
                BuildingNumber, ApartmentNumber, State,
                Coordinates);
        }

        public Address UpdateDistrict(string district)
        {
            return new Address(
                Street, district, City, Country, PostalCode,
                BuildingNumber, ApartmentNumber, State,
                Coordinates);
        }

        public Address UpdateCity(string city)
        {
            return new Address(
                Street, District, city, Country, PostalCode,
                BuildingNumber, ApartmentNumber, State,
                Coordinates);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Address);
        }

        public bool Equals(Address? other)
        {
            if (other is null)
                return false;

            return Street == other.Street &&
                   BuildingNumber == other.BuildingNumber &&
                   ApartmentNumber == other.ApartmentNumber &&
                   District == other.District &&
                   City == other.City &&
                   State == other.State &&
                   Country == other.Country &&
                   PostalCode == other.PostalCode &&
                   Coordinates == other.Coordinates;
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(Street);
            hashCode.Add(BuildingNumber);
            hashCode.Add(ApartmentNumber);
            hashCode.Add(District);
            hashCode.Add(City);
            hashCode.Add(State);
            hashCode.Add(Country);
            hashCode.Add(PostalCode);
            hashCode.Add(Coordinates);

            return hashCode.ToHashCode();
        }
        public override string ToString()
        {
            return GetFullAddress();
        }
    }
}