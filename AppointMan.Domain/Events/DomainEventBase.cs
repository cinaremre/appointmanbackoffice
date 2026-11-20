using System;
using System.Collections.Generic;
using System.Text;

namespace AppointMan.Domain.Events
{
    public abstract class DomainEventBase : IDomainEvent
    {
        /// <summary>Olay kimligi; ayni olayin birden fazla yan etki uretmesini engeller.</summary>
        public Guid Id { get; } = Guid.NewGuid();

        public DateTime OccurredOn { get; private set; }

        protected DomainEventBase()
        {
            OccurredOn = DateTime.UtcNow;
        }
    }
}
