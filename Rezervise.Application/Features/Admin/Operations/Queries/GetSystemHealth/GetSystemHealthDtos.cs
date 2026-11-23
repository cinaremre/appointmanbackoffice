namespace Rezervise.Application.Features.Admin.Operations.Queries.GetSystemHealth
{
    /// <summary>
    /// Sistem sağlık raporu. İlk sürümde veritabanı erişim testi yerine
    /// uygulamanın ayakta öldüğü bilgisi + aktarım is sayıları raporlanır.
    /// </summary>
    public class SystemHealthDto
    {
        /// <summary>İlk sürümde sabit "Healthy"; sorgu çalışıyorsa uygulama ayaktadır.</summary>
        public string Status { get; set; } = "Healthy";

        /// <summary>Henüz başlamamış işler (Uploaded/Mapping/Approved).</summary>
        public int PendingJobs { get; set; }

        /// <summary>Su anda işlenen işler (Processing).</summary>
        public int RunningJobs { get; set; }

        /// <summary>Hata ile sonlanan işler (Failed).</summary>
        public int FailedJobs { get; set; }

        /// <summary>Sağlık kontrolünün kapsam açıklaması.</summary>
        public string Note { get; set; } = string.Empty;
    }
}
