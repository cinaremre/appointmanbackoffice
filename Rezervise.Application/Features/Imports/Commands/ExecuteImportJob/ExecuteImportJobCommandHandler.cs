using System.Text.Json;
using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Helpers;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using Rezervise.Domain.ValueObjects;
using MediatR;

namespace Rezervise.Application.Features.Imports.Commands.ExecuteImportJob
{
    /// <summary>
    /// Aktarım işini işler:
    /// - Kolon eşleşmesinden telefon kolonu bulunur,
    /// - her satır için SourceRecordKey (normalize edilmiş telefon,
    ///   PhoneNumber value object ile) hesaplanır,
    /// - aynı anahtarın ilk görünümü Imported, tekrarları SkippedDuplicate,
    /// - geçersiz telefonlar Failed olarak kaydedilir,
    /// - is Completed durumuna geçer.
    ///
    /// BASITLESTIRME KARARI (ilk sürüm): müşteri (User) kaydı oluşturulmaz;
    /// satırlar yalnızca Imported/SkippedDuplicate olarak işaretlenir.
    /// Kullanıcı kayıtları register akışına bağlanacak; buradaki
    /// SourceRecordKey altyapısı ileride kullanıcı hesabı eşleştirmesinde
    /// deterministik anahtar olarak kullanılacak.
    /// </summary>
    public class ExecuteImportJobCommandHandler : IRequestHandler<ExecuteImportJobCommand, Result<Guid>>
    {
        private readonly IRepository<ImportJob> _importJobRepository;
        private readonly IRepository<ImportRowResult> _rowResultRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public ExecuteImportJobCommandHandler(
            IRepository<ImportJob> importJobRepository,
            IRepository<ImportRowResult> rowResultRepository,
            IRepository<Business> businessRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser)
        {
            _importJobRepository = importJobRepository;
            _rowResultRepository = rowResultRepository;
            _businessRepository = businessRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<Result<Guid>> Handle(ExecuteImportJobCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. İs kontrolü
                var job = await _importJobRepository.GetByIdAsync(request.ImportJobId, cancellationToken);
                if (job is null)
                    return Result<Guid>.Failure(Error.NotFound("Aktarım isi", request.ImportJobId.ToString()));

                // 2. Yetki kontrolü: işin işletmesi üzerinden JWT kimliği esas alınır.
                var business = await _businessRepository.GetByIdAsync(job.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("İşletme", job.BusinessId.ToString()));

                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<Guid>.Failure(Error.Unauthorized("Bu aktarım işini çalıştırma yetkiniz yok."));

                // 3. Tamamlanmış is tekrar çalıştırılamaz.
                if (job.Status == EnumImportStatus.Completed)
                    return Result<Guid>.Failure(Error.Conflict("Bu aktarım isi zaten tamamlanmış; tekrar çalıştırılamaz."));

                // 4. Kolon eşleşmesini çöz: {"0":"FullName","1":"Phone"}.
                //    Phone alani zorunludur; SourceRecordKey telefondan üretilir.
                var phoneIndex = TryResolvePhoneIndex(request.ColumnMapping, out var mappingError);
                if (phoneIndex is null)
                    return Result<Guid>.Failure(mappingError!);

                // 5. İsi işleme al ve eşleşmesi kaydet.
                job.SetColumnMapping(request.ColumnMapping);
                job.StartProcessing();
                await _importJobRepository.UpdateAsync(job, cancellationToken);

                // 6. Satırları isle. İlk satır kolon başlığıdır; veri 2. satırdan başlar.
                var rows = CsvParser.Parse(job.CsvContent);
                var importedKeys = new HashSet<string>();
                var importedCount = 0;
                var failedCount = 0;

                for (var i = 1; i < rows.Count; i++)
                {
                    var row = rows[i];
                    var rowNumber = i;

                    // 6a. Telefon değerini eşleşmeden al.
                    var phoneValue = phoneIndex.Value < row.Length ? row[phoneIndex.Value] : null;

                    if (string.IsNullOrWhiteSpace(phoneValue))
                    {
                        await AddRowAsync(job.Id, rowNumber, EnumImportRowStatus.Failed,
                            errorDetails: "Telefon kolonu boş.", cancellationToken: cancellationToken);
                        failedCount++;
                        continue;
                    }

                    // 6b. Normalize edilmiş telefon = deterministik eşleştirme anahtarı.
                    string recordKey;
                    try
                    {
                        recordKey = new PhoneNumber(phoneValue).Value;
                    }
                    catch (DomainException)
                    {
                        await AddRowAsync(job.Id, rowNumber, EnumImportRowStatus.Failed,
                            errorDetails: "Geçersiz telefon numarası.", cancellationToken: cancellationToken);
                        failedCount++;
                        continue;
                    }

                    // 6c. Mükerrer kontrolü: önce bu çalıştırmada, sonra önceki
                    //     işlerden aynı işletmeye ait Imported satırlar.
                    if (importedKeys.Contains(recordKey) || await WasAlreadyImportedAsync(job.BusinessId, recordKey, cancellationToken))
                    {
                        await AddRowAsync(job.Id, rowNumber, EnumImportRowStatus.SkippedDuplicate,
                            sourceRecordKey: recordKey, cancellationToken: cancellationToken);
                        continue;
                    }

                    // 6d. Yeni kayıt: ilk sürümde müşteri tablosuna yazılmaz,
                    //     yalnızca Imported olarak işaretlenir (register akışı bağlanacak).
                    await AddRowAsync(job.Id, rowNumber, EnumImportRowStatus.Imported,
                        sourceRecordKey: recordKey, cancellationToken: cancellationToken);
                    importedKeys.Add(recordKey);
                    importedCount++;
                }

                // 7. İsi tamamla ve sayaçları yaz.
                job.Complete(importedCount, failedCount);
                await _importJobRepository.UpdateAsync(job, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(job.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Aktarım isi çalıştırılırken bir hata oluştu.", ex.Message));
            }
        }

        /// <summary>
        /// Kolon eşleşme JSON'unu parse eder ve Phone alanının index'ini döner.
        /// </summary>
        private static int? TryResolvePhoneIndex(string columnMapping, out Error? error)
        {
            error = null;

            Dictionary<string, string>? mapping;
            try
            {
                mapping = JsonSerializer.Deserialize<Dictionary<string, string>>(columnMapping);
            }
            catch (JsonException)
            {
                error = Error.Conflict("Kolon eşleşmesi geçerli bir JSON değil.");
                return null;
            }

            if (mapping is null || mapping.Count == 0)
            {
                error = Error.Conflict("Kolon eşleşmesi boş olamaz.");
                return null;
            }

            foreach (var (key, value) in mapping)
            {
                if (string.Equals(value, "Phone", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(key, out var index))
                {
                    return index;
                }
            }

            error = Error.Conflict("Kolon eşleşmesinde Phone alani tanımlı değil.");
            return null;
        }

        /// <summary>Aynı işletmenin önceki aktarımlarında bu anahtar zaten aktarılmış mi?</summary>
        private async Task<bool> WasAlreadyImportedAsync(Guid businessId, string recordKey, CancellationToken cancellationToken)
        {
            var importedRows = await _rowResultRepository.FindAsync(
                r => r.SourceRecordKey == recordKey && r.Status == EnumImportRowStatus.Imported && !r.IsDeleted,
                cancellationToken);

            foreach (var row in importedRows)
            {
                var rowJob = await _importJobRepository.GetByIdAsync(row.ImportJobId, cancellationToken);
                if (rowJob is not null && rowJob.BusinessId == businessId)
                    return true;
            }

            return false;
        }

        private async Task AddRowAsync(
            Guid importJobId,
            int rowNumber,
            EnumImportRowStatus status,
            string? sourceRecordKey = null,
            string? errorDetails = null,
            CancellationToken cancellationToken = default)
        {
            var rowResult = new ImportRowResult(importJobId, rowNumber, status, sourceRecordKey, errorDetails);
            await _rowResultRepository.AddAsync(rowResult, cancellationToken);
        }
    }
}
