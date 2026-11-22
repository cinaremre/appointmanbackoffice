using Rezervise.Domain.Exceptions;
using System;

namespace Rezervise.Domain.ValueObjects
{
    public class Coordinates : IEquatable<Coordinates>
    {
        public double? Latitude { get; private set; }
        public double? Longitude { get; private set; }

        private Coordinates() { } // EF Core

        public Coordinates(double? latitude, double? longitude)
        {
            if (latitude.HasValue && (latitude.Value < -90 || latitude.Value > 90))
                throw new DomainException("Coordinates.LatitudeOutOfRange");

            if (longitude.HasValue && (longitude.Value < -180 || longitude.Value > 180))
                throw new DomainException("Coordinates.LongitudeOutOfRange");

            Latitude = latitude.HasValue ? Math.Round(latitude.Value, 6) : null;
            Longitude = longitude.HasValue ? Math.Round(longitude.Value, 6) : null;
        }

        public double? DistanceTo(Coordinates other)
        {
            if (other is null || !Latitude.HasValue || !Longitude.HasValue ||
                !other.Latitude.HasValue || !other.Longitude.HasValue)
                return null;

            // Haversine formulu ile iki nokta arasi mesafe (km)
            const double earthRadius = 6371;

            var lat1 = Latitude.Value * Math.PI / 180;
            var lat2 = other.Latitude.Value * Math.PI / 180;
            var lon1 = Longitude.Value * Math.PI / 180;
            var lon2 = other.Longitude.Value * Math.PI / 180;

            var dLat = lat2 - lat1;
            var dLon = lon2 - lon1;

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(lat1) * Math.Cos(lat2) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return earthRadius * c;
        }

        public double? DistanceInMetersTo(Coordinates other)
        {
            var distanceKm = DistanceTo(other);
            return distanceKm.HasValue ? distanceKm.Value * 1000 : null;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Coordinates);
        }

        public bool Equals(Coordinates? other)
        {
            if (other is null)
                return false;

            return Latitude == other.Latitude && Longitude == other.Longitude;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Latitude, Longitude);
        }

        public override string ToString()
        {
            if (!Latitude.HasValue || !Longitude.HasValue)
                return "Koordinat yok";

            return $"{Latitude.Value}, {Longitude.Value}";
        }
    }
}