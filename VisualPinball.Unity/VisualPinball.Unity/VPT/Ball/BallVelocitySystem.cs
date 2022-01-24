// Visual Pinball Engine
// Copyright (C) 2022 freezy and VPE Team
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

// ReSharper disable CompareOfFloatsByEqualityOperator

using Unity.Entities;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;
using VisualPinball.Engine.Common;

namespace VisualPinball.Unity
{
	[UpdateInGroup(typeof(UpdateVelocitiesSystemGroup))]
	internal class BallVelocitySystem : SystemBase
	{
		private float3 _gravity;
		private static readonly ProfilerMarker PerfMarker = new ProfilerMarker("BallVelocitySystem");

		protected override void OnStartRunning()
		{
			_gravity = Object.FindObjectOfType<Player>().Gravity;
		}

		protected override void OnUpdate()
		{
			var gravity = _gravity;
			var marker = PerfMarker;
			Entities.WithName("BallVelocityJob").ForEach((ref BallData ball) => {

				if (ball.IsFrozen) {
					return;
				}

				marker.Begin();

				if (ball.ManualControl) {
					ball.Velocity *= 0.5f; // Null out most of the X/Y velocity, want a little bit so the ball can sort of find its way out of obstacles.
					ball.Velocity += new float3(
						math.max(-10.0f, math.min(10.0f, (ball.ManualPosition.x - ball.Position.x) * (float)(1.0/10.0))),
						math.max(-10.0f, math.min(10.0f, (ball.ManualPosition.y - ball.Position.y) * (float)(1.0/10.0))),
						-2.0f
					);
				} else {
					ball.Velocity += gravity * (float)PhysicsConstants.PhysFactor;
				}

				marker.End();

			}).Run();
		}
	}
}
