using System;
using System.Collections.Generic;

namespace FeatherMap
{
    internal class ComplexMapResult<T, TU>
    {
        public ComplexMapResult(Action<T, TU, ReferenceTracker> mappingFunc, List<ReferenceTrackingType> referenceTrackingTypes)
        {
            MappingFunc = mappingFunc;
            ReferenceTrackingTypes = referenceTrackingTypes;
        }

        public Action<T, TU, ReferenceTracker> MappingFunc { get; }

        public List<ReferenceTrackingType> ReferenceTrackingTypes { get; }
    }
}
