// Visual Pinball Engine
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

using Unity.Collections;
using Unity.Mathematics;
using VisualPinball.Engine.Game;
using VisualPinball.Unity;

namespace VisualPinballUnity
{
	internal static class PlungerDisplacementPhysics
	{
		internal static void UpdateDisplacement(int itemId, ref PlungerMovementData movementData,
			ref PlungerColliderData colliderData, in PlungerStaticData staticData, float dTime,
			ref NativeQueue<EventData>.ParallelWriter events)
		{
			// figure the travel distance
			var dx = dTime * movementData.Speed;

			// figure the position change
			movementData.Position += dx;

			// apply the travel limit
			if (movementData.Position < movementData.TravelLimit) {
				movementData.Position = movementData.TravelLimit;
			}

			// if we're in firing mode and we've crossed the bounce position, reverse course
			var relPos = (movementData.Position - staticData.FrameEnd) / staticData.FrameLen;
			var bouncePos = staticData.RestPosition + movementData.FireBounce;
			if (movementData.FireTimer != 0 && dTime != 0.0f &&
			    (movementData.FireSpeed < 0.0f ? relPos <= bouncePos : relPos >= bouncePos))
			{
				// stop at the bounce position
				movementData.Position = staticData.FrameEnd + bouncePos * staticData.FrameLen;

				// reverse course at reduced speed
				movementData.FireSpeed = -movementData.FireSpeed * 0.4f;

				// figure the new bounce as a fraction of the previous bounce
				movementData.FireBounce *= -0.4f;
			}

			// apply the travel limit (again)
			if (movementData.Position < movementData.TravelLimit) {
				movementData.Position = movementData.TravelLimit;
			}

			// limit motion to the valid range
			if (dTime != 0.0f) {

				if (movementData.Position < staticData.FrameEnd) {
					movementData.Speed = 0.0f;
					movementData.Position = staticData.FrameEnd;

				} else if (movementData.Position > staticData.FrameStart) {
					movementData.Speed = 0.0f;
					movementData.Position = staticData.FrameStart;
				}

				// apply the travel limit (yet again)
				if (movementData.Position < movementData.TravelLimit) {
					movementData.Position = movementData.TravelLimit;
				}
			}

			// the travel limit applies to one displacement update only - reset it
			movementData.TravelLimit = staticData.FrameEnd;

			// fire an Start/End of Stroke events, as appropriate
			var strokeEventLimit = staticData.FrameLen / 50.0f;
			var strokeEventHysteresis = strokeEventLimit * 2.0f;
			if (movementData.StrokeEventsArmed && movementData.Position + dx > staticData.FrameStart - strokeEventLimit) {
				events.Enqueue(new EventData(EventId.LimitEventsBos, itemId, math.abs(movementData.Speed)));
				movementData.StrokeEventsArmed = false;

			} else if (movementData.StrokeEventsArmed && movementData.Position + dx < staticData.FrameEnd + strokeEventLimit) {
				events.Enqueue(new EventData(EventId.LimitEventsEos, itemId, math.abs(movementData.Speed)));
				movementData.StrokeEventsArmed = false;

			} else if (movementData.Position > staticData.FrameEnd + strokeEventHysteresis && movementData.Position < staticData.FrameStart - strokeEventHysteresis) {
				// away from the limits - arm the stroke events
				movementData.StrokeEventsArmed = true;
			}

			// update the display
			UpdateCollider(movementData.Position, ref colliderData);
		}

		private static void UpdateCollider(float len, ref PlungerColliderData colliderData)
		{
			colliderData.LineSegSide0.V1y = len;
			colliderData.LineSegSide1.V2y = len;

			colliderData.LineSegEnd.V2y = len;
			colliderData.LineSegEnd.V1y = len; // + 0.0001f;

			colliderData.JointEnd0.XyY = len;
			colliderData.JointEnd1.XyY = len; // + 0.0001f;

			colliderData.LineSegSide0.CalcNormal();
			colliderData.LineSegSide1.CalcNormal();
			colliderData.LineSegEnd.CalcNormal();
		}
	}
}