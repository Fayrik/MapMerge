using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapMerge
{
    public class Location
    {
        public int X;
        public int Y;
        public int Z;

        public Location()
        {
        }

        public Location(int x, int y, int z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public void Set(int x, int y, int z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public override int GetHashCode()
        { return (this.Z * 256 + this.Y) * 256 + this.X; }

        public override String ToString()
        { return String.Concat("(", this.X, ",", this.Y, ",", this.Z, ")"); }

        public override bool Equals(Object obj)
        {
            if (!(obj is Location))
            { return false; }
            Location location = (Location)obj;
            if (this.X == location.X && this.Y == location.Y && this.Z == location.Z)
            { return true; }
            return false;
        }

        public static bool operator ==(Location l1, Location l2)
        { return l1.Equals(l2); }

        public static bool operator !=(Location l1, Location l2)
        { return !l1.Equals(l2); }
    }
}
