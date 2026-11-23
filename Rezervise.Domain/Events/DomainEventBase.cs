using System;
using System.Collections.Generic;
using System.Text;

namespace Rezervise.Domain.Events
{
    public abstract class DomainEventBase : IDomainEvent
    {
        /// <summary>Olay kimliği; aynı olayın birden fazla yan etki üretmesini engeller.</summary>
        public Guid Id { get; } = Guid.NewGuid();

        public DateTime OccurredOn { get; private set; }

        protected DomainEventBase()
        {
            OccurredOn = DateTime.UtcNow;
        }
    }
}
