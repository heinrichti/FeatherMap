using System;

namespace FeatherMap.New
{
    internal readonly struct SourceTargetType : IEquatable<SourceTargetType>
    {
        public SourceTargetType(Type sourcePropertyType, Type targetPropertyType)
        {
            SourcePropertyType = sourcePropertyType;
            TargetPropertyType = targetPropertyType;
        }
        public Type SourcePropertyType { get; }
        public Type TargetPropertyType { get; }

        public bool Equals(SourceTargetType other) => SourcePropertyType == other.SourcePropertyType && TargetPropertyType == other.TargetPropertyType;

        public override bool Equals(object obj) => obj is SourceTargetType other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return ((SourcePropertyType != null ? SourcePropertyType.GetHashCode() : 0) * 397) ^ (TargetPropertyType != null ? TargetPropertyType.GetHashCode() : 0);
            }
        }
    }
}
