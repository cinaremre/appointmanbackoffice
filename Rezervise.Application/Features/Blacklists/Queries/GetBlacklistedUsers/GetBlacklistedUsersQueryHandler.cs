using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Blacklists.Queries.GetBlacklistedUsers
{
    public class GetBlacklistedUsersQueryHandler
        : IRequestHandler<GetBlacklistedUsersQuery, Result<List<BlacklistEntryDto>>>
    {
        private readonly IRepository<BlacklistEntry> _blacklistRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly IRepository<User> _userRepository;

        public GetBlacklistedUsersQueryHandler(
            IRepository<BlacklistEntry> blacklistRepository,
            IRepository<Business> businessRepository,
            IRepository<User> userRepository)
        {
            _blacklistRepository = blacklistRepository;
            _businessRepository = businessRepository;
            _userRepository = userRepository;
        }

        public async Task<Result<List<BlacklistEntryDto>>> Handle(
            GetBlacklistedUsersQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                // 1. Isletme kontrolu
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<List<BlacklistEntryDto>>.Failure(
                        Error.NotFound("Isletme", request.BusinessId.ToString()));

                // 2. Yetki kontrolu
                if (business.OwnerId != request.RequesterId)
                    return Result<List<BlacklistEntryDto>>.Failure(
                        Error.Unauthorized("Bu islemi sadece isletme sahibi yapabilir."));

                // 3. Kara liste kayitlarini getir
                var entries = await _blacklistRepository.FindAsync(
                    b => b.BusinessId == request.BusinessId &&
                         (request.IncludeRemoved || !b.IsDeleted),
                    cancellationToken);

                // 4. Sayfalama + siralama
                var pagedEntries = entries
                    .OrderByDescending(b => b.BlacklistedAt)
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                // 5. User bilgilerini ayri cek
                var userIds = pagedEntries.Select(b => b.UserId).Distinct().ToList();
                var users = await _userRepository.FindAsync(
                    u => userIds.Contains(u.Id),
                    cancellationToken);

                var userDict = users.ToDictionary(u => u.Id);

                // 6. DTO'ya donustur
                var dtos = pagedEntries.Select(b =>
                {
                    userDict.TryGetValue(b.UserId, out var user);
                    return new BlacklistEntryDto
                    {
                        Id = b.Id,
                        UserId = b.UserId,
                        UserFullName = user?.GetFullName() ?? "Bilinmiyor",
                        UserEmail = user?.Email ?? string.Empty,
                        UserPhoneNumber = user?.PhoneNumber?.Value,
                        BusinessId = b.BusinessId,
                        BusinessName = business.Name,
                        Reason = b.Reason,
                        AppointmentCount = b.AppointmentCount,
                        BlacklistedAt = b.BlacklistedAt,
                        RemovedAt = b.RemovedAt,
                        RemovedBy = b.RemovedBy,
                        RemoveReason = b.RemoveReason,
                        IsActive = !b.IsDeleted
                    };
                }).ToList();

                return Result<List<BlacklistEntryDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                return Result<List<BlacklistEntryDto>>.Failure(
                    Error.Internal("Kara liste getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}