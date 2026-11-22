using Rezervise.Application.Common.Errors;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Models;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using Rezervise.Domain.ValueObjects;
using MediatR;

namespace Rezervise.Application.Features.Users.Commands.ResendPhoneOtp
{
    /// <summary>
    /// Telefon dogrulama kodunu yeniden gonderir. Yanit, numaranin sistemde
    /// kayitli olup olmadigini sizdirmamak icin her durumda basarili doner.
    /// </summary>
    public class ResendPhoneOtpCommand : IRequest<Result<bool>>
    {
        /// <summary>Kodun gonderilecegi telefon numarasi.</summary>
        public string PhoneNumber { get; set; }
    }

    /// <summary>
    /// Yeniden gonderim akisi: kullanici bulunamaz veya zaten dogrulanmissa
    /// sessizce basarili doner.
    /// </summary>
    public class ResendPhoneOtpCommandHandler : IRequestHandler<ResendPhoneOtpCommand, Result<bool>>
    {
        private readonly IRepository<User> _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IOtpCodeService _otpCodeService;
        private readonly ISmsCodeSender _smsCodeSender;
        private readonly IDateTimeService _dateTimeService;
        private readonly OtpOptions _otpOptions;

        public ResendPhoneOtpCommandHandler(
            IRepository<User> userRepository,
            IUnitOfWork unitOfWork,
            IOtpCodeService otpCodeService,
            ISmsCodeSender smsCodeSender,
            IDateTimeService dateTimeService,
            OtpOptions otpOptions)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _otpCodeService = otpCodeService;
            _smsCodeSender = smsCodeSender;
            _dateTimeService = dateTimeService;
            _otpOptions = otpOptions;
        }

        public async Task<Result<bool>> Handle(
            ResendPhoneOtpCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.PhoneNumber))
                    return Result<bool>.Success(true);

                var phone = new PhoneNumber(request.PhoneNumber);

                var users = await _userRepository.FindAsync(
                    u => u.PhoneNumber == phone && !u.IsDeleted,
                    cancellationToken);

                var user = users.FirstOrDefault();

                // Numara kayitli degil veya zaten dogrulanmis: bilgi sizdirilmaz.
                if (user is null || user.IsPhoneVerified)
                    return Result<bool>.Success(true);

                var utcNow = _dateTimeService.UtcNow;

                // Bekleme suresi dolmadiysa yeni kod uretilmez; yine basarili donulur.
                if (user.PhoneVerificationRequestedAt.HasValue &&
                    utcNow - user.PhoneVerificationRequestedAt.Value <
                    TimeSpan.FromSeconds(_otpOptions.ResendCooldownSeconds))
                    return Result<bool>.Success(true);

                var code = _otpCodeService.IssueCode(user, utcNow);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // SMS hatasi akisi bozmaz; kullanici tekrar isteyebilir.
                await _smsCodeSender.SendAsync(user.PhoneNumber.Value, code, cancellationToken);

                return Result<bool>.Success(true);
            }
            catch (DomainException ex)
            {
                return Result<bool>.Failure(Error.BusinessRule(ex.Key));
            }
            catch (Exception)
            {
                return Result<bool>.Failure(
                    Error.Internal("Dogrulama kodu yeniden gonderilirken bir hata olustu."));
            }
        }
    }
}
