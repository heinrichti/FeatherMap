using System;

namespace FeatherMap
{
    internal class ReferenceTracker
    {
        private Container _head;

        private class Container
        {
            public Container(Type targetType, object sourceObject, object targetObject, Container next)
            {
                TargetType = targetType;
                SourceObject = sourceObject;
                TargetObject = targetObject;
                Next = next;
            }

            internal readonly Type TargetType;
            internal readonly object SourceObject;
            internal readonly object TargetObject;

            internal readonly Container Next;
        }

        public bool TryGet(Type targetPropertyType, object sourceObject, out object alreadyMappedObject)
        {
            if (_head == null)
            {
                alreadyMappedObject = null;
                return false;
            }

            var item = _head;
            while (item != null)
            {
                var current = item;
                if (current.TargetType == targetPropertyType && current.SourceObject == sourceObject)
                {
                    alreadyMappedObject = current.TargetObject;
                    return true;
                }

                item = item.Next;
            }

            alreadyMappedObject = null;
            return false;
        }

        public void Add(Type targetPropertyType, object sourceObject, object targetObject) =>
            _head = new Container(targetPropertyType, sourceObject, targetObject, _head);
    }
}
