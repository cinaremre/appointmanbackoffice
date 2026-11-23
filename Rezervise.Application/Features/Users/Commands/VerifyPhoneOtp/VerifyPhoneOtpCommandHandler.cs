using Rezervise.Application.Common.Errors;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using Rezervise.Domain.ValueObjects;
using MediatR;

namespace Rezervise.Application.Features.Users.Commands.VerifyPhoneOtp
{
    /// <summary>
    /// Telefon OTP kodunu doğrular. Kod tek kullanımlıktır ve başarılı doğrulamada
    /// temizlenir; başarısız denemeler sayılır.
    /// </summary>
    public class VerifyPhoneOtpCommandHandler : IRequestHandler<VerifyPhoneOtpCommand, Result<Guid>>
    {
        private readonly IRepository<User> _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IOtpCodeService _otpCodeService;
        private readonly IDateTimeService _dateTimeService;

        public VerifyPhoneOtpCommandHandler(
            IRepository<User> userRepository,
            IUnitOfWork unitOfWork,
            IOtpCodeService otpCodeService,
            IDateTimeService dateTimeService)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _otpCodeService = otpCodeService;
            _dateTimeService = dateTimeService;
        }

        public async Task<Result<Guid>> Handle(
            VerifyPhoneOtpCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.PhoneNumber) || string.IsNullOrWhiteSpace(request.Code))
                    return Result<Guid>.Failure(Error.BusinessRule("Otp.CodeRequired"));

                var phone = new PhoneNumber(request.PhoneNumber);

                var users = await _userRepository.FindAsync(
                    u => u.PhoneNumber == phone && !u.IsDeleted,
                    cancellationToken);

                var user = users.FirstOrDefault();
                if (user is null)
                    return Result<Guid>.Failure(Error.NotFound("Kullanıcı", request.PhoneNumber));

                var utcNow = _dateTimeService.UtcNow;
                var (success, errorKey) = _otpCodeService.VerifyCode(user, request.Code, utcNow);

                // Başarısız denemede sayaç da güncellenir; bu yüzden her durumda kaydedilir.
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                if (!success)
                    return Result<Guid>.Failure(Error.BusinessRule(errorKey ?? "Otp.InvalidCode"));

                return Result<Guid>.Success(user.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Key));
            }
            catch (Exception)
            {
                return Result<Guid>.Failure(
                    Error.Internal("Telefon doğrulaması sırasında bir hata oluştu."));
            }
        }
    }
}
