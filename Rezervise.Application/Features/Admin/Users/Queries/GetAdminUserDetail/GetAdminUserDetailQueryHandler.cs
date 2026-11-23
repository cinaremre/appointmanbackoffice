using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Admin.Users.Queries.GetAdminUserDetail
{
    /// <summary>
    /// Kullanıcıyı; sahiplenilen işletmeler, bağlı çalışan kayıtları ve son
    /// giriş zamanıyla birlikte getirir. Kullanıcı yoksa NotFound döner.
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
                // 1. Kullanıcıyı yükle; yoksa NotFound döner.
                var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
                if (user is null)
                    return Result<AdminUserDetailDto>.Failure(Error.NotFoundById("Kullanıcı", request.UserId));

                // 2. İşletme üyelikleri: sahiplenilen işletmeler.
                var ownedBusinesses = await _businessRepository.FindAsync(
                    b => b.OwnerId == request.UserId,
                    cancellationToken);

                // 3. Çalışan üyelikleri: Employee entity'si opsionönel UserId
                //    bağlantısı taşır; bağlı kayıtlar buradan bulunur.
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
                    Error.Internal("Kullanıcı detayı getirilirken bir hata oluştu.", ex.Message));
            }
        }
    }
}
