using System;
using System.Collections.Generic;
using System.Text;

namespace AppointMan.Domain.Events
{
    public interface IDomainEvent
    {
        DateTime OccurredOn { get; }
    }
}
