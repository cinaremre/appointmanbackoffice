using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Reengagement.Queries.GetReengagementCandidates
{
    public class GetReengagementCandidatesQueryHandler
        : IRequestHandler<GetReengagementCandidatesQuery, Result<List<ReengagementCandidateDto>>>
    {
        private readonly IRepository<Business> _businessRepository;
        private readonly IRepository<BusinessReengagementRule> _ruleRepository;
        private readonly IRepository<CustomerServiceHistory> _historyRepository;
        private readonly IRepository<Appointment> _appointmentRepository;
        private readonly IRepository<ReengagementAttempt> _attemptRepository;
        private readonly IRepository<CustomerCommunicationPreference> _preferenceRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTimeService _dateTime;

        public GetReengagementCandidatesQueryHandler(
            IRepository<Business> businessRepository,
            IRepository<BusinessReengagementRule> ruleRepository,
            IRepository<CustomerServiceHistory> historyRepository,
            IRepository<Appointment> appointmentRepository,
            IRepository<ReengagementAttempt> attemptRepository,
            IRepository<CustomerCommunicationPreference> preferenceRepository,
            ICurrentUserService currentUser,
            IDateTimeService dateTime)
        {
            _businessRepository = businessRepository;
            _ruleRepository = ruleRepository;
            _historyRepository = historyRepository;
            _appointmentRepository = appointmentRepository;
            _attemptRepository = attemptRepository;
            _preferenceRepository = preferenceRepository;
            _currentUser = currentUser;
            _dateTime = dateTime;
        }

        public async Task<Result<List<ReengagementCandidateDto>>> Handle(
            GetReengagementCandidatesQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                // 1. Isletme kontrolu.
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<List<ReengagementCandidateDto>>.Failure(
                        Error.NotFound("Isletme", request.BusinessId.ToString()));

                // 2. Yetki: aday listesi isletme sahibi veya platform yoneticisine aittir.
                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<List<ReengagementCandidateDto>>.Failure(
                        Error.Unauthorized("Bu isletmenin geri cagirma adaylarini gorme yetkiniz yok."));

                var now = _dateTime.UtcNow;

                // 3. Kaynaklari topluca getir; dongu icinde sorgu yapilmaz.
                var rules = await _ruleRepository.FindAsync(
                    r => r.BusinessId == request.BusinessId && r.IsEnabled && !r.IsDeleted,
                    cancellationToken);

                var histories = await _historyRepository.FindAsync(
                    h => h.BusinessId == request.BusinessId && !h.IsDeleted,
                    cancellationToken);

                // Gelecek randevusu olan musteriler: aday olmasin.
                var futureAppointments = await _appointmentRepository.FindAsync(
                    a => a.BusinessId == request.BusinessId
                        && (a.Status == EnumAppointmentStatus.Pending || a.Status == EnumAppointmentStatus.Confirmed)
                        && a.AppointmentDate > now
                        && !a.IsDeleted,
                    cancellationToken);

                // Cooldown icindeki onceki denemeler.
                var openAttempts = await _attemptRepository.FindAsync(
                    a => a.BusinessId == request.BusinessId
                        && (a.Status == EnumReengagementStatus.Eligible || a.Status == EnumReengagementStatus.Notified)
                        && !a.IsDeleted,
                    cancellationToken);

                // Pazarlama izni kaydi bilinen musteriler (ilk surum: kanal fark etmeksizin).
                var marketingPreferences = await _preferenceRepository.FindAsync(
                    p => p.BusinessId == request.BusinessId
                        && p.Purpose == EnumConsentPurpose.Marketing
                        && !p.IsDeleted,
                    cancellationToken);

                var futureAppointmentKeys = futureAppointments
                    .Select(a => (a.UserId, a.ServiceId))
                    .ToHashSet();

                var consentKnownCustomers = marketingPreferences
                    .Select(p => p.CustomerId)
                    .ToHashSet();

                var candidates = new List<ReengagementCandidateDto>();
                var emitted = new HashSet<(Guid CustomerId, Guid ServiceId)>();

                foreach (var rule in rules)
                {
                    // Kuralin hizmet kapsami: null = tum hizmetler.
                    var ruleHistories = rule.ServiceId.HasValue
                        ? histories.Where(h => h.ServiceId == rule.ServiceId.Value)
                        : histories;

                    foreach (var history in ruleHistories)
                    {
                        // 4. Esik: son ziyaretten gecen gun, IntervalDays - LeadDays'e ulasmis olmali.
                        var daysSinceLastVisit = (now - history.LastCompletedAt).TotalDays;
                        if (daysSinceLastVisit < rule.IntervalDays - rule.LeadDays)
                            continue;

                        // 5. Bu hizmette gelecek randevusu varsa aday degil.
                        if (futureAppointmentKeys.Contains((history.CustomerId, history.ServiceId)))
                            continue;

                        // 6. Cooldown icinde acik (Eligible/Notified) deneme varsa aday degil.
                        var cooldownStart = now.AddDays(-rule.CooldownDays);
                        var inCooldown = openAttempts.Any(a =>
                            a.CustomerId == history.CustomerId
                            && a.ServiceId == history.ServiceId
                            && a.TriggeredAt >= cooldownStart);

                        if (inCooldown)
                            continue;

                        // 7. Ayni musteri+hizmet cifti birden fazla kuralla tekrar listelenmez;
                        //    isaretleme yalniz gercekten aday olustugunda yapilir.
                        if (!emitted.Add((history.CustomerId, history.ServiceId)))
                            continue;

                        candidates.Add(new ReengagementCandidateDto
                        {
                            CustomerId = history.CustomerId,
                            ServiceId = history.ServiceId,
                            RuleId = rule.Id,
                            LastCompletedAt = history.LastCompletedAt,
                            VisitCount = history.VisitCount,
                            MedianIntervalDays = history.MedianIntervalDays,
                            // Sablon degiskenleri gonderim aninda doldurulur.
                            SuggestedMessage = rule.TemplateMessage,
                            IsConsentKnown = consentKnownCustomers.Contains(history.CustomerId)
                        });
                    }
                }

                return Result<List<ReengagementCandidateDto>>.Success(candidates);
            }
            catch (Exception ex)
            {
                return Result<List<ReengagementCandidateDto>>.Failure(
                    Error.Internal("Geri cagirma adaylari listelenirken bir hata olustu.", ex.Message));
            }
        }
    }
}
