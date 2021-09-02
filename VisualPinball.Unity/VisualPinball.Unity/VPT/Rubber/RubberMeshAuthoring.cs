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
using UnityEngine;
using VisualPinball.Engine.Game;
using VisualPinball.Engine.VPT.Rubber;
using VisualPinball.Engine.VPT.Table;
using Mesh = VisualPinball.Engine.VPT.Mesh;

namespace VisualPinball.Unity
{
	[ExecuteInEditMode]
	[AddComponentMenu("Visual Pinball/Mesh/Rubber Mesh")]
	public class RubberMeshAuthoring : ItemMeshAuthoring<RubberData, RubberAuthoring>
	{
		public static readonly Type[] ValidParentTypes = Type.EmptyTypes;
		public override IEnumerable<Type> ValidParents => ValidParentTypes;

		protected override RenderObject GetRenderObject(RubberData data, Table table)
			=> new RubberMeshGenerator(MainComponent).GetRenderObject(table, data);
		protected override Mesh GetMesh(RubberData data)
		{
			return new RubberMeshGenerator(MainComponent).GetTransformedMesh(MainComponent.PlayfieldHeight, MainComponent.PlayfieldDetailLevel);
		}
	}
}
