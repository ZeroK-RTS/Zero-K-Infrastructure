using System;

namespace ServiceData
{
	public struct Position
	{
		public double X;
		public double Y;

		public Position(double x, double y)
		{
			X = x;
			Y = y;
		}

		public bool Equals(Position other)
		{
			return other.X == X && other.Y == Y;
		}

		public double Length()
		{
			return Math.Sqrt(X*X + Y*Y);
		}

		public Position Normalized()
		{
			var len = Length();
			return new Position(X /= len, Y/=len);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (obj.GetType() != typeof(Position)) return false;
			return Equals((Position)obj);
		}

		public override int GetHashCode()
		{
			unchecked {
				return (X.GetHashCode()*397) ^ Y.GetHashCode();
			}
		}

		public static Position operator -(Position first, Position second)
		{
			return new Position(first.X - second.X, first.Y - second.Y);
		}

		public static Position operator +(Position first, Position second)
		{
			return new Position(first.X + second.X, first.Y + second.Y);
		}

		public static Position operator *(Position first, double scaler)
		{
			return new Position(first.X*scaler, first.Y*scaler);
		}

		public static Position operator *(double scaler, Position first)
		{
			return new Position(first.X*scaler, first.Y*scaler);
		}


		public static bool operator ==(Position left, Position right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(Position left, Position right)
		{
			return !left.Equals(right);
		}
	}
}