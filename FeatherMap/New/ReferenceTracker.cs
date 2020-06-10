using System.Collections.Generic;

namespace FeatherMap.New
{
    internal class ReferenceTracker
    {
        private readonly Dictionary<TargetTypeSourceObject, object> _references = new Dictionary<TargetTypeSourceObject, object>();

        public bool TryGet(TargetTypeSourceObject key, out object alreadyMappedObject) => 
            _references.TryGetValue(key, out alreadyMappedObject);

        public void Add(TargetTypeSourceObject key, object targetObj)
        {
            if (!_references.ContainsKey(key))
            {
                _references.Add(key, targetObj);
            }
        }
    }
}
