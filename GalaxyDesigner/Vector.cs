// A Force-Directed Diagram Layout Algorithm
// Bradley Smith - 2010/07/01

using System;
using System.Drawing;

/// <summary>
/// Represents a vector whose magnitude and direction are both expressed as System.Double. 
/// Vector addition and scalar multiplication are supported.
/// </summary>
public struct Vector {

	private double mMagnitude;
	private double mDirection;

	/// <summary>
	/// Gets or sets the magnitude of the vector.
	/// </summary>
	public double Magnitude {
		get { return mMagnitude; }
		set { mMagnitude = value; }
	}
	/// <summary>
	/// Gets or sets the direction of the vector.
	/// </summary>
	public double Direction {
		get { return mDirection; }
		set { mDirection = value; }
	}

	/// <summary>
	/// Initialises a new instance of the Vector class using the specified magnitude and direction. 
	/// Automatically simplifies the representation to ensure a positive magnitude and a sub-circular angle.
	/// </summary>
	/// <param name="magnitude">The magnitude of the vector.</param>
	/// <param name="direction">The direction of the vector, in degrees.</param>
	public Vector(double magnitude, double direction) {
		mMagnitude = magnitude;
		mDirection = direction;

		if (mMagnitude < 0) {
			// resolve negative magnitude by reversing direction
			mMagnitude = -mMagnitude;
			mDirection = (180.0 + mDirection) % 360;
		}

		// resolve negative direction
		if (mDirection < 0) mDirection = (360.0 + mDirection);
	}

	/// <summary>
	/// Calculates the resultant sum of two vectors.
	/// </summary>
	/// <param name="a">The first operand.</param>
	/// <param name="b">The second operand.</param>
	/// <returns>The result of vector addition.</returns>
	public static Vector operator +(Vector a, Vector b) {
		// break into x-y components
		double aX = a.Magnitude * Math.Cos((Math.PI / 180.0) * a.Direction);
		double aY = a.Magnitude * Math.Sin((Math.PI / 180.0) * a.Direction);

		double bX = b.Magnitude * Math.Cos((Math.PI / 180.0) * b.Direction);
		double bY = b.Magnitude * Math.Sin((Math.PI / 180.0) * b.Direction);

		// add x-y components
		aX += bX;
		aY += bY;

		// pythagorus' theorem to get resultant magnitude
		double magnitude = Math.Sqrt(Math.Pow(aX, 2) + Math.Pow(aY, 2));

		// calculate direction using inverse tangent
		double direction;
		if (magnitude == 0.0)
			direction = 0.0;
		else if (Math.Abs(aX) > Math.Abs(aY))
			direction = ((180.0 / Math.PI) * Math.Tanh(aY / aX));
		else
			direction = 90.0 - ((180.0 / Math.PI) * Math.Tanh(aX / aY));

		// apply correction depending on quadrant
		if ((aX < 0) && ((aY < 0) || (direction < 0)))
			direction += 180.0;
		else if ((aY < 0) && (direction >= 0))
			direction += 180.0;

		return new Vector(magnitude, direction);
	}

	/// <summary>
	/// Calculates the result of multiplication by a scalar value.
	/// </summary>
	/// <param name="vector">The Vector that forms the first operand.</param>
	/// <param name="multiplier">The System.Double that forms the second operand.</param>
	/// <returns>A Vector whose magnitude has been multiplied by the scalar value.</returns>
	public static Vector operator *(Vector vector, double multiplier) {
		// only magnitude is affected by scalar multiplication
		return new Vector(vector.Magnitude * multiplier, vector.Direction);
	}

	/// <summary>
	/// Converts the vector into an X-Y coordinate representation.
	/// </summary>
	/// <returns>An X-Y coordinate representation of the Vector.</returns>
	public Point ToPoint() {
		// break into x-y components
		double aX = mMagnitude * Math.Cos((Math.PI / 180.0) * mDirection);
		double aY = mMagnitude * Math.Sin((Math.PI / 180.0) * mDirection);

		return new Point((int)aX, (int)aY);
	}

	/// <summary>
	/// Returns a string representation of the vector.
	/// </summary>
	/// <returns>A System.String representing the vector.</returns>
	public override string ToString() {
		return mMagnitude.ToString("N5") + " " + mDirection.ToString("N2") + "°";
	}
}