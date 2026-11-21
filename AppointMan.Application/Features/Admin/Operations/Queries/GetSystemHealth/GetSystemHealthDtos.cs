namespace AppointMan.Application.Features.Admin.Operations.Queries.GetSystemHealth
{
    /// <summary>
    /// Sistem saglik raporu. Ilk surumde veritabani erisim testi yerine
    /// uygulamanin ayakta oldugu bilgisi + aktarim is sayilari raporlanir.
    /// </summary>
    public class SystemHealthDto
    {
        /// <summary>Ilk surumde sabit "Healthy"; sorgu calisiyorsa uygulama ayaktadir.</summary>
        public string Status { get; set; } = "Healthy";

        /// <summary>Henuz baslamamis isler (Uploaded/Mapping/Approved).</summary>
        public int PendingJobs { get; set; }

        /// <summary>Su anda islenen isler (Processing).</summary>
        public int RunningJobs { get; set; }

        /// <summary>Hata ile sonlanan isler (Failed).</summary>
        public int FailedJobs { get; set; }

        /// <summary>Saglik kontrolunun kapsam aciklamasi.</summary>
        public string Note { get; set; } = string.Empty;
    }
}
