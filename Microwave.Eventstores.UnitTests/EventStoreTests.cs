using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microwave.Application;
using Microwave.Application.Exceptions;
using Microwave.Application.Results;
using Microwave.Domain;
using Microwave.EventStores;
using Microwave.EventStores.Ports;
using Moq;

namespace Microwave.Eventstores.UnitTests
{
    [TestClass]
    public class EventStoreTests : IntegrationTests
    {
        [TestMethod]
        public async Task ApplyMethod_HappyPath()
        {
            var snapShotRepo = new Mock<ISnapShotRepository>();
            snapShotRepo.Setup(re => re.LoadSnapShot<TestEntity>(It.IsAny<string>()))
                .ReturnsAsync(new DefaultSnapshot<TestEntity>());

            var entityStremRepo = new Mock<IEventRepository>();
            var entityId = Guid.NewGuid().ToString();
            var testEventEventStore = new TestEventEventStore(entityId);
            var domainEventWrapper = new DomainEventWrapper
            {
                DomainEvent  = testEventEventStore
            };
            entityStremRepo.Setup(ev => ev.LoadEventsByEntity(It.IsAny<string>(), It.IsAny<long>()))
                .ReturnsAsync( Result<IEnumerable<DomainEventWrapper>>.Ok( new[] { domainEventWrapper }));
            var eventStore = new EventStore(entityStremRepo.Object, snapShotRepo.Object);
            var loadAsync = await eventStore.LoadAsync<TestEntity>(entityId);

            Assert.AreEqual(entityId, loadAsync.Value.Entity.Id);
        }

        [TestMethod]
        public async Task ApplyMethod_NoIfDeclared()
        {
            var snapShotRepo = new Mock<ISnapShotRepository>();
            snapShotRepo.Setup(re => re.LoadSnapShot<TestEntity_NoIApply>(It.IsAny<string>()))
                .ReturnsAsync(new DefaultSnapshot<TestEntity_NoIApply>());

            var entityStremRepo = new Mock<IEventRepository>();
            var entityId = Guid.NewGuid().ToString();
            var testEventEventStore = new TestEventEventStore(entityId);
            var domainEventWrapper = new DomainEventWrapper
            {
                DomainEvent  = testEventEventStore
            };
            entityStremRepo.Setup(ev => ev.LoadEventsByEntity(It.IsAny<string>(), It.IsAny<long>()))
                .ReturnsAsync( Result<IEnumerable<DomainEventWrapper>>.Ok( new[] { domainEventWrapper }));
            var eventStore = new EventStore(entityStremRepo.Object, snapShotRepo.Object);
            var loadAsync = await eventStore.LoadAsync<TestEntity_NoIApply>(entityId);

            Assert.AreEqual(Guid.Empty, loadAsync.Value.Entity.Id);
        }

        [TestMethod]
        public async Task ApplyMethod_WrongIfDeclared()
        {
            var entityStremRepo = new Mock<IEventRepository>();
            var snapShotRepo = new Mock<ISnapShotRepository>();
            snapShotRepo.Setup(re => re.LoadSnapShot<TestEntity_NoIApply>(It.IsAny<string>()))
                .ReturnsAsync(new DefaultSnapshot<TestEntity_NoIApply>());
            var entityId = Guid.NewGuid().ToString();
            var testEventEventStore = new TestEventEventStore(entityId);
            var domainEventWrapper = new DomainEventWrapper
            {
                DomainEvent  = testEventEventStore
            };
            entityStremRepo.Setup(ev => ev.LoadEventsByEntity(It.IsAny<string>(), It.IsAny<long>()))
                .ReturnsAsync( Result<IEnumerable<DomainEventWrapper>>.Ok( new[] { domainEventWrapper }));
            var eventStore = new EventStore(entityStremRepo.Object, snapShotRepo.Object);
            var loadAsync = await eventStore.LoadAsync<TestEntity_NoIApply>(entityId);

            Assert.AreEqual(Guid.Empty, loadAsync.Value.Entity.Id);
        }

        [TestMethod]
        public async Task IntegrationWithRepo()
        {
            var snapShotRepo = new Mock<ISnapShotRepository>();
            snapShotRepo.Setup(re => re.LoadSnapShot<TestEntity>(It.IsAny<string>()))
                .ReturnsAsync(new DefaultSnapshot<TestEntity>());
            var entityId = Guid.NewGuid().ToString();
            var eventStore = new EventStore(new EventRepository(EventDatabase, new VersionCache(EventDatabase)), snapShotRepo.Object);

            await eventStore.AppendAsync(new List<IDomainEvent> {new TestEventEventStore(entityId, "Test")}, 0);
            var loadAsync = await eventStore.LoadAsync<TestEntity>(entityId);
            var loadAsync2 = await eventStore.LoadAsync<TestEntity>(entityId);

            Assert.IsTrue(entityId.Equals(loadAsync.Value.Entity.Id));
            Assert.AreEqual("Test", loadAsync.Value.Entity.Name);

            Assert.IsTrue(entityId.Equals(loadAsync2.Value.Entity.Id));
            Assert.AreEqual("Test", loadAsync2.Value.Entity.Name);
        }

