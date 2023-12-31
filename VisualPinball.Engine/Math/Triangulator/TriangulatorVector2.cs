// MIT License - Copyright (C) The Mono.Xna Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;

namespace VisualPinball.Engine.Math.Triangulator
{
	internal struct TriangulatorVector2 : IEquatable<TriangulatorVector2>
	{
		#region Public Static Properties

		/// <summary>
		/// Returns a <see cref="TriangulatorVector2"/> with components 1, 1.
		/// </summary>
		public static TriangulatorVector2 One => unitVector;

		/// <summary>
		/// Returns a <see cref="TriangulatorVector2"/> with components 1, 0.
		/// </summary>
		public static TriangulatorVector2 UnitX => unitXVector;

		/// <summary>
		/// Returns a <see cref="TriangulatorVector2"/> with components 0, 1.
		/// </summary>
		public static TriangulatorVector2 UnitY => unitYVector;

		#endregion

		#region Public Fields

		/// <summary>
		/// The x coordinate of this <see cref="TriangulatorVector2"/>.
		/// </summary>
		public float X;

		/// <summary>
		/// The y coordinate of this <see cref="TriangulatorVector2"/>.
		/// </summary>
		public float Y;

		#endregion

		#region Private Static Fields

		private static readonly TriangulatorVector2 zeroVector = new TriangulatorVector2(0f, 0f);
		private static readonly TriangulatorVector2 unitVector = new TriangulatorVector2(1f, 1f);
		private static readonly TriangulatorVector2 unitXVector = new TriangulatorVector2(1f, 0f);
		private static readonly TriangulatorVector2 unitYVector = new TriangulatorVector2(0f, 1f);

		#endregion

		#region Public Constructors

