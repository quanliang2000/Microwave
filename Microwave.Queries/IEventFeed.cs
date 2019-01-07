using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microwave.Application;

namespace Microwave.Queries
{
    public interface IEventFeed<T>
    {
        Task<IEnumerable<DomainEventWrapper>> GetEventsAsync(DateTimeOffset since = default(DateTimeOffset));
    }
}