        [TestMethod]
        public async Task IntegrationWithRepo_AddSingleEvent()
        {
            var snapShotRepo = new Mock<ISnapShotRepository>();
            snapShotRepo.Setup(re => re.LoadSnapShot<TestEntity>(It.IsAny<string>()))
                .ReturnsAsync(new DefaultSnapshot<TestEntity>());
            var entityId = Guid.NewGuid().ToString();
            var eventStore = new EventStore(new EventRepository(EventDatabase, new VersionCache(EventDatabase)), snapShotRepo.Object);

            await eventStore.AppendAsync(new TestEventEventStore(entityId, "Test"), 0);
            var loadAsync = await eventStore.LoadAsync<TestEntity>(entityId);

            Assert.IsTrue(entityId.Equals(loadAsync.Value.Entity.Id));
            Assert.AreEqual("Test", loadAsync.Value.Entity.Name);
        }

        [TestMethod]
        public async Task DifferentIdsInEventsDefined()
        {
            var snapShotRepo = new Mock<ISnapShotRepository>();
            snapShotRepo.Setup(re => re.LoadSnapShot<TestEntity>(It.IsAny<string>()))
                .ReturnsAsync(new DefaultSnapshot<TestEntity>());
            var entityId = Guid.NewGuid().ToString();
            var entityId2 = Guid.NewGuid();
            var eventStore = new EventStore(new EventRepository(EventDatabase, new VersionCache(EventDatabase)), snapShotRepo.Object);

            await Assert.ThrowsExceptionAsync<DifferentIdsException>(async () => await eventStore.AppendAsync(new
            List<IDomainEvent> {new
            TestEventEventStore(entityId, "Test"), new
                TestEventEventStore(entityId2.ToString(), "Test")}, 0));
        }

        [TestMethod]
        public async Task NotFoundExceptionIsWithCorrectT()
        {
            var snapShotRepo = new Mock<ISnapShotRepository>();
            snapShotRepo.Setup(re => re.LoadSnapShot<TestEntity>(It.IsAny<string>()))
                .ReturnsAsync(new DefaultSnapshot<TestEntity>());
            var entityId = Guid.NewGuid().ToString();
            var eventStore = new EventStore(new EventRepository(EventDatabase, new VersionCache(EventDatabase)), snapShotRepo.Object);

            var result = await eventStore.LoadAsync<TestEntity>(entityId);
            var exception = Assert.ThrowsException<NotFoundException>(() => result.Value);

            Assert.IsTrue(exception.Message.StartsWith("Could not find TestEntity"));

        }

        [TestMethod]
        public async Task IntegrationWithRepo_NotFound()
        {
            var snapShotRepo = new Mock<ISnapShotRepository>();
            snapShotRepo.Setup(re => re.LoadSnapShot<TestEntity>(It.IsAny<string>()))
                .ReturnsAsync(new DefaultSnapshot<TestEntity>());
            var entityId = Guid.NewGuid().ToString();
            var eventStore = new EventStore(new EventRepository(EventDatabase, new VersionCache(EventDatabase)), snapShotRepo.Object);

            await eventStore.AppendAsync(new List<IDomainEvent> {new TestEventEventStore(entityId, "Test")}, 0);
            var loadAsync = await eventStore.LoadAsync<TestEntity>(Guid.NewGuid().ToString());

            Assert.IsTrue(loadAsync.Is<NotFound>());
        }
    }

    public class TestEntity_WrongIApply : Entity
    {
        public string Id { get; private set; }
        public void Apply(WrongEvent domainEvent)
        {
            Id = domainEvent.EntityId;
        }
    }

    public class WrongEvent : IDomainEvent
    {
        public WrongEvent(string entityId)
        {
            EntityId = entityId;
        }

        public string EntityId { get; }
    }

    public class TestEntity_NoIApply : Entity
    {
        public Guid Id { get; private set; }
    }

    public class TestEntity : Entity
    {
        public void Apply(TestEventEventStore domainEvent)
        {
            Id = domainEvent.EntityId;
            Name = domainEvent.Name;
        }

        public string Id { get; private set; }
        public string Name { get; set; }
    }

    public class TestEventEventStore : IDomainEvent
    {
        public TestEventEventStore(string entityId, string name = null)
        {
            EntityId = entityId;
            Name = name;
        }

        public string EntityId { get; }
        public string Name { get; }
    }
}