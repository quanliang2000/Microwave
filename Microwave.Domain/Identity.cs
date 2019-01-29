using System;

namespace Microwave.Domain
{
    public abstract class Identity : IEquatable<Identity>
    {
        public string Id { get; protected set; }

        public static bool operator== (Identity id1, Identity id2)
        {
            return (id1?.Id ?? string.Empty) == (id2?.Id ?? string.Empty);
        }

        public static bool operator!= (Identity id1, Identity id2)
        {
            return id1?.Equals(id2) == false;
        }

        public override bool Equals(Object obj)
        {
            var identity = obj as Identity;
            return Equals(identity);
        }

        public override int GetHashCode()
        {
            return Id != null ? Id.GetHashCode() : 0;
        }

        public static Identity Create(string entityId)
        {
            if (Guid.TryParse(entityId, out var guid))
                return GuidIdentity.Create(guid);
            return StringIdentity.Create(entityId);
        }

        public static Identity Create(Guid entityId)
        {
            return GuidIdentity.Create(entityId);
        }

        public virtual bool Equals(Identity other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Id, other.Id);
        }

        public override string ToString()
        {
            return Id;
        }
    }
}