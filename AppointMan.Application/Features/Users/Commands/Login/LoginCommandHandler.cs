using AppointMan.Application.Common.Diagnostics;
using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Application.Common.Security;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Interfaces;
using AppointMan.Domain.ValueObjects;
using MediatR;
using BCryptNet = BCrypt.Net.BCrypt;

namespace AppointMan.Application.Features.Users.Commands.Login
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResponse>>
    {
        private readonly IRepository<User> _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITokenService _tokenService;
        private readonly AppointManMetrics _metrics;

        public LoginCommandHandler(
            IRepository<User> userRepository,
            IUnitOfWork unitOfWork,
            ITokenService tokenService,
            AppointManMetrics metrics)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _tokenService = tokenService;
            _metrics = metrics;
        }

        public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var identifier = request.Email?.Trim() ?? string.Empty;

                // Tek alan iki formati da kabul eder: '@' iceriyorsa e-posta, aksi halde telefon.
                // Alan adi geriye donuk uyumluluk icin "Email" olarak korunmustur.
                User? user = null;

                if (identifier.Contains('@'))
                {
                    var usersByEmail = await _userRepository.FindAsync(
                        u => u.Email == identifier && !u.IsDeleted, cancellationToken);

                    user = usersByEmail.FirstOrDefault();
                }
                else if (!string.IsNullOrWhiteSpace(identifier))
                {
                    // PhoneNumber gecersiz girdide DomainException firlatir; bunu kimlik hatasina
                    // ceviriyoruz ki istemci her durumda ayni sabit cevabi alsin.
                    PhoneNumber? phoneNumber = null;

                    try
                    {
                        phoneNumber = new PhoneNumber(identifier);
                    }
                    catch (DomainException)
                    {
                        phoneNumber = null;
                    }

                    if (phoneNumber is not null)
                    {
                        // PhoneNumber value converter ile tek kolona map'li; esitlik SQL'e cevrilebilir.
                        var usersByPhone = await _userRepository.FindAsync(
                            u => u.PhoneNumber == phoneNumber && !u.IsDeleted, cancellationToken);

                        user = usersByPhone.FirstOrDefault();
                    }
                }

                // Unknown account and wrong password share the same error to prevent account enumeration.
                if (user is null)
                {
                    _metrics.RecordLoginAttempt("invalid_credentials");
                    return Result<LoginResponse>.Failure(Error.Unauthorized("Auth.InvalidCredentials"));
                }

                // Locked accounts are rejected before the password is even verified.
                if (user.IsLockedOut)
                {
                    _metrics.RecordLoginAttempt("locked");
                    return Result<LoginResponse>.Failure(Error.Unauthorized("Auth.AccountLocked"));
                }

                if (!BCryptNet.Verify(request.Password, user.PasswordHash))
                {
                    user.RegisterFailedLogin(AccountLockoutPolicy.MaxFailedAccessAttempts, AccountLockoutPolicy.LockoutDuration);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    _metrics.RecordLoginAttempt("invalid_credentials");

                    return Result<LoginResponse>.Failure(Error.Unauthorized("Auth.InvalidCredentials"));
                }

                // Sifre dogru ama adres dogrulanmamis: giris engellenir.
                // Kod ayirt edilebilir; istemci dogrulama ekranina yonlendirebilir.
                if (!user.IsEmailVerified)
                {
                    _metrics.RecordLoginAttempt("email_not_verified");
                    return Result<LoginResponse>.Failure(Error.Unauthorized("Auth.EmailNotVerified"));
                }

                var accessToken = _tokenService.GenerateAccessToken(user);
                var refreshToken = _tokenService.GenerateRefreshToken();

                user.RegisterSuccessfulLogin();

                // Only the hash is persisted: a database leak must not expose usable tokens.
                user.IssueRefreshToken(
                    RefreshTokenHasher.Hash(refreshToken),
                    _tokenService.GetRefreshTokenExpiryUtc(),
                    DateTime.UtcNow);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var response = new LoginResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    Expiration = _tokenService.GetAccessTokenExpiryUtc(),
                    UserId = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role.ToString(),
                    LanguageCode = string.IsNullOrWhiteSpace(user.LanguageCode)
                        ? User.DefaultLanguageCode
                        : user.LanguageCode
                };

                _metrics.RecordLoginAttempt("success");

                return Result<LoginResponse>.Success(response);
            }
            catch (Exception)
            {
                // Internal details stay out of the response; only the error key is surfaced.
                return Result<LoginResponse>.Failure(Error.Internal("Auth.LoginFailed"));
            }
        }
    }
}
