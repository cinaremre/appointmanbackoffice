using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// İşletme stok ürünü. Fiyat alanları decimal para değerleridir
    /// (Money value object'i kadar katı kurallar gerektirmediğinden
    /// doğrudan decimal tutulur). Stok bakiyesi Product üzerinde
    /// tutulmaz; hareketlerin (StockMovement) toplamından hesaplanır.
    /// </summary>
    public class Product : BaseEntity
    {
        public Guid BusinessId { get; private set; }

        /// <summary>İşletme içinde benzersiz stok kodu.</summary>
        public string Sku { get; private set; }

        /// <summary>Barkod (opsiyonel, taratma için).</summary>
        public string? Barcode { get; private set; }

        public string Name { get; private set; }

        /// <summary>Ürün kategorisi (opsiyonel; kategori ağacı ilk süründe kullanılmaz).</summary>
        public Guid? CategoryId { get; private set; }

        /// <summary>Ölçü birimi (adet, kg, lt, ...). Varsayılan "adet".</summary>
        public string Unit { get; private set; }

        /// <summary>Satın alma fiyatı (giriş maliyeti).</summary>
        public decimal PurchasePrice { get; private set; }

        /// <summary>Satış fiyatı.</summary>
        public decimal SalePrice { get; private set; }

        /// <summary>Kritik stok seviyesi; altına düşüldüğünde ürün low-stock raporuna girer.</summary>
        public decimal MinimumStock { get; private set; }

        public bool IsActive { get; private set; }

        private Product() { } // EF Core için

        public Product(
            string sku,
            string name,
            Guid businessId,
            decimal purchasePrice,
            decimal salePrice,
            decimal minimumStock = 0m,
            string unit = "adet",
            string? barcode = null,
            Guid? categoryId = null)
            : this(Guid.NewGuid(), sku, name, businessId, purchasePrice, salePrice,
                  minimumStock, unit, barcode, categoryId)
        {
        }

        public Product(
            Guid id,
            string sku,
            string name,
            Guid businessId,
            decimal purchasePrice,
            decimal salePrice,
            decimal minimumStock = 0m,
            string unit = "adet",
            string? barcode = null,
            Guid? categoryId = null)
            : base(id)
        {
            if (businessId == Guid.Empty)
                throw new ArgumentException("İşletme ID boş olamaz.", nameof(businessId));

            Sku = NormalizeSku(sku);
            SetName(name);
            BusinessId = businessId;
            PurchasePrice = ValidatePrice(purchasePrice, nameof(purchasePrice));
            SalePrice = ValidatePrice(salePrice, nameof(salePrice));
            MinimumStock = ValidateQuantity(minimumStock, nameof(minimumStock));
            Unit = NormalizeUnit(unit);
            Barcode = NormalizeBarcode(barcode);
            CategoryId = categoryId == Guid.Empty ? null : categoryId;
            IsActive = true;
        }

        /// <summary>SKU benzersizliği işletme bazındadır; büyük/küçük harf ve boşluklar normalize edilir.</summary>
        private static string NormalizeSku(string sku)
        {
            if (string.IsNullOrWhiteSpace(sku))
                throw new DomainException("Ürün SKU'su boş olamaz.");

            var normalized = sku.Trim().ToUpperInvariant();

            if (normalized.Length > 50)
                throw new DomainException("Ürün SKU'su 50 karakterden uzun olamaz.");

            return normalized;
        }

        private static string NormalizeUnit(string unit)
        {
            if (string.IsNullOrWhiteSpace(unit))
                throw new DomainException("Ürün ölçü birimi boş olamaz.");

            var normalized = unit.Trim();

            if (normalized.Length > 20)
                throw new DomainException("Ürün ölçü birimi 20 karakterden uzun olamaz.");

            return normalized;
        }

        private static string? NormalizeBarcode(string? barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode))
                return null;

            var normalized = barcode.Trim();

            if (normalized.Length > 50)
                throw new DomainException("Barkod 50 karakterden uzun olamaz.");

            return normalized;
        }

        private static decimal ValidatePrice(decimal value, string paramName)
        {
            if (value < 0)
                throw new DomainException("Fiyat negatif olamaz.");

            if (value > 99999999.99m)
                throw new DomainException("Fiyat çok yüksek.");

            return Math.Round(value, 2);
        }

        private static decimal ValidateQuantity(decimal value, string paramName)
        {
            if (value < 0)
                throw new DomainException("Miktar negatif olamaz.");

            if (value > 99999999.99m)
                throw new DomainException("Miktar çok yüksek.");

            return value;
        }

        public void SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("Ürün adı boş olamaz.");

            if (name.Length < 2 || name.Length > 100)
                throw new DomainException("Ürün adı 2-100 karakter arasında olmalıdır.");

            Name = name.Trim();
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetBarcode(string? barcode)
        {
            Barcode = NormalizeBarcode(barcode);
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetPrices(decimal purchasePrice, decimal salePrice)
        {
            PurchasePrice = ValidatePrice(purchasePrice, nameof(purchasePrice));
            SalePrice = ValidatePrice(salePrice, nameof(salePrice));
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetMinimumStock(decimal minimumStock)
        {
            MinimumStock = ValidateQuantity(minimumStock, nameof(minimumStock));
            UpdatedAt = DateTime.UtcNow;
        }

        public void Activate()
        {
            if (IsActive)
                return;

            IsActive = true;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Deactivate()
        {
            if (!IsActive)
                return;

            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
