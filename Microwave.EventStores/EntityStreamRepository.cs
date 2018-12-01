﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microwave.Application;
using Microwave.Application.Results;
using Microwave.Domain;

namespace Microwave.EventStores
{
    public class EntityStreamRepository : IEntityStreamRepository
    {
        private readonly IObjectConverter _eventConverter;
        private readonly EventStoreWriteContext _eventStoreWriteContext;
        private readonly object _lock = new Object();

        public EntityStreamRepository(IObjectConverter eventConverter, EventStoreWriteContext eventStoreWriteContext)
        {
            _eventConverter = eventConverter;
            _eventStoreWriteContext = eventStoreWriteContext;
        }

        public async Task<Result<IEnumerable<DomainEventWrapper>>> LoadEventsByEntity(Guid entityId, long from = -1)
        {
            var stream = _eventStoreWriteContext.EntityStreams
                .Where(str => str.EntityId == entityId.ToString() && str.Version > from).ToList();
            if (!stream.Any()) return Result<IEnumerable<DomainEventWrapper>>.NotFound(entityId.ToString());

            var domainEvents = stream.Select(dbo =>
            {
                return new DomainEventWrapper
                {
                    Created = dbo.Created,
                    Version = dbo.Version,
                    DomainEvent = _eventConverter.Deserialize<IDomainEvent>(dbo.Payload)
                };
            });

            return Result<IEnumerable<DomainEventWrapper>>.Ok(domainEvents);
        }

        public async Task<Result<IEnumerable<DomainEventWrapper>>> LoadEventsSince(long tickSince = -1)
        {
            var stream = _eventStoreWriteContext.EntityStreams
                .Where(str => str.Created > tickSince).ToList();
            if (!stream.Any()) return Result<IEnumerable<DomainEventWrapper>>.Ok(new List<DomainEventWrapper>());

            var domainEvents = stream.Select(dbo =>
            {
                var domainEvent = _eventConverter.Deserialize<IDomainEvent>(dbo.Payload);
                return new DomainEventWrapper
                {
                    Created = dbo.Created,
                    Version = dbo.Version,
                    DomainEvent = domainEvent
                };
            });

            return Result<IEnumerable<DomainEventWrapper>>.Ok(domainEvents);
        }

        //TODO remove Lock and make threadsafe
        public async Task<Result> AppendAsync(IEnumerable<IDomainEvent> domainEvents, long entityVersion)
        {
            var events = domainEvents.ToList();
            var entityId = events.First().EntityId;
            lock (_lock)
            {
                var stream = _eventStoreWriteContext.EntityStreams
                    .Where(str => str.EntityId == entityId.ToString()).ToList();

                var entityVersionTemp = stream.LastOrDefault()?.Version ?? -1;
                if (entityVersionTemp != entityVersion) return Result.ConcurrencyResult(entityVersion, entityVersionTemp);

                foreach (var domainEvent in events)
                {
                    entityVersionTemp = entityVersionTemp + 1;
                    var serialize = _eventConverter.Serialize(domainEvent);
                    var domainEventDbo = new DomainEventDbo
                    {
                        Payload = serialize,
                        Created = DateTime.UtcNow.Ticks,
                        Version = entityVersionTemp,
                        EntityId = domainEvent.EntityId.ToString()
                    };

                    _eventStoreWriteContext.EntityStreams.Add(domainEventDbo);
                }

                _eventStoreWriteContext.SaveChanges();
                return Result.Ok();
            }
        }
    }
}