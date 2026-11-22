using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// Isletme stok urunu. Fiyat alanlari decimal para degerleridir
    /// (Money value object'i kadar kati kurallar gerektirmediginden
    /// dogrudan decimal tutulur). Stok bakiyesi Product uzerinde
    /// tutulmaz; hareketlerin (StockMovement) toplamindan hesaplanir.
    /// </summary>
    public class Product : BaseEntity
    {
        public Guid BusinessId { get; private set; }

        /// <summary>Isletme icinde benzersiz stok kodu.</summary>
        public string Sku { get; private set; }

        /// <summary>Barkod (opsiyonel, taratma icin).</summary>
        public string? Barcode { get; private set; }

        public string Name { get; private set; }

        /// <summary>Urun kategorisi (opsiyonel; kategori agaci ilk surunde kullanilmaz).</summary>
        public Guid? CategoryId { get; private set; }

        /// <summary>Olcu birimi (adet, kg, lt, ...). Varsayilan "adet".</summary>
        public string Unit { get; private set; }

        /// <summary>Satin alma fiyati (giris maliyeti).</summary>
        public decimal PurchasePrice { get; private set; }

        /// <summary>Satis fiyati.</summary>
        public decimal SalePrice { get; private set; }

        /// <summary>Kritik stok seviyesi; altina dusuldugunde urun low-stock raporuna girer.</summary>
        public decimal MinimumStock { get; private set; }

        public bool IsActive { get; private set; }

        private Product() { } // EF Core icin

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
                throw new ArgumentException("Isletme ID bos olamaz.", nameof(businessId));

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

        /// <summary>SKU benzersizligi isletme bazindadir; buyuk/kucuk harf ve bosluklar normalize edilir.</summary>
        private static string NormalizeSku(string sku)
        {
            if (string.IsNullOrWhiteSpace(sku))
                throw new DomainException("Urun SKU'su bos olamaz.");

            var normalized = sku.Trim().ToUpperInvariant();

            if (normalized.Length > 50)
                throw new DomainException("Urun SKU'su 50 karakterden uzun olamaz.");

            return normalized;
        }

        private static string NormalizeUnit(string unit)
        {
            if (string.IsNullOrWhiteSpace(unit))
                throw new DomainException("Urun olcu birimi bos olamaz.");

            var normalized = unit.Trim();

            if (normalized.Length > 20)
                throw new DomainException("Urun olcu birimi 20 karakterden uzun olamaz.");

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
                throw new DomainException("Fiyat cok yuksek.");

            return Math.Round(value, 2);
        }

        private static decimal ValidateQuantity(decimal value, string paramName)
        {
            if (value < 0)
                throw new DomainException("Miktar negatif olamaz.");

            if (value > 99999999.99m)
                throw new DomainException("Miktar cok yuksek.");

            return value;
        }

        public void SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("Urun adi bos olamaz.");

            if (name.Length < 2 || name.Length > 100)
                throw new DomainException("Urun adi 2-100 karakter arasinda olmalidir.");

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
