using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using FeatherMap.New;

namespace FeatherMap
{
    internal class ReferenceTracker
    {
        private readonly Dictionary<SourceTargetType, Dictionary<object, object>> _references = new Dictionary<SourceTargetType, Dictionary<object, object>>();

        public bool TryGet(SourceTargetType key, object sourceObj, out object alreadyMappedObject)
        {
            if (_references.TryGetValue(key, out var objects))
                return !objects.TryGetValue(sourceObj, out alreadyMappedObject);

            alreadyMappedObject = null;
            return true;
        }

        public void Add(SourceTargetType key, object sourceObj, object targetObj)
        {
            if (!_references.TryGetValue(key, out var objects))
            {
                objects = new Dictionary<object, object>(ReferenceEqualsComparer.Instance);
                _references.Add(key, objects);
            }

            objects.Add(sourceObj, targetObj);
        }
    }
}
