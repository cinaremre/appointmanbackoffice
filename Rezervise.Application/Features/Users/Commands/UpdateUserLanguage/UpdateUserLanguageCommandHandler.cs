using Rezervise.Application.Common.Errors;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Users.Commands.UpdateUserLanguage
{
    public class UpdateUserLanguageCommandHandler : IRequestHandler<UpdateUserLanguageCommand, Result<Guid>>
    {
        private readonly IRepository<User> _userRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateUserLanguageCommandHandler(
            IRepository<User> userRepository,
            IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<Guid>> Handle(UpdateUserLanguageCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Kullanici kontrolu. Silinmis hesap uzerinde islem yapilamaz.
                var users = await _userRepository.FindAsync(
                    u => u.Id == request.UserId && !u.IsDeleted, cancellationToken);

                var user = users.FirstOrDefault();
                if (user is null)
                    return Result<Guid>.Failure(Error.NotFound("User.NotFound", request.UserId.ToString()));

                // 2. Domain kurali ile dil kodunu dogrula ve uygula
                user.SetLanguageCode(request.LanguageCode);

                // 3. Kaydet
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(user.Id);
            }
            catch (ConcurrencyConflictException)
            {
                // Rowversion uyusmazligi: baska bir islem ayni kullaniciyi guncelledi.
                return Result<Guid>.Failure(ConcurrencyErrors.ConcurrencyConflict());
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Key));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("User.LanguageUpdateFailed", ex.Message));
            }
        }
    }
}
