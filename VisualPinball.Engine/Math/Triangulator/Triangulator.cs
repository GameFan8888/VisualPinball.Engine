// Triangulator
//
// The MIT License (MIT)
//
// Copyright (c) 2017, Nick Gravelyn
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace VisualPinball.Engine.Math.Triangulator
{
	/// <summary>
	/// A static class exposing methods for triangulating 2D polygons. This is the sole public
	/// class in the entire library; all other classes/structures are intended as internal-only
	/// objects used only to assist in triangulation.
	///
	/// This class makes use of the DEBUG conditional and produces quite verbose output when built
	/// in Debug mode. This is quite useful for debugging purposes, but can slow the process down
	/// quite a bit. For optimal performance, build the library in Release mode.
	///
	/// The triangulation is also not optimized for garbage sensitive processing. The point of the
	/// library is a robust, yet simple, system for triangulating 2D shapes. It is intended to be
	/// used as part of your content pipeline or at load-time. It is not something you want to be
	/// using each and every frame unless you really don't care about garbage.
	/// </summary>
	internal static class Triangulator
	{
		#region Fields

		static readonly IndexableCyclicalLinkedList<Vertex> polygonVertices = new IndexableCyclicalLinkedList<Vertex>();
		static readonly IndexableCyclicalLinkedList<Vertex> earVertices = new IndexableCyclicalLinkedList<Vertex>();
		static readonly CyclicalList<Vertex> convexVertices = new CyclicalList<Vertex>();
		static readonly CyclicalList<Vertex> reflexVertices = new CyclicalList<Vertex>();

		#endregion

		#region Public Methods

		#region Triangulate

		/// <summary>
		/// Triangulates a 2D polygon produced the indexes required to render the points as a triangle list.
		/// </summary>
		/// <param name="inputVertices">The polygon vertices in counter-clockwise winding order.</param>
		/// <param name="desiredWindingOrder">The desired output winding order.</param>
		/// <param name="outputVertices">The resulting vertices that include any reversals of winding order and holes.</param>
		/// <param name="indices">The resulting indices for rendering the shape as a triangle list.</param>
		public static void Triangulate(
			TriangulatorVector2[] inputVertices,
			WindingOrder desiredWindingOrder,
			out TriangulatorVector2[] outputVertices,
			out int[] indices)
		{
			Log("\nBeginning triangulation...");

			List<Triangle> triangles = new List<Triangle>();

			//make sure we have our vertices wound properly
			if (DetermineWindingOrder(inputVertices) == WindingOrder.Clockwise)
				outputVertices = ReverseWindingOrder(inputVertices);
			else
				outputVertices = (TriangulatorVector2[])inputVertices.Clone();

			//clear all of the lists
			polygonVertices.Clear();
			earVertices.Clear();
			convexVertices.Clear();
			reflexVertices.Clear();

			//generate the cyclical list of vertices in the polygon
			for (int i = 0; i < outputVertices.Length; i++)
				polygonVertices.AddLast(new Vertex(outputVertices[i], i));

			//categorize all of the vertices as convex, reflex, and ear
			FindConvexAndReflexVertices();
			FindEarVertices();

			//clip all the ear vertices
			while (polygonVertices.Count > 3 && earVertices.Count > 0)
				ClipNextEar(triangles);

			//if there are still three points, use that for the last triangle
			if (polygonVertices.Count == 3)
				triangles.Add(new Triangle(
					polygonVertices[0].Value,
					polygonVertices[1].Value,
					polygonVertices[2].Value));

			//add all of the triangle indices to the output array
			indices = new int[triangles.Count * 3];

			//move the if statement out of the loop to prevent all the
			//redundant comparisons
			if (desiredWindingOrder == WindingOrder.CounterClockwise)
			{
				for (int i = 0; i < triangles.Count; i++)
				{
					indices[i * 3] = triangles[i].A.Index;
					indices[i * 3 + 1] = triangles[i].B.Index;
					indices[i * 3 + 2] = triangles[i].C.Index;
				}
			}
			else
			{
				for (int i = 0; i < triangles.Count; i++)
				{
					indices[i * 3] = triangles[i].C.Index;
					indices[i * 3 + 1] = triangles[i].B.Index;
					indices[i * 3 + 2] = triangles[i].A.Index;
				}
			}
		}

		#endregion

		#region ReverseWindingOrder

		/// <summary>
		/// Reverses the winding order for a set of vertices.
		/// </summary>
		/// <param name="vertices">The vertices of the polygon.</param>
		/// <returns>The new vertices for the polygon with the opposite winding order.</returns>
		public static TriangulatorVector2[] ReverseWindingOrder(TriangulatorVector2[] vertices)
		{
			Log("\nReversing winding order...");
			TriangulatorVector2[] newVerts = new TriangulatorVector2[vertices.Length];

#if DEBUG
			StringBuilder vString = new StringBuilder();
			foreach (TriangulatorVector2 v in vertices)
				vString.Append(string.Format("{0}, ", v));
			Log("Original Vertices: {0}", vString);
#endif

			newVerts[0] = vertices[0];
			for (int i = 1; i < newVerts.Length; i++)
				newVerts[i] = vertices[vertices.Length - i];

#if DEBUG
			vString = new StringBuilder();
			foreach (TriangulatorVector2 v in newVerts)
				vString.Append(string.Format("{0}, ", v));
			Log("New Vertices After Reversal: {0}\n", vString);
#endif

			return newVerts;
		}

		#endregion

		#region DetermineWindingOrder

		/// <summary>
		/// Determines the winding order of a polygon given a set of vertices.
		/// </summary>
		/// <param name="vertices">The vertices of the polygon.</param>
		/// <returns>The calculated winding order of the polygon.</returns>
		public static WindingOrder DetermineWindingOrder(TriangulatorVector2[] vertices)
		{
			int clockWiseCount = 0;
			int counterClockWiseCount = 0;
			TriangulatorVector2 p1 = vertices[0];

			for (int i = 1; i < vertices.Length; i++)
			{
				TriangulatorVector2 p2 = vertices[i];
				TriangulatorVector2 p3 = vertices[(i + 1) % vertices.Length];

				TriangulatorVector2 e1 = p1 - p2;
				TriangulatorVector2 e2 = p3 - p2;

				if (e1.X * e2.Y - e1.Y * e2.X >= 0)
					clockWiseCount++;
				else
					counterClockWiseCount++;

				p1 = p2;
			}

			return clockWiseCount > counterClockWiseCount
				? WindingOrder.Clockwise
				: WindingOrder.CounterClockwise;
		}

		#endregion

		#endregion

		#region Private Methods

		#region ClipNextEar

		private static void ClipNextEar(ICollection<Triangle> triangles)
		{
			//find the triangle
			Vertex ear = earVertices[0].Value;
			Vertex prev = polygonVertices[polygonVertices.IndexOf(ear) - 1].Value;
			Vertex next = polygonVertices[polygonVertices.IndexOf(ear) + 1].Value;
			triangles.Add(new Triangle(ear, next, prev));

			//remove the ear from the shape
			earVertices.RemoveAt(0);
			polygonVertices.RemoveAt(polygonVertices.IndexOf(ear));
			Log("\nRemoved Ear: {0}", ear);

			//validate the neighboring vertices
			ValidateAdjacentVertex(prev);
			ValidateAdjacentVertex(next);

			//write out the states of each of the lists
#if DEBUG
			StringBuilder rString = new StringBuilder();
			foreach (Vertex v in reflexVertices)
				rString.Append(string.Format("{0}, ", v.Index));
			Log("Reflex Vertices: {0}", rString);

			StringBuilder cString = new StringBuilder();
			foreach (Vertex v in convexVertices)
				cString.Append(string.Format("{0}, ", v.Index));
			Log("Convex Vertices: {0}", cString);

			StringBuilder eString = new StringBuilder();
			foreach (Vertex v in earVertices)
				eString.Append(string.Format("{0}, ", v.Index));
			Log("Ear Vertices: {0}", eString);
#endif
		}

		#endregion

		#region ValidateAdjacentVertex

		private static void ValidateAdjacentVertex(Vertex vertex)
		{
			Log("Validating: {0}...", vertex);

			if (reflexVertices.Contains(vertex))
			{
				if (IsConvex(vertex))
				{
					reflexVertices.Remove(vertex);
					convexVertices.Add(vertex);
					Log("Vertex: {0} now convex", vertex);
				}
				else
				{
					Log("Vertex: {0} still reflex", vertex);
				}
			}

			if (convexVertices.Contains(vertex))
			{
				bool wasEar = earVertices.Contains(vertex);
				bool isEar = IsEar(vertex);

				if (wasEar && !isEar)
				{
					earVertices.Remove(vertex);
					Log("Vertex: {0} no longer ear", vertex);
				}
				else if (!wasEar && isEar)
				{
					earVertices.AddFirst(vertex);
					Log("Vertex: {0} now ear", vertex);
				}
				else
				{
					Log("Vertex: {0} still ear", vertex);
				}
			}
		}

		#endregion

		#region FindConvexAndReflexVertices

		private static void FindConvexAndReflexVertices()
		{
			for (int i = 0; i < polygonVertices.Count; i++)
			{
				Vertex v = polygonVertices[i].Value;

				if (IsConvex(v))
				{
					convexVertices.Add(v);
					Log("Convex: {0}", v);
				}
				else
				{
					reflexVertices.Add(v);
					Log("Reflex: {0}", v);
				}
			}
		}

		#endregion

		#region FindEarVertices

		private static void FindEarVertices()
		{
			for (int i = 0; i < convexVertices.Count; i++)
			{
				Vertex c = convexVertices[i];

				if (IsEar(c))
				{
					earVertices.AddLast(c);
					Log("Ear: {0}", c);
				}
			}
		}

		#endregion

		#region IsEar

		private static bool IsEar(Vertex c)
		{
			Vertex p = polygonVertices[polygonVertices.IndexOf(c) - 1].Value;
			Vertex n = polygonVertices[polygonVertices.IndexOf(c) + 1].Value;

			Log("Testing vertex {0} as ear with triangle {1}, {0}, {2}...", c, p, n);

			foreach (Vertex t in reflexVertices)
			{
				if (t.Equals(p) || t.Equals(c) || t.Equals(n))
					continue;

				if (Triangle.ContainsPoint(p, c, n, t))
				{
					Log("\tTriangle contains vertex {0}...", t);
					return false;
				}
			}

			return true;
		}

		#endregion

		#region IsConvex

		private static bool IsConvex(Vertex c)
		{
			Vertex p = polygonVertices[polygonVertices.IndexOf(c) - 1].Value;
			Vertex n = polygonVertices[polygonVertices.IndexOf(c) + 1].Value;

			TriangulatorVector2 d1 = TriangulatorVector2.Normalize(c.Position - p.Position);
			TriangulatorVector2 d2 = TriangulatorVector2.Normalize(n.Position - c.Position);
			TriangulatorVector2 n2 = new TriangulatorVector2(-d2.Y, d2.X);

			return TriangulatorVector2.Dot(d1, n2) <= 0f;
		}

		#endregion

		#region IsReflex

		private static bool IsReflex(Vertex c)
		{
			return !IsConvex(c);
		}

		#endregion

		#region Log

		[Conditional("DEBUG")]
		private static void Log(string format, params object[] parameters)
		{
			Console.WriteLine(format, parameters);
		}

		#endregion

		#endregion
	}

	/// <summary>
	/// Specifies a desired winding order for the shape vertices.
	/// </summary>
	public enum WindingOrder
	{
		Clockwise,
		CounterClockwise
	}
}
