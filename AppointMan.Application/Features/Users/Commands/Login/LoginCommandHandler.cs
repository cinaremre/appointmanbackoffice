using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
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

        public LoginCommandHandler(
            IRepository<User> userRepository,
            IUnitOfWork unitOfWork,
            ITokenService tokenService)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _tokenService = tokenService;
        }

        public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var users = await _userRepository.FindAsync(u => u.Email == request.Email && !u.IsDeleted, cancellationToken);
                var user = users.FirstOrDefault();

                if (user is null || !BCryptNet.Verify(request.Password, user.PasswordHash))
                    return Result<LoginResponse>.Failure(Error.Unauthorized("Geçersiz email veya şifre."));

                var token = _tokenService.GenerateAccessToken(user);
                var refreshToken = _tokenService.GenerateRefreshToken();

                user.SetRefreshToken(refreshToken, DateTime.UtcNow.AddDays(7));
                user.UpdateLastLogin();
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var response = new LoginResponse
                {
                    AccessToken = token,
                    RefreshToken = refreshToken,
                    Expiration = DateTime.UtcNow.AddMinutes(60),
                    UserId = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role.ToString()
                };

                return Result<LoginResponse>.Success(response);
            }
            catch (Exception ex)
            {
                return Result<LoginResponse>.Failure(Error.Internal("Giriş yapılırken bir hata oluştu.", ex.Message));
            }
        }
    }
}