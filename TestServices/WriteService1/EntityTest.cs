using Microwave.Domain;

namespace WriteService1
{
    public class EntityTest : Entity, IApply<Event2>
    {
        public void Apply(Event2 domainEvent)
        {
            Id = domainEvent.EntityId;
        }

        public Identity Id { get; private set; }
    }
}