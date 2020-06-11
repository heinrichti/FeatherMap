using System;

namespace FeatherMap
{
    internal class SourceToTargetMap : IEquatable<SourceToTargetMap>
    {
        private readonly Type _source;
        private readonly Type _target;

        public SourceToTargetMap(Type source, Type target)
        {
            _source = source;
            _target = target;
        }

        public bool Equals(SourceToTargetMap other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return _source == other._source && _target == other._target;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SourceToTargetMap) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((_source != null ? _source.GetHashCode() : 0) * 397) ^ (_target != null ? _target.GetHashCode() : 0);
            }
        }
    }
}
