using System;

namespace FeatherMap
{
    internal readonly struct TargetTypeSourceObject : IEquatable<TargetTypeSourceObject>
    {
        public TargetTypeSourceObject(Type targetPropertyType, object sourceObject)
        {
            TargetPropertyType = targetPropertyType;
            SourceObject = sourceObject;
        }

        public readonly Type TargetPropertyType;

        public readonly object SourceObject;

        public bool Equals(TargetTypeSourceObject other) => TargetPropertyType == other.TargetPropertyType && SourceObject == other.SourceObject;

        public override bool Equals(object obj) => obj is TargetTypeSourceObject other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return ((TargetPropertyType != null ? TargetPropertyType.GetHashCode() : 0) * 397) ^ (SourceObject != null ? SourceObject.GetHashCode() : 0);
            }
        }
    }
}
