﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Framework;
using Domain.Framework;

namespace Adapters.Framework.EventStores
{
    public class EventStore : IEventStore
    {
        private readonly IDomainEventPersister _domainEventPersister;
        private readonly IEventSourcingStrategy _eventSourcingStrategy;
        private IEnumerable<DomainEvent> _domainEvents;

        public EventStore(IDomainEventPersister domainEventPersister, IEventSourcingStrategy eventSourcingStrategy)
        {
            _domainEventPersister = domainEventPersister;
            _eventSourcingStrategy = eventSourcingStrategy;
        }

        public async Task AppendAsync(IEnumerable<DomainEvent> domainEvents)
        {
            if (_domainEvents == null) _domainEvents = await _domainEventPersister.GetAsync() ?? new List<DomainEvent>();

            var domainEventsTemp = _domainEvents.ToList();
            foreach (var domainEvent in domainEvents)
            {
                domainEventsTemp.Add(domainEvent);
            }

            _domainEvents = domainEventsTemp;
            await _domainEventPersister.Save(domainEventsTemp);
        }

        public async Task AppendAsync(DomainEvent domainEvent)
        {
            if (_domainEvents == null) _domainEvents = await _domainEventPersister.GetAsync();
            var domainEventsTemp = _domainEvents.ToList();
            domainEventsTemp.Add(domainEvent);
            _domainEvents = domainEventsTemp;
            await _domainEventPersister.Save(domainEventsTemp);
        }

        public async Task<T> LoadAsync<T>(Guid commandEntityId) where T : Entity, new()
        {
            var entity = new T();
            if (_domainEvents == null) _domainEvents = await _domainEventPersister.GetAsync();
            var domainEventsForEntity = _domainEvents.Where(domainEvent => domainEvent.EntityId == commandEntityId);
            foreach (var domainEvent in domainEventsForEntity)
            {
                entity = _eventSourcingStrategy.Apply(entity, domainEvent);
            }

            return entity;
        }

        public async Task<IEnumerable<DomainEvent>> GetEvents()
        {
            if (_domainEvents == null) _domainEvents = await _domainEventPersister.GetAsync();
            return _domainEvents;
        }
    }
}