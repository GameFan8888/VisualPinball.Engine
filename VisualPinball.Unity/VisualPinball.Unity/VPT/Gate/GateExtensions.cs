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
using VisualPinball.Engine.Resources.Meshes;
using VisualPinball.Engine.VPT;
using VisualPinball.Engine.VPT.Gate;

namespace VisualPinball.Unity
{
	internal static class GateExtensions
	{
		public static IItemMainAuthoring SetupGameObject(this Gate gate, GameObject obj, IItemMainAuthoring parentAuthoring)
		{
			var mainAuthoring = obj.AddComponent<GateAuthoring>().SetItem(gate);

			switch (gate.SubComponent) {
				case ItemSubComponent.None:
					obj.AddColliderComponent(gate);
					CreateChild<GateBracketMeshAuthoring>(obj, GateMeshGenerator.Bracket);
					var wire = CreateChild<GateWireMeshAuthoring>(obj, GateMeshGenerator.Wire);
					wire.AddComponent<GateWireAnimationAuthoring>();
					break;

				case ItemSubComponent.Collider: {
					obj.AddColliderComponent(gate);
					if (parentAuthoring != null && parentAuthoring is IItemMainAuthoring parentMainAuthoring) {
						parentMainAuthoring.DestroyColliderComponent();
					}
					break;
				}

				case ItemSubComponent.Mesh: {
					// todo
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

		private static void AddColliderComponent(this GameObject obj, Gate gate)
		{
			if (gate.Data.IsCollidable) {
				obj.AddComponent<GateColliderAuthoring>();
			}
		}

		private static GameObject CreateChild<T>(GameObject obj, string name) where T : MonoBehaviour, IItemMeshAuthoring
		{
			var subObj = new GameObject(name);
			subObj.transform.SetParent(obj.transform, false);
			subObj.AddComponent<T>();
			//subObj.layer = ChildObjectsLayer;
			return subObj;
		}
	}
}
