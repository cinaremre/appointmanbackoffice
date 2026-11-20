using System;
using System.Collections.Generic;
using System.Text;

namespace AppointMan.Domain.Events
{
    public interface IDomainEvent
    {
        /// <summary>Olayın benzersiz kimligi; yan etki tekrarlarini engellemek icin kullanilir.</summary>
        Guid Id { get; }

        DateTime OccurredOn { get; }
    }
}
