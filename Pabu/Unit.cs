using System;

namespace Pabu
{
    public struct Unit : IEquatable<Unit>, IComparable<Unit>
    {
        public bool Equals(Unit other) => true;

        public int CompareTo(Unit other) => 0;

        public override int GetHashCode() => 0;

        public override bool Equals(object obj) => obj is Unit;
    }
}