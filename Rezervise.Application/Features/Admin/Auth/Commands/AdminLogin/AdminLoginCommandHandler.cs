using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Admin.Auth.Commands.AdminLogin
{
    /// <summary>
    /// Backoffice giriş isteği. Yanıt yalnızca kimlik/parola doğru VE
    /// kullanıcı backoffice rollerinden birine sahipse üretilir.
    /// </summary>
    public class AdminLoginCommand : IRequest<Result<AdminLoginResponse>>
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>Başarılı giriş yanıtı.</summary>
    public class AdminLoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime Expiration { get; set; }
    }

    /// <summary>
    /// Backoffice girişi. Müşteri/işletme sahibi hesapları bilinçli olarak
    /// reddedilir: backoffice token'i yalnızca yönetici rolleri için üretilir.
    /// Kimlik bilgisi hatalarında tek sabit mesaj dönerulur (hesap sayısı
    /// sızdırılmaz).
    /// </summary>
    public class AdminLoginCommandHandler : IRequestHandler<AdminLoginCommand, Result<AdminLoginResponse>>
    {
        /// <summary>
        /// Backoffice'e girebilen roller (doküman 5.2). BusinessOwner da girer;
        /// erişebileceği uç'lar yalnızca kendi işletmesi kapsamındadır.
        /// </summary>
        private static readonly string[] BackOfficeRoles =
        {
            "SuperAdmin", "Admin", "PlatformAdmin", "FinanceAdmin",
            "SupportAgent", "MarketingAdmin", "ContentManager", "Auditor",
            "ReadOnlyAnalyst", "BusinessOwner"
        };

        private readonly IRepository<User> _userRepository;
        private readonly ITokenService _tokenService;

        public AdminLoginCommandHandler(IRepository<User> userRepository, ITokenService tokenService)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
        }

        public async Task<Result<AdminLoginResponse>> Handle(AdminLoginCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var email = request.Email?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(request.Password))
                    return Result<AdminLoginResponse>.Failure(Error.Validation("E-posta ve şifre zorunludur."));

                var users = await _userRepository.FindAsync(
                    u => u.Email == email && !u.IsDeleted,
                    cancellationToken);

                var user = users.FirstOrDefault();

                // Bilinmeyen hesap ve yanlış parola aynı sabit cevabı alır.
                if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                    return Result<AdminLoginResponse>.Failure(Error.Unauthorized("Auth.InvalidCredentials"));

                if (user.IsLockedOut)
                    return Result<AdminLoginResponse>.Failure(Error.Unauthorized("Auth.AccountLocked"));

                // Rol kontrolü: backoffice yalnızca yönetici rollerine açıktır.
                if (!BackOfficeRoles.Contains(user.Role.ToString()))
                    return Result<AdminLoginResponse>.Failure(Error.Unauthorized("Auth.InvalidCredentials"));

                var accessToken = _tokenService.GenerateAccessToken(user);

                return Result<AdminLoginResponse>.Success(new AdminLoginResponse
                {
                    AccessToken = accessToken,
                    UserId = user.Id,
                    Email = user.Email,
                    Role = user.Role.ToString(),
                    Expiration = _tokenService.GetAccessTokenExpiryUtc()
                });
            }
            catch (Exception)
            {
                return Result<AdminLoginResponse>.Failure(Error.Internal("Auth.LoginFailed"));
            }
        }
    }
}
