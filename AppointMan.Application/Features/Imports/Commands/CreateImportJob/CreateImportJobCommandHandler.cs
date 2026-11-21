using AppointMan.Application.Common.Authorization;
using AppointMan.Application.Common.Helpers;
using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Imports.Commands.CreateImportJob
{
    public class CreateImportJobCommandHandler : IRequestHandler<CreateImportJobCommand, Result<Guid>>
    {
        private readonly IRepository<ImportJob> _importJobRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public CreateImportJobCommandHandler(
            IRepository<ImportJob> importJobRepository,
            IRepository<Business> businessRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser)
        {
            _importJobRepository = importJobRepository;
            _businessRepository = businessRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<Result<Guid>> Handle(CreateImportJobCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Isletme kontrolu
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("Isletme", request.BusinessId.ToString()));

                // 2. Yetki kontrolu: govde OwnerId'si degil, JWT kimligi esas alinir.
                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<Guid>.Failure(Error.Unauthorized("Bu isletme icin veri aktarimi baslatma yetkiniz yok."));

                // CreatedBy yalnizca kimligi dogrulanmis cagiricidan gelir.
                if (_currentUser.UserId is null)
                    return Result<Guid>.Failure(Error.Unauthorized());

                // 3. CSV'yi parse et ve satir sayisini hesapla.
                //    Satir kayitlari (ImportRowResult) bu asamada olusturulmaz;
                //    is kolon eslesmesi onaylandiktan sonra (Execute) islenir.
                //    SourceRecordKey (normalize edilmis telefon) Execute adiminda
                //    kolon eslesmesine gore hesaplanir.
                //    Ilk satir kolon basligidir; veri satiri sayimina dahil edilmez.
                var rows = CsvParser.Parse(request.CsvContent);
                var dataRowCount = Math.Max(0, rows.Count - 1);

                if (dataRowCount == 0)
                    return Result<Guid>.Failure(Error.BusinessRule("Aktarilacak veri satiri bulunamadi."));

                // 4. Aktarim isini olustur: is onay (mapping) bekler.
                var job = new ImportJob(
                    request.BusinessId,
                    request.SourceSystem,
                    request.FileName,
                    request.CsvContent,
                    dataRowCount,
                    _currentUser.UserId.Value);

                job.MarkAwaitingMapping();

                // 5. Kaydet
                await _importJobRepository.AddAsync(job, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(job.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Aktarim isi olusturulurken bir hata olustu.", ex.Message));
            }
        }
    }
}
