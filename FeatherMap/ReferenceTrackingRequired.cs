using System;

namespace FeatherMap
{
    internal class ReferenceTrackingRequired
    {
        public Type Source { get; }
        public Type Target { get; }

        public ReferenceTrackingRequired(Type source, Type target)
        {
            Source = source;
            Target = target;
        }
    }
}
