﻿// Visual Pinball Engine
// Copyright (C) 2020 freezy and VPE Team
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

using System;
using Unity.Entities;
using UnityEngine;
using VisualPinball.Engine.VPT;
using VisualPinball.Engine.VPT.Primitive;

namespace VisualPinball.Unity
{
	internal static class PrimitiveExtensions
	{
		public static IItemMainAuthoring SetupGameObject(this Primitive primitive, GameObject obj, IItemMainAuthoring parentAuthoring)
		{
			var mainAuthoring = obj.AddComponent<PrimitiveAuthoring>().SetItem(primitive);

			switch (primitive.SubComponent) {
				case ItemSubComponent.None:
					obj.AddMeshComponent(primitive);
					obj.AddColliderComponent(primitive);
					break;

				case ItemSubComponent.Collider: {
					obj.AddColliderComponent(primitive);
					if (parentAuthoring != null && parentAuthoring is IItemMainAuthoring parentMainAuthoring) {
						parentMainAuthoring.DestroyColliderComponent();
					}
					break;
				}

				case ItemSubComponent.Mesh: {
					obj.AddMeshComponent(primitive);
					if (parentAuthoring != null && parentAuthoring is IItemMainAuthoring parentMainAuthoring) {
						parentMainAuthoring.DestroyMeshComponent();
					}
					break;
				}

				default:
					throw new ArgumentOutOfRangeException();
			}
			obj.AddComponent<ConvertToEntity>();
			return mainAuthoring;
		}

		private static void AddMeshComponent(this GameObject obj, Primitive primitive)
		{
			var comp = obj.AddComponent<PrimitiveMeshAuthoring>();
			comp.enabled = primitive.Data.IsVisible;
		}

		private static void AddColliderComponent(this GameObject obj, Primitive primitive)
		{
			if (!primitive.Data.IsToy && primitive.IsCollidable) { // todo handle dynamic collision
				obj.AddComponent<PrimitiveColliderAuthoring>();
			}
		}
	}
}
