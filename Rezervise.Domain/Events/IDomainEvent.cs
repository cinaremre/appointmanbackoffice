using System;
using System.Collections.Generic;
using System.Text;

namespace Rezervise.Domain.Events
{
    public interface IDomainEvent
    {
        /// <summary>Olayın benzersiz kimliği; yan etki tekrarlarını engellemek için kullanılır.</summary>
        Guid Id { get; }

        DateTime OccurredOn { get; }
    }
}
