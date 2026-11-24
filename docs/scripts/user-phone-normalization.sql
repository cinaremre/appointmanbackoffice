/* ============================================================
   Kullanim amaci:
   "20261125120000_NormalizeUserPhoneNumbers" migration'i deploy edilmeden
   ONCE calistirilir. Script yalnizca SELECT yapar, veri degistirmez.

   Neden gerekli:
   Telefon numaralari eskiden kanonik olmayan bicimde saklanabiliyordu
   ("+9005551112233" gibi). Normalizasyon sonrasi iki kayit ayni degere
   inebilir ve Users tablosundaki unique index (UX_Users_PhoneNumber)
   olusturulamaz. Bu script once riskli satirlari raporlar.

   Kontrol sirasi:
     1) Degisecek satirlar
     2) Normalizasyon sonrasi CAKISAN aktif kayitlar  <-- bos olmali
     3) Kanonik hale gelemeyen / manuel inceleme gereken satirlar
   ============================================================ */

IF OBJECT_ID('tempdb..#Codes') IS NOT NULL DROP TABLE #Codes;
IF OBJECT_ID('tempdb..#PhoneReport') IS NOT NULL DROP TABLE #PhoneReport;

CREATE TABLE #Codes (Code varchar(5) NOT NULL);

INSERT INTO #Codes (Code) VALUES
    ('+1'),('+7'),('+20'),('+27'),('+30'),('+31'),('+32'),('+33'),('+34'),('+36'),
    ('+39'),('+40'),('+41'),('+43'),('+44'),('+45'),('+46'),('+47'),('+48'),('+49'),
    ('+51'),('+52'),('+53'),('+54'),('+55'),('+56'),('+57'),('+58'),('+60'),('+61'),
    ('+62'),('+63'),('+64'),('+65'),('+66'),('+81'),('+82'),('+84'),('+86'),('+90'),
    ('+91'),('+92'),('+93'),('+94'),('+95'),('+98'),
    ('+212'),('+213'),('+216'),('+218'),('+220'),('+221'),('+234'),
    ('+351'),('+352'),('+353'),('+356'),('+357'),('+358'),('+359'),
    ('+370'),('+371'),('+372'),('+380'),('+381'),('+385'),('+386'),('+387'),
    ('+420'),('+421'),('+423'),
    ('+961'),('+962'),('+963'),('+964'),('+965'),('+966'),('+967'),('+968'),
    ('+970'),('+971'),('+972'),('+973'),('+974'),('+975'),('+976'),('+977'),
    ('+992'),('+993'),('+994'),('+995'),('+996'),('+998');

-- Her kullanici icin en uzun eslesen ulke kodu + trunk sifirlari temizlenmis hali.
SELECT u.Id,
       u.PhoneNumber,
       u.IsDeleted,
       u.IsPhoneVerified,
       u.Email,
       n.NormalizedPhone
INTO #PhoneReport
FROM Users u
OUTER APPLY (
    SELECT TOP 1
           c.Code + STUFF(x.NationalNumber, 1, PATINDEX('%[^0]%', x.NationalNumber + 'X') - 1, '') AS NormalizedPhone
    FROM #Codes c
    CROSS APPLY (SELECT SUBSTRING(u.PhoneNumber, LEN(c.Code) + 1, 20) AS NationalNumber) x
    WHERE u.PhoneNumber LIKE '+%'
      AND LEN(u.PhoneNumber) > LEN(c.Code)
      AND LEFT(u.PhoneNumber, LEN(c.Code)) = c.Code
      AND x.NationalNumber LIKE '%[1-9]%'
    ORDER BY LEN(c.Code) DESC
) n;

/* ---------- 1) Degisecek satirlar ---------- */
SELECT Id,
       PhoneNumber  AS EskiTelefon,
       NormalizedPhone AS YeniTelefon
FROM #PhoneReport
WHERE NormalizedPhone IS NOT NULL
  AND PhoneNumber <> NormalizedPhone
ORDER BY Id;

/* ---------- 2) CAKISMA RAPORU: normalizasyon sonrasi ayni numarayi paylasan aktif kayitlar ----------
   Bu sorgu BOS donmelidir. Aksi halde migration'in index adimi hata verir;
   once mukerrer kayitlar (genellikle OTP onayi tamamlanmamis olan) manuel temizlenmeli. */
SELECT COALESCE(NormalizedPhone, PhoneNumber) AS Telefon,
       COUNT(*)                             AS KayitSayisi,
       STRING_AGG(CAST(Id AS varchar(36)), ', ') AS KullaniciIdleri
FROM #PhoneReport
WHERE IsDeleted = 0
GROUP BY COALESCE(NormalizedPhone, PhoneNumber)
HAVING COUNT(*) > 1
ORDER BY KayitSayisi DESC;

/* ---------- 3) Manuel inceleme gereken satirlar ----------
   Bilinen bir ulke koduyla eslesmeyen ya da ulke kodu iki kez yazilmis kayitlar. */
SELECT Id, PhoneNumber, IsDeleted, IsPhoneVerified, Email
FROM #PhoneReport
WHERE IsDeleted = 0
  AND (NormalizedPhone IS NULL OR PhoneNumber LIKE '+9090%')
ORDER BY Id;

DROP TABLE #PhoneReport;
DROP TABLE #Codes;
