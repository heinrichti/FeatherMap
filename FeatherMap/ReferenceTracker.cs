using System.Collections.Specialized;

namespace FeatherMap
{
    internal class ReferenceTracker
    {
        private readonly HybridDictionary _references = new HybridDictionary();

        public bool TryGet(TargetTypeSourceObject key, out object alreadyMappedObject)
        {
            var reference = _references[key];
            if (reference == null)
            {
                alreadyMappedObject = null;
                return false;
            }

            alreadyMappedObject = reference;
            return true;
        }

        public void Add(TargetTypeSourceObject key, object targetObj) => _references[key] = targetObj;
    }
}
