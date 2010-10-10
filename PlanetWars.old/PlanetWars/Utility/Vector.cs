using System.Drawing;

namespace PlanetWars.Utility
{
    public struct Vector
    {
        public Vector(Point point) : this()
        {
            X = point.X;
            Y = point.Y;
        }

        public Vector(Point a, Point b) : this()
        {
            X = a.X - b.X;
            Y = a.Y - b.Y;
        }

        public Vector(int x, int y) : this()
        {
            X = x;
            Y = y;
        }

        public int X { get; set; }
        public int Y { get; set; }

        public static Vector operator -(Vector value)
        {
            return -1*value;
        }

        public static Vector operator *(Vector left, int right)
        {
            return new Vector(left.X*right, left.Y*right);
        }

        public static Vector operator *(int right, Vector left)
        {
            return left*right;
        }

        public override string ToString()
        {
            return X + ", " + Y;
        }
    }
}