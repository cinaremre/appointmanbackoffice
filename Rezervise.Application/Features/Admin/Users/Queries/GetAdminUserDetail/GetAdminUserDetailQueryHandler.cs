using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Admin.Users.Queries.GetAdminUserDetail
{
    /// <summary>
    /// Kullaniciyi; sahiplenilen isletmeler, bagli calisan kayitlari ve son
    /// giris zamaniyla birlikte getirir. Kullanici yoksa NotFound doner.
    /// </summary>
    public class GetAdminUserDetailQueryHandler : IRequestHandler<GetAdminUserDetailQuery, Result<AdminUserDetailDto>>
    {
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly IRepository<Employee> _employeeRepository;

        public GetAdminUserDetailQueryHandler(
            IRepository<User> userRepository,
            IRepository<Business> businessRepository,
            IRepository<Employee> employeeRepository)
        {
            _userRepository = userRepository;
            _businessRepository = businessRepository;
            _employeeRepository = employeeRepository;
        }

        public async Task<Result<AdminUserDetailDto>> Handle(
            GetAdminUserDetailQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                // 1. Kullaniciyi yukle; yoksa NotFound doner.
                var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
                if (user is null)
                    return Result<AdminUserDetailDto>.Failure(Error.NotFoundById("Kullanici", request.UserId));

                // 2. Isletme uyelikleri: sahiplenilen isletmeler.
                var ownedBusinesses = await _businessRepository.FindAsync(
                    b => b.OwnerId == request.UserId,
                    cancellationToken);

                // 3. Calisan uyelikleri: Employee entity'si opsiononel UserId
                //    baglantisi tasir; bagli kayitlar buradan bulunur.
                var employeePositions = await _employeeRepository.FindAsync(
                    e => e.UserId == request.UserId,
                    cancellationToken);

                var dto = new AdminUserDetailDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber?.Value,
                    Role = user.Role,
                    AccountStatus = user.AccountStatus,
                    IsLockedOut = user.AccountStatus == EnumAccountStatus.Locked || user.IsLockedOut,
                    LastLoginDate = user.LastLoginDate,
                    CreatedAt = user.CreatedAt,
                    OwnedBusinesses = ownedBusinesses
                        .OrderBy(b => b.Name)
                        .Select(b => new AdminUserBusinessDto
                        {
                            Id = b.Id,
                            Name = b.Name,
                            IsActive = b.IsActive
                        })
                        .ToList(),
                    EmployeePositions = employeePositions
                        .OrderBy(e => e.LastName)
                        .ThenBy(e => e.FirstName)
                        .Select(e => new AdminUserEmployeeDto
                        {
                            Id = e.Id,
                            BusinessId = e.BusinessId,
                            FirstName = e.FirstName,
                            LastName = e.LastName,
                            IsActive = e.IsActive
                        })
                        .ToList()
                };

                return Result<AdminUserDetailDto>.Success(dto);
            }
            catch (Exception ex)
            {
                return Result<AdminUserDetailDto>.Failure(
                    Error.Internal("Kullanici detayi getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}
