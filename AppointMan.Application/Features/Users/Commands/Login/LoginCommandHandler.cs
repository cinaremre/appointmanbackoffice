using AppointMan.Application.Common.Diagnostics;
using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Application.Common.Security;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Interfaces;
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
                var users = await _userRepository.FindAsync(u => u.Email == request.Email && !u.IsDeleted, cancellationToken);
                var user = users.FirstOrDefault();

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
