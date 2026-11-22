using System;
using System.Collections.Generic;
using System.Text;

namespace Rezervise.Domain.Events
{
    public interface IDomainEvent
    {
        /// <summary>Olayin benzersiz kimligi; yan etki tekrarlarini engellemek icin kullanilir.</summary>
        Guid Id { get; }

        DateTime OccurredOn { get; }
    }
}
