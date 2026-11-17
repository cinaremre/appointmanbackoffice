using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Interfaces;
using MediatR;
using BCryptNet = BCrypt.Net.BCrypt;

namespace AppointMan.Application.Features.Users.Commands.ChangePassword
{
    public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result<Guid>>
    {
        private readonly IRepository<User> _userRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ChangePasswordCommandHandler(
            IRepository<User> userRepository,
            IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<Guid>> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Kullanici kontrolu. Silinmis hesap uzerinde sifre degistirilemez.
                var users = await _userRepository.FindAsync(
                    u => u.Id == request.UserId && !u.IsDeleted, cancellationToken);

                var user = users.FirstOrDefault();
                if (user is null)
                    return Result<Guid>.Failure(Error.NotFound("Kullanici", request.UserId.ToString()));

                // 2. Mevcut sifre dogrulamasi
                if (!BCryptNet.Verify(request.CurrentPassword, user.PasswordHash))
                    return Result<Guid>.Failure(Error.Unauthorized("Mevcut sifre yanlis."));

                // 3. Yeni sifreyi hashle ve kaydet
                user.SetPasswordHash(BCryptNet.HashPassword(request.NewPassword));

                // 4. Degisiklikleri kaydet
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(user.Id);
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Sifre degistirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}