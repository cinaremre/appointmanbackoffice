using System.Text.Json;
using AppointMan.Application.Common.Authorization;
using AppointMan.Application.Common.Helpers;
using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Interfaces;
using AppointMan.Domain.ValueObjects;
using MediatR;

namespace AppointMan.Application.Features.Imports.Commands.ExecuteImportJob
{
    /// <summary>
    /// Aktarim isini isler:
    /// - Kolon eslesmesinden telefon kolonu bulunur,
    /// - her satir icin SourceRecordKey (normalize edilmis telefon,
    ///   PhoneNumber value object ile) hesaplanir,
    /// - ayni anahtarin ilk gorunumu Imported, tekrarlari SkippedDuplicate,
    /// - gecersiz telefonlar Failed olarak kaydedilir,
    /// - is Completed durumuna gecer.
    ///
    /// BASITLESTIRME KARARI (ilk surum): musteri (User) kaydi olusturulmaz;
    /// satirlar yalnizca Imported/SkippedDuplicate olarak isaretlenir.
    /// Kullanici kayitlari register akisina baglanacak; buradaki
    /// SourceRecordKey altyapisi ileride kullanici hesabi eslestirmesinde
    /// deterministik anahtar olarak kullanilacak.
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
                // 1. Is kontrolu
                var job = await _importJobRepository.GetByIdAsync(request.ImportJobId, cancellationToken);
                if (job is null)
                    return Result<Guid>.Failure(Error.NotFound("Aktarim isi", request.ImportJobId.ToString()));

                // 2. Yetki kontrolu: isin isletmesi uzerinden JWT kimligi esas alinir.
                var business = await _businessRepository.GetByIdAsync(job.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("Isletme", job.BusinessId.ToString()));

                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<Guid>.Failure(Error.Unauthorized("Bu aktarim isini calistirma yetkiniz yok."));

                // 3. Tamamlanmis is tekrar calistirilamaz.
                if (job.Status == EnumImportStatus.Completed)
                    return Result<Guid>.Failure(Error.Conflict("Bu aktarim isi zaten tamamlanmis; tekrar calistirilamaz."));

                // 4. Kolon eslesmesini coz: {"0":"FullName","1":"Phone"}.
                //    Phone alani zorunludur; SourceRecordKey telefondan uretilir.
                var phoneIndex = TryResolvePhoneIndex(request.ColumnMapping, out var mappingError);
                if (phoneIndex is null)
                    return Result<Guid>.Failure(mappingError!);

                // 5. Isi isleme al ve eslesmesi kaydet.
                job.SetColumnMapping(request.ColumnMapping);
                job.StartProcessing();
                await _importJobRepository.UpdateAsync(job, cancellationToken);

                // 6. Satirlari isle. Ilk satir kolon basligidir; veri 2. satirdan baslar.
                var rows = CsvParser.Parse(job.CsvContent);
                var importedKeys = new HashSet<string>();
                var importedCount = 0;
                var failedCount = 0;

                for (var i = 1; i < rows.Count; i++)
                {
                    var row = rows[i];
                    var rowNumber = i;

                    // 6a. Telefon degerini eslesmeden al.
                    var phoneValue = phoneIndex.Value < row.Length ? row[phoneIndex.Value] : null;

                    if (string.IsNullOrWhiteSpace(phoneValue))
                    {
                        await AddRowAsync(job.Id, rowNumber, EnumImportRowStatus.Failed,
                            errorDetails: "Telefon kolonu bos.", cancellationToken: cancellationToken);
                        failedCount++;
                        continue;
                    }

                    // 6b. Normalize edilmis telefon = deterministik eslestirme anahtari.
                    string recordKey;
                    try
                    {
                        recordKey = new PhoneNumber(phoneValue).Value;
                    }
                    catch (DomainException)
                    {
                        await AddRowAsync(job.Id, rowNumber, EnumImportRowStatus.Failed,
                            errorDetails: "Gecersiz telefon numarasi.", cancellationToken: cancellationToken);
                        failedCount++;
                        continue;
                    }

                    // 6c. Mukerrer kontrolu: once bu calistirmada, sonra onceki
                    //     islerden ayni isletmeye ait Imported satirlar.
                    if (importedKeys.Contains(recordKey) || await WasAlreadyImportedAsync(job.BusinessId, recordKey, cancellationToken))
                    {
                        await AddRowAsync(job.Id, rowNumber, EnumImportRowStatus.SkippedDuplicate,
                            sourceRecordKey: recordKey, cancellationToken: cancellationToken);
                        continue;
                    }

                    // 6d. Yeni kayit: ilk surumde musteri tablosuna yazilmaz,
                    //     yalnizca Imported olarak isaretlenir (register akisi baglanacak).
                    await AddRowAsync(job.Id, rowNumber, EnumImportRowStatus.Imported,
                        sourceRecordKey: recordKey, cancellationToken: cancellationToken);
                    importedKeys.Add(recordKey);
                    importedCount++;
                }

                // 7. Isi tamamla ve sayaclari yaz.
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
                return Result<Guid>.Failure(Error.Internal("Aktarim isi calistirilirken bir hata olustu.", ex.Message));
            }
        }

        /// <summary>
        /// Kolon eslesme JSON'unu parse eder ve Phone alaninin index'ini doner.
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
                error = Error.Conflict("Kolon eslesmesi gecerli bir JSON degil.");
                return null;
            }

            if (mapping is null || mapping.Count == 0)
            {
                error = Error.Conflict("Kolon eslesmesi bos olamaz.");
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

            error = Error.Conflict("Kolon eslesmesinde Phone alani tanimli degil.");
            return null;
        }

        /// <summary>Ayni isletmenin onceki aktarimlarinda bu anahtar zaten aktarilmis mi?</summary>
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
