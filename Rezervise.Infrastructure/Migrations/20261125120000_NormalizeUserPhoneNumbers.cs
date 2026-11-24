using Rezervise.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rezervise.Infrastructure.Migrations
{
    /// <summary>
    /// Users.PhoneNumber değerlerini kanonik (+90XXXXXXXXXX) biçime getirir.
    /// Eski PhoneNumber üretimi ülke kodu yazılmamış girdilerde baştaki trunk sıfırını
    /// koruduğu için aynı numara "+9005551112233" ve "+905551112233" gibi farklı
    /// değerlerle saklanabiliyordu. Bu fark yüzünden kayıt sırasındaki tekillik
    /// kontrolü ikinci kaydı yakalayamıyordu. Index de aynı kolon üzerinde olduğu için
    /// veritabanı da ikinci kaydı engellemiyordu.
    ///
    /// Normalizasyon sırasında iki satır aynı değere inebileceği için unique index
    /// normalizasyondan SONRA oluşturulur. Önceden oluşmuş çift kayıt varsa index
    /// oluşturma adımı hata verir; bu durumda önce
    /// docs/scripts/user-phone-normalization.sql script'indeki çakışma raporu
    /// incelenip mükerrer kayıtlar temizlenmelidir.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261125120000_NormalizeUserPhoneNumbers")]
    public partial class NormalizeUserPhoneNumbers : Migration
    {
        /// <inheritdoç />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Eski index otomatik adla kurulmuştu; config artık UX_Users_PhoneNumber
            // adını kullanıyor (duplicate-key mesaj eşlemesi bu ada bağlı).
            migrationBuilder.DropIndex(
                name: "IX_Users_PhoneNumber",
                table: "Users");

            migrationBuilder.Sql(NormalizePhoneNumbersSql);

            migrationBuilder.CreateIndex(
                name: "UX_Users_PhoneNumber",
                table: "Users",
                column: "PhoneNumber",
                unique: true);
        }

        /// <inheritdoç />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Users_PhoneNumber",
                table: "Users");

            // Normalizasyon geri alınamaz; yalnızca index adı eski haline döner.
            migrationBuilder.CreateIndex(
                name: "IX_Users_PhoneNumber",
                table: "Users",
                column: "PhoneNumber",
                unique: true);
        }

        /// <summary>
        /// Her satır için en uzun eşleşen ülke kodunu bulur ve ulusal numaranın başındaki
        /// trunk sıfırlarını temizler. Domain tarafındaki PhoneNumber kanonikleştirmesiyle
        /// aynı sonucu üretir; böylece eski kayıtlar da yeni kontrollere takılır.
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
