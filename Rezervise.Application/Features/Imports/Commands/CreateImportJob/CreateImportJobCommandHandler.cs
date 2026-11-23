using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Helpers;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Imports.Commands.CreateImportJob
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
                // 1. İşletme kontrolü
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("İşletme", request.BusinessId.ToString()));

                // 2. Yetki kontrolü: gövde OwnerId'si değil, JWT kimliği esas alınır.
                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<Guid>.Failure(Error.Unauthorized("Bu işletme için veri aktarımı başlatma yetkiniz yok."));

                // CreatedBy yalnızca kimliği doğrulanmış çağırıcıdan gelir.
                if (_currentUser.UserId is null)
                    return Result<Guid>.Failure(Error.Unauthorized());

                // 3. CSV'yi parse et ve satır sayısını hesapla.
                //    Satır kayıtları (ImportRowResult) bu aşamada oluşturulmaz;
                //    is kolon eşleşmesi onaylandıktan sonra (Execute) işlenir.
                //    SourceRecordKey (normalize edilmiş telefon) Execute adımında
                //    kolon eşleşmesine göre hesaplanır.
                //    İlk satır kolon başlığıdır; veri satırı sayımına dahil edilmez.
                var rows = CsvParser.Parse(request.CsvContent);
                var dataRowCount = Math.Max(0, rows.Count - 1);

                if (dataRowCount == 0)
                    return Result<Guid>.Failure(Error.BusinessRule("Aktarılacak veri satırı bulunamadı."));

                // 4. Aktarım işini oluştur: is onay (mapping) bekler.
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
                return Result<Guid>.Failure(Error.Internal("Aktarım isi oluşturulurken bir hata oluştu.", ex.Message));
            }
        }
    }
}
