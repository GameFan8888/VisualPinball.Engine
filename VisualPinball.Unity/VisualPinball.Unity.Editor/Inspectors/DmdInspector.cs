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

// ReSharper disable CheckNamespace
// ReSharper disable CompareOfFloatsByEqualityOperator

using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace VisualPinball.Unity.Editor
{
	[CustomEditor(typeof(DmdAuthoring)), CanEditMultipleObjects]
	public class DmdInspector : DisplayInspector
	{
		private DmdAuthoring _mb;

		private void OnEnable()
		{
			_mb = target as DmdAuthoring;
			base.OnEnable();
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			var width = EditorGUILayout.IntField("Columns", _mb.Width);
			if (width != _mb.Width) {
				_mb.Width = width;
			}

			var height = EditorGUILayout.IntField("Rows", _mb.Height);
			if (height != _mb.Height) {
				_mb.Height = height;
			}
		}
	}
}
