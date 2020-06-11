using System;
using System.Collections.Generic;

namespace FeatherMap
{
    internal class ComplexMapResult<T, TU>
    {
        public ComplexMapResult(Action<T, TU, ReferenceTracker> mappingFunc, List<ReferenceTrackingRequired> referenceCheckList)
        {
            MappingFunc = mappingFunc;
            ReferenceCheckList = referenceCheckList;
        }

        public Action<T, TU, ReferenceTracker> MappingFunc { get; }

        public List<ReferenceTrackingRequired> ReferenceCheckList { get; }
    }
}
