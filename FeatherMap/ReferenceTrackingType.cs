using System;

namespace FeatherMap
{
    internal class ReferenceTrackingType
    {
        public Type Source { get; }
        public Type Target { get; }

        public ReferenceTrackingType(Type source, Type target)
        {
            Source = source;
            Target = target;
        }
    }
}
