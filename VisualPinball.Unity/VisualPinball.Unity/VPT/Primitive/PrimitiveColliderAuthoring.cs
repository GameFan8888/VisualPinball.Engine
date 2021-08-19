﻿// Visual Pinball Engine
// Copyright (C) 2021 freezy and VPE Team
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
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using VisualPinball.Engine.VPT.Primitive;

namespace VisualPinball.Unity
{
	[AddComponentMenu("Visual Pinball/Collision/Primitive Collider")]
	public class PrimitiveColliderAuthoring : ItemColliderAuthoring<Primitive, PrimitiveData, PrimitiveAuthoring>
	{
		#region Data

		public bool HitEvent = true;

		public float Threshold = 2f;

		public float Elasticity = 0.3f;

		public float ElasticityFalloff = 0.5f;

		public float Friction = 0.3f;

		public float Scatter;

		public float CollisionReductionFactor = 0;

		public bool OverwritePhysics = true;

		#endregion

		public static readonly Type[] ValidParentTypes = {
			typeof(PrimitiveAuthoring),
			typeof(RubberAuthoring),
			typeof(SurfaceAuthoring)
		};

		public override IEnumerable<Type> ValidParents => ValidParentTypes;
		public override PhysicsMaterialData PhysicsMaterialData => GetPhysicsMaterialData(Elasticity, ElasticityFalloff, Friction, Scatter, OverwritePhysics);
		protected override IApiColliderGenerator InstantiateColliderApi(Player player, Entity entity, Entity parentEntity)
			=> new PrimitiveApi(gameObject, entity, parentEntity, player);
	}
}