		/// <summary>
		/// Constructs a 2d vector with X and Y from two values.
		/// </summary>
		/// <param name="x">The x coordinate in 2d-space.</param>
		/// <param name="y">The y coordinate in 2d-space.</param>
		public TriangulatorVector2(float x, float y)
		{
			X = x;
			Y = y;
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Compares whether current instance is equal to specified <see cref="Object"/>.
		/// </summary>
		/// <param name="obj">The <see cref="Object"/> to compare.</param>
		/// <returns><c>true</c> if the instances are equal; <c>false</c> otherwise.</returns>
		public override bool Equals(object obj)
		{
			return obj is TriangulatorVector2 && Equals((TriangulatorVector2) obj);
		}

		/// <summary>
		/// Compares whether current instance is equal to specified <see cref="TriangulatorVector2"/>.
		/// </summary>
		/// <param name="other">The <see cref="TriangulatorVector2"/> to compare.</param>
		/// <returns><c>true</c> if the instances are equal; <c>false</c> otherwise.</returns>
		public bool Equals(TriangulatorVector2 other)
		{
			return X == other.X &&
			       Y == other.Y;
		}

		/// <summary>
		/// Gets the hash code of this <see cref="TriangulatorVector2"/>.
		/// </summary>
		/// <returns>Hash code of this <see cref="TriangulatorVector2"/>.</returns>
		public override int GetHashCode()
		{
			return X.GetHashCode() + Y.GetHashCode();
		}


		/// <summary>
		/// Returns a <see cref="String"/> representation of this <see cref="TriangulatorVector2"/> in the format:
		/// {X:[<see cref="X"/>] Y:[<see cref="Y"/>]}
		/// </summary>
		/// <returns>A <see cref="String"/> representation of this <see cref="TriangulatorVector2"/>.</returns>
		public override string ToString()
		{
			return "{X:" + X.ToString() +
			       " Y:" + Y.ToString() +
			       "}";
		}

		#endregion

		#region Public Static Methods


		/// <summary>
		/// Returns the distance between two vectors.
		/// </summary>
		/// <param name="value1">The first vector.</param>
		/// <param name="value2">The second vector.</param>
		/// <returns>The distance between two vectors.</returns>
		public static float Distance(TriangulatorVector2 value1, TriangulatorVector2 value2)
		{
			float v1 = value1.X - value2.X, v2 = value1.Y - value2.Y;
			return (float) System.Math.Sqrt(v1 * v1 + v2 * v2);
		}

		/// <summary>
		/// Returns the distance between two vectors.
		/// </summary>
		/// <param name="value1">The first vector.</param>
		/// <param name="value2">The second vector.</param>
		/// <param name="result">The distance between two vectors as an output parameter.</param>
		public static void Distance(ref TriangulatorVector2 value1, ref TriangulatorVector2 value2, out float result)
		{
			float v1 = value1.X - value2.X, v2 = value1.Y - value2.Y;
			result = (float) System.Math.Sqrt(v1 * v1 + v2 * v2);
		}

		/// <summary>
		/// Returns the squared distance between two vectors.
		/// </summary>
		/// <param name="value1">The first vector.</param>
		/// <param name="value2">The second vector.</param>
		/// <returns>The squared distance between two vectors.</returns>
		public static float DistanceSquared(TriangulatorVector2 value1, TriangulatorVector2 value2)
		{
			float v1 = value1.X - value2.X, v2 = value1.Y - value2.Y;
			return v1 * v1 + v2 * v2;
		}

		/// <summary>
		/// Returns the squared distance between two vectors.
		/// </summary>
		/// <param name="value1">The first vector.</param>
		/// <param name="value2">The second vector.</param>
		/// <param name="result">The squared distance between two vectors as an output parameter.</param>
		public static void DistanceSquared(
			ref TriangulatorVector2 value1,
			ref TriangulatorVector2 value2,
			out float result
		) {
			float v1 = value1.X - value2.X, v2 = value1.Y - value2.Y;
			result = v1 * v1 + v2 * v2;
		}

		/// <summary>
		/// Returns a dot product of two vectors.
		/// </summary>
		/// <param name="value1">The first vector.</param>
		/// <param name="value2">The second vector.</param>
		/// <returns>The dot product of two vectors.</returns>
		public static float Dot(TriangulatorVector2 value1, TriangulatorVector2 value2)
		{
			return value1.X * value2.X + value1.Y * value2.Y;
		}

		/// <summary>
		/// Creates a new <see cref="TriangulatorVector2"/> that contains a normalized values from another vector.
		/// </summary>
		/// <param name="value">Source <see cref="TriangulatorVector2"/>.</param>
		/// <returns>Unit vector.</returns>
		public static TriangulatorVector2 Normalize(TriangulatorVector2 value)
		{
			float val = 1.0f / (float) System.Math.Sqrt(value.X * value.X + value.Y * value.Y);
			value.X *= val;
			value.Y *= val;
			return value;
		}

		#endregion

		#region Public Static Operators

		/// <summary>
		/// Inverts values in the specified <see cref="TriangulatorVector2"/>.
		/// </summary>
		/// <param name="value">Source <see cref="TriangulatorVector2"/> on the right of the sub sign.</param>
		/// <returns>Result of the inversion.</returns>
		public static TriangulatorVector2 operator -(TriangulatorVector2 value)
		{
			value.X = -value.X;
			value.Y = -value.Y;
			return value;
		}

		/// <summary>
		/// Compares whether two <see cref="TriangulatorVector2"/> instances are equal.
		/// </summary>
		/// <param name="value1"><see cref="TriangulatorVector2"/> instance on the left of the equal sign.</param>
		/// <param name="value2"><see cref="TriangulatorVector2"/> instance on the right of the equal sign.</param>
		/// <returns><c>true</c> if the instances are equal; <c>false</c> otherwise.</returns>
		public static bool operator ==(TriangulatorVector2 value1, TriangulatorVector2 value2)
		{
			return value1.X == value2.X &&
			       value1.Y == value2.Y;
		}

		/// <summary>
		/// Compares whether two <see cref="TriangulatorVector2"/> instances are equal.
		/// </summary>
		/// <param name="value1"><see cref="TriangulatorVector2"/> instance on the left of the equal sign.</param>
		/// <param name="value2"><see cref="TriangulatorVector2"/> instance on the right of the equal sign.</param>
		/// <returns><c>true</c> if the instances are equal; <c>false</c> otherwise.</returns>
		public static bool operator !=(TriangulatorVector2 value1, TriangulatorVector2 value2)
		{
			return !(value1 == value2);
		}

		/// <summary>
		/// Adds two vectors.
		/// </summary>
		/// <param name="value1">Source <see cref="TriangulatorVector2"/> on the left of the add sign.</param>
		/// <param name="value2">Source <see cref="TriangulatorVector2"/> on the right of the add sign.</param>
		/// <returns>Sum of the vectors.</returns>
		public static TriangulatorVector2 operator +(TriangulatorVector2 value1, TriangulatorVector2 value2)
		{
			value1.X += value2.X;
			value1.Y += value2.Y;
			return value1;
		}

		/// <summary>
		/// Subtracts a <see cref="TriangulatorVector2"/> from a <see cref="TriangulatorVector2"/>.
		/// </summary>
		/// <param name="value1">Source <see cref="TriangulatorVector2"/> on the left of the sub sign.</param>
		/// <param name="value2">Source <see cref="TriangulatorVector2"/> on the right of the sub sign.</param>
		/// <returns>Result of the vector subtraction.</returns>
		public static TriangulatorVector2 operator -(TriangulatorVector2 value1, TriangulatorVector2 value2)
		{
			value1.X -= value2.X;
			value1.Y -= value2.Y;
			return value1;
		}

		/// <summary>
		/// Multiplies the components of two vectors by each other.
		/// </summary>
		/// <param name="value1">Source <see cref="TriangulatorVector2"/> on the left of the mul sign.</param>
		/// <param name="value2">Source <see cref="TriangulatorVector2"/> on the right of the mul sign.</param>
		/// <returns>Result of the vector multiplication.</returns>
		public static TriangulatorVector2 operator *(TriangulatorVector2 value1, TriangulatorVector2 value2)
		{
			value1.X *= value2.X;
			value1.Y *= value2.Y;
			return value1;
		}

		/// <summary>
		/// Multiplies the components of vector by a scalar.
		/// </summary>
		/// <param name="value">Source <see cref="TriangulatorVector2"/> on the left of the mul sign.</param>
		/// <param name="scaleFactor">Scalar value on the right of the mul sign.</param>
		/// <returns>Result of the vector multiplication with a scalar.</returns>
		public static TriangulatorVector2 operator *(TriangulatorVector2 value, float scaleFactor)
		{
			value.X *= scaleFactor;
			value.Y *= scaleFactor;
			return value;
		}

		/// <summary>
		/// Multiplies the components of vector by a scalar.
		/// </summary>
		/// <param name="scaleFactor">Scalar value on the left of the mul sign.</param>
		/// <param name="value">Source <see cref="TriangulatorVector2"/> on the right of the mul sign.</param>
		/// <returns>Result of the vector multiplication with a scalar.</returns>
		public static TriangulatorVector2 operator *(float scaleFactor, TriangulatorVector2 value)
		{
			value.X *= scaleFactor;
			value.Y *= scaleFactor;
			return value;
		}

		/// <summary>
		/// Divides the components of a <see cref="TriangulatorVector2"/> by the components of another <see cref="TriangulatorVector2"/>.
		/// </summary>
		/// <param name="value1">Source <see cref="TriangulatorVector2"/> on the left of the div sign.</param>
		/// <param name="value2">Divisor <see cref="TriangulatorVector2"/> on the right of the div sign.</param>
		/// <returns>The result of dividing the vectors.</returns>
		public static TriangulatorVector2 operator /(TriangulatorVector2 value1, TriangulatorVector2 value2)
		{
			value1.X /= value2.X;
			value1.Y /= value2.Y;
			return value1;
		}

		/// <summary>
		/// Divides the components of a <see cref="TriangulatorVector2"/> by a scalar.
		/// </summary>
		/// <param name="value1">Source <see cref="TriangulatorVector2"/> on the left of the div sign.</param>
		/// <param name="divider">Divisor scalar on the right of the div sign.</param>
		/// <returns>The result of dividing a vector by a scalar.</returns>
		public static TriangulatorVector2 operator /(TriangulatorVector2 value1, float divider)
		{
			float factor = 1 / divider;
			value1.X *= factor;
			value1.Y *= factor;
			return value1;
		}

		#endregion
	}
}
