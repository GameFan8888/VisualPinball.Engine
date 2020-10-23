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

// ReSharper disable AssignmentInConditionalExpression

using UnityEditor;
using VisualPinball.Engine.VPT.Flipper;
using VisualPinball.Engine.VPT.Surface;

namespace VisualPinball.Unity.Editor
{
	[CustomEditor(typeof(FlipperColliderAuthoring))]
	public class FlipperColliderInspector : ItemColliderInspector<Flipper, FlipperData, FlipperAuthoring, FlipperColliderAuthoring>
	{
		private FlipperData _flipperData;

		protected override void OnEnable()
		{
			base.OnEnable();
			_flipperData = Data;
		}

		public override void OnInspectorGUI()
		{
			if (_flipperData == null) {
				NoDataPanel();
				return;
			}

			ItemDataField("Mass", ref _flipperData.Mass, false);
			ItemDataField("Strength", ref _flipperData.Strength, false);
			ItemDataField("Elasticity", ref _flipperData.Elasticity, false);
			ItemDataField("Elasticity Falloff", ref _flipperData.ElasticityFalloff, false);
			ItemDataField("Friction", ref _flipperData.Friction, false);
			ItemDataField("Return Strength", ref _flipperData.Return, false);
			ItemDataField("Coil Ramp Up", ref _flipperData.RampUp, false);
			ItemDataField("Scatter Angle", ref _flipperData.Scatter, false);
			ItemDataField("EOS Torque", ref _flipperData.TorqueDamping, false);
			ItemDataField("EOS Torque Angle", ref _flipperData.TorqueDampingAngle, false);

			base.OnInspectorGUI();
		}
	}
}
