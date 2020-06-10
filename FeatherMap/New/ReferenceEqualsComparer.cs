using System;
using System.Collections.Generic;

namespace FeatherMap.New
{
    internal class ReferenceEqualsComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceEqualsComparer Instance = new ReferenceEqualsComparer();

        public bool Equals(object x, object y) => ReferenceEquals(x, y);

        public int GetHashCode(object obj) => obj.GetHashCode();
    }
}
