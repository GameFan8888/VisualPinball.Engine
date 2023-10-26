﻿// Visual Pinball Engine
// Copyright (C) 2023 freezy and VPE Team
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.

using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace VisualPinball.Unity
{
	public static class PhysicsColliderExtensions
	{
		internal static int GetId(this BlobAssetReference<ColliderBlob> colliders, int index) 
			=> colliders.Value.Colliders[index].Value.Id;

		internal static ColliderType GetType(this BlobAssetReference<ColliderBlob> colliders, int index) 
			=> colliders.Value.Colliders[index].Value.Type;

		internal static float GetFriction(this BlobAssetReference<ColliderBlob> colliders, int index) 
			=> colliders.Value.Colliders[index].Value.Material.Friction;

		internal static Aabb GetAabb(this BlobAssetReference<ColliderBlob> colliders, int index) 
			=> colliders.Value.Colliders[index].Value.Bounds().Aabb;

		internal static unsafe ref CircleCollider Circle(this in BlobAssetReference<ColliderBlob> colliders, int index)
		{
			ref var coll = ref colliders.Value.Colliders[index].Value;
			fixed (Collider* cPtr = &coll) {
				var circleCollider = (CircleCollider*) cPtr;
				return ref UnsafeUtility.AsRef<CircleCollider>(circleCollider);
			}
		}
		
		internal static unsafe ref PlaneCollider Plane(this in BlobAssetReference<ColliderBlob> colliders, int index)
		{
			ref var coll = ref colliders.Value.Colliders[index].Value;
			fixed (Collider* cPtr = &coll) {
				var planeCollider = (PlaneCollider*) cPtr;
				return ref UnsafeUtility.AsRef<PlaneCollider>(planeCollider);
			}
		}

		internal static unsafe ref SpinnerCollider Spinner(this in BlobAssetReference<ColliderBlob> colliders, int index)
		{
			ref var coll = ref colliders.Value.Colliders[index].Value;
			fixed (Collider* cPtr = &coll) {
				var spinnerCollider = (SpinnerCollider*) cPtr;
				return ref UnsafeUtility.AsRef<SpinnerCollider>(spinnerCollider);
			}
		}

		internal static unsafe ref GateCollider Gate(this in BlobAssetReference<ColliderBlob> colliders, int index)
		{
			ref var coll = ref colliders.Value.Colliders[index].Value;
			fixed (Collider* cPtr = &coll) {
				var gateCollider = (GateCollider*) cPtr;
				return ref UnsafeUtility.AsRef<GateCollider>(gateCollider);
			}
		}

		internal static unsafe ref LineCollider Line(this in BlobAssetReference<ColliderBlob> colliders, int index)
		{
			ref var coll = ref colliders.Value.Colliders[index].Value;
			fixed (Collider* cPtr = &coll) {
				var lineCollider = (LineCollider*) cPtr;
				return ref UnsafeUtility.AsRef<LineCollider>(lineCollider);
			}
		}

		internal static unsafe ref TriangleCollider Triangle(this in BlobAssetReference<ColliderBlob> colliders, int index)
		{
			ref var coll = ref colliders.Value.Colliders[index].Value;
			fixed (Collider* cPtr = &coll) {
				var triangleCollider = (TriangleCollider*) cPtr;
				return ref UnsafeUtility.AsRef<TriangleCollider>(triangleCollider);
			}
		}

		internal static unsafe ref Line3DCollider Line3D(this in BlobAssetReference<ColliderBlob> colliders, int index)
		{
			ref var coll = ref colliders.Value.Colliders[index].Value;
			fixed (Collider* cPtr = &coll) {
				var line3DCollider = (Line3DCollider*) cPtr;
				return ref UnsafeUtility.AsRef<Line3DCollider>(line3DCollider);
			}
		}

		internal static unsafe ref LineSlingshotCollider LineSlingShot(this in BlobAssetReference<ColliderBlob> colliders, int index)
		{
			ref var coll = ref colliders.Value.Colliders[index].Value;
			fixed (Collider* cPtr = &coll) {
				var lineSlingshotCollider = (LineSlingshotCollider*) cPtr;
				return ref UnsafeUtility.AsRef<LineSlingshotCollider>(lineSlingshotCollider);
			}
		}

		internal static unsafe ref PointCollider Point(this in BlobAssetReference<ColliderBlob> colliders, int index)
		{
			ref var coll = ref colliders.Value.Colliders[index].Value;
			fixed (Collider* cPtr = &coll) {
				var pointCollider = (PointCollider*) cPtr;
				return ref UnsafeUtility.AsRef<PointCollider>(pointCollider);
			}
		}

		internal static unsafe ref LineZCollider LineZ(this in BlobAssetReference<ColliderBlob> colliders, int index)
		{
			ref var coll = ref colliders.Value.Colliders[index].Value;
			fixed (Collider* cPtr = &coll) {
				var lineZCollider = (LineZCollider*) cPtr;
				return ref UnsafeUtility.AsRef<LineZCollider>(lineZCollider);
			}
		}

		internal static unsafe ref FlipperCollider Flipper(this in BlobAssetReference<ColliderBlob> colliders, int index)
		{
			ref var coll = ref colliders.Value.Colliders[index].Value;
			fixed (Collider* cPtr = &coll) {
				var flipperCollider = (FlipperCollider*) cPtr;
				return ref UnsafeUtility.AsRef<FlipperCollider>(flipperCollider);
			}
		}

		internal static unsafe ref PlungerCollider Plunger(this in BlobAssetReference<ColliderBlob> colliders, int index)
		{
			ref var coll = ref colliders.Value.Colliders[index].Value;
			fixed (Collider* cPtr = &coll) {
				var plungerCollider = (PlungerCollider*) cPtr;
				return ref UnsafeUtility.AsRef<PlungerCollider>(plungerCollider);
			}
		}
	}
}
