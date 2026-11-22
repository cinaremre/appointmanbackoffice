using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    public class BusinessServiceCategory : BaseEntity
    {
        public Guid BusinessId { get; private set; }
        public Business Business { get; private set; }

        public Guid ServiceCategoryId { get; private set; }
        public ServiceCategory ServiceCategory { get; private set; }

        private BusinessServiceCategory() { }

        public BusinessServiceCategory(Guid businessId, Guid serviceCategoryId)
    : this(Guid.NewGuid(), businessId, serviceCategoryId)
        {
        }
        public BusinessServiceCategory(Guid id, Guid businessId, Guid serviceCategoryId)
    : base(id)
        {
            if (businessId == Guid.Empty)
                throw new ArgumentException("Isletme ID bos olamaz.", nameof(businessId));

            if (serviceCategoryId == Guid.Empty)
                throw new ArgumentException("Kategori ID bos olamaz.", nameof(serviceCategoryId));

            BusinessId = businessId;
            ServiceCategoryId = serviceCategoryId;
        }
    }
}