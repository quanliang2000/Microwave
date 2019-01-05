﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microwave.Application.Results;
using Microwave.Domain;
using Microwave.EventStores.Ports;

namespace Microwave.EventStores
{
    public class EventStore : IEventStore
    {
        private readonly IEventRepository _eventRepository;
        private readonly ISnapShotRepository _snapShotRepository;

        public EventStore(IEventRepository eventRepository, ISnapShotRepository snapShotRepository)
        {
            _eventRepository = eventRepository;
            _snapShotRepository = snapShotRepository;
        }

        public async Task AppendAsync(IEnumerable<IDomainEvent> domainEvents, long entityVersion)
        {
            var result = await _eventRepository.AppendAsync(domainEvents, entityVersion);
            result.Check();
        }

        public async Task<EventStoreResult<T>> LoadAsync<T>(Identity entityId) where T : IApply, new()
        {
            var snapShot = await _snapShotRepository.LoadSnapShot<T>(entityId);
            var entity = snapShot.Entity;
            var result = await _eventRepository.LoadEventsByEntity(entityId, snapShot.Version);
            if (result.Is<NotFound>()) return EventStoreResult<T>.NotFound(entityId);
            var domainEventWrappers = result.Value.ToList();
            entity.Apply(domainEventWrappers.Select(ev => ev.DomainEvent));
            var version = domainEventWrappers.LastOrDefault()?.Version ?? snapShot.Version;
            if (NeedSnapshot(typeof(T), snapShot.Version, version))
                await _snapShotRepository.SaveSnapShot(new SnapShotWrapper<T>(entity, entityId, version));
            return EventStoreResult<T>.Ok(entity, version);
        }

        private bool NeedSnapshot(Type type, long snapShotVersion, long version)
        {
            if (!(type.GetCustomAttribute(typeof(SnapShotAfterAttribute)) is SnapShotAfterAttribute customAttribute)) return false;
            return customAttribute.DoesNeedSnapshot(snapShotVersion, version);
        }
    }
}