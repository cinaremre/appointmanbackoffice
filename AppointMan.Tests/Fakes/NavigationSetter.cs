using System.Reflection;
using AppointMan.Domain.Entities;

namespace AppointMan.Tests.Fakes;

/// <summary>
/// Birim testlerinde EF Core olmadigi icin navigation property'ler otomatik
/// yuklenmez. Yetki ve cakisma kontrolleri bu navigation'lara baktigindan,
/// senaryo kurarken bunlari elle baglamak gerekir.
/// </summary>
public static class NavigationSetter
{
    /// <summary>Randevunun isletme ve calisan navigation'larini baglar.</summary>
    /// <param name="appointment">Hedef randevu.</param>
    /// <param name="business">Baglanacak isletme.</param>
    /// <param name="employee">Baglanacak calisan.</param>
    public static void LinkAppointment(Appointment appointment, Business business, Employee employee)
    {
        Set(appointment, nameof(Appointment.Business), business);
        Set(appointment, nameof(Appointment.Employee), employee);
    }

    /// <summary>Randevunun hizmet navigation'ini baglar (cakisma penceresi icin gerekir).</summary>
    /// <param name="appointment">Hedef randevu.</param>
    /// <param name="service">Baglanacak hizmet.</param>
    public static void SetService(Appointment appointment, Service service)
        => Set(appointment, nameof(Appointment.Service), service);

    private static void Set(object target, string propertyName, object value)
    {
        var property = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"'{propertyName}' ozelligi bulunamadi.");

        property.SetValue(target, value);
    }
}
