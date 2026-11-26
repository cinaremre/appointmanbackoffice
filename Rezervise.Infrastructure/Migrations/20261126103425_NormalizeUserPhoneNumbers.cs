using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rezervise.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeUserPhoneNumbers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_Users_PhoneNumber",
                table: "Users",
                newName: "UX_Users_PhoneNumber");

            // Index adi degisimi tek basina yetmiyor: eski PhoneNumber uretimi ulke kodu
            // yazilmamis girdilerde bastaki trunk sifirini koruyordu ("+9005551112233").
            // Ayni numara farkli bicimlerde saklandigi icin kayit sirasindaki tekillik
            // kontrolu ikinci kaydi yakalayamiyordu. Mevcut satirlar kanonik bicime
            // cevrilir; index zaten kolon uzerinde oldugu icin normalizasyon sonrasi
            // mukerrer kayit varsa bu adim hata verir (once
            // docs/scripts/user-phone-normalization.sql ile cakisma temizlenmelidir).
            migrationBuilder.Sql(NormalizePhoneNumbersSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "UX_Users_PhoneNumber",
                table: "Users",
                newName: "IX_Users_PhoneNumber");
        }

        /// <summary>
        /// Her satir icin en uzun eslesen ulke kodunu bulur ve ulusal numaranin basindaki
        /// trunk sifirlarini temizler. Domain tarafindaki PhoneNumber kanoniklestirmesiyle
        /// ayni sonucu uretir; boylece eski kayitlar da yeni kontrollere takilir.
        /// </summary>
        private const string NormalizePhoneNumbersSql = @"
;WITH Codes AS (
    SELECT Code
    FROM (VALUES
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
        ('+992'),('+993'),('+994'),('+995'),('+996'),('+998')
    ) AS code(Code)
),
Matched AS (
    SELECT u.Id,
           c.Code,
           SUBSTRING(u.PhoneNumber, LEN(c.Code) + 1, 20) AS NationalNumber,
           ROW_NUMBER() OVER (PARTITION BY u.Id ORDER BY LEN(c.Code) DESC) AS RowRank
    FROM Users u
    JOIN Codes c
      ON LEN(u.PhoneNumber) > LEN(c.Code)
     AND LEFT(u.PhoneNumber, LEN(c.Code)) = c.Code
    WHERE u.PhoneNumber LIKE '+%'
),
Canonical AS (
    SELECT Id,
           Code + STUFF(NationalNumber, 1, PATINDEX('%[^0]%', NationalNumber + 'X') - 1, '') AS NormalizedPhone
    FROM Matched
    WHERE RowRank = 1
      AND NationalNumber LIKE '%[1-9]%'
)
UPDATE u
SET PhoneNumber = c.NormalizedPhone
FROM Users u
JOIN Canonical c ON c.Id = u.Id
WHERE u.PhoneNumber <> c.NormalizedPhone;
";
    }
}
