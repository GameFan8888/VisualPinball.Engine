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

// ReSharper disable AssignmentInConditionalExpression

using UnityEditor;
using VisualPinball.Engine.VPT.HitTarget;

namespace VisualPinball.Unity.Editor
{
	[CustomEditor(typeof(DropTargetAnimationAuthoring))]
	public class HitAnimationInspector : ItemAnimationInspector<HitTarget, HitTargetData, HitTargetAuthoring, DropTargetAnimationAuthoring>
	{
		private SerializedProperty _isDroppedProperty;
		private SerializedProperty _dropSpeedProperty;
		private SerializedProperty _raiseDelayProperty;

		protected override void OnEnable()
		{
			base.OnEnable();

			_isDroppedProperty = serializedObject.FindProperty(nameof(DropTargetAnimationAuthoring.IsDropped));
			_dropSpeedProperty = serializedObject.FindProperty(nameof(DropTargetAnimationAuthoring.DropSpeed));
			_raiseDelayProperty = serializedObject.FindProperty(nameof(DropTargetAnimationAuthoring.RaiseDelay));
		}

		public override void OnInspectorGUI()
		{
			if (HasErrors()) {
				return;
			}

			serializedObject.Update();

			OnPreInspectorGUI();

			PropertyField(_isDroppedProperty, updateTransforms: true);
			PropertyField(_dropSpeedProperty, updateTransforms: true);
			PropertyField(_raiseDelayProperty, updateTransforms: true);

			base.OnInspectorGUI();

			serializedObject.ApplyModifiedProperties();
		}
	}
}
