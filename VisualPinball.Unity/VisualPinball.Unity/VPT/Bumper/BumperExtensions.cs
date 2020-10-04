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

using Unity.Entities;
using UnityEngine;
using VisualPinball.Engine.Game;
using VisualPinball.Engine.VPT.Bumper;

namespace VisualPinball.Unity
{
	internal static class BumperExtensions
	{
		public static BumperAuthoring SetupGameObject(this Engine.VPT.Bumper.Bumper bumper, GameObject obj)
		{
			var ic = obj.AddComponent<BumperAuthoring>().SetItem(bumper);

			obj.AddComponent<ConvertToEntity>();

			var bse = obj.transform.Find(BumperMeshGenerator.Base).gameObject;
			var cap = obj.transform.Find(BumperMeshGenerator.Cap).gameObject;
			var ring = obj.transform.Find(BumperMeshGenerator.Ring).gameObject;
			var skirt = obj.transform.Find(BumperMeshGenerator.Skirt).gameObject;

			bse.AddComponent<BumperBaseMeshAuthoring>();
			cap.AddComponent<BumperCapMeshAuthoring>();
			ring.AddComponent<BumperRingMeshAuthoring>();
			skirt.AddComponent<BumperSkirtMeshAuthoring>();
			ring.AddComponent<BumperRingAuthoring>();
			skirt.AddComponent<BumperSkirtAuthoring>();

			return ic as BumperAuthoring;
		}
	}
}
