using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using VisualPinball.Engine.Math;

namespace VisualPinball.Unity.VPT.Flipper
{
	public class FlipperDisplacementSystem : JobComponentSystem
	{

		//[BurstCompile]
		private struct FlipperDisplacement : IJobForEach<FlipperMovementData, FlipperMaterialData>
		{
			public float DTime;

			public void Execute(ref FlipperMovementData state, [ReadOnly] ref FlipperMaterialData data)
			{
				var dTime = DTime * PhysicsConstants.DefaultStepTime / PhysicsConstants.PhysicsStepTime;
				// state.Angle += state.AngleSpeed * dTime; // move flipper angle
				//
				// var angleMin = math.min(data.AngleStart, data.AngleEnd);
				// var angleMax = math.max(data.AngleStart, data.AngleEnd);
				//
				// if (state.Angle > angleMax) {
				// 	state.Angle = angleMax;
				// }
				//
				// if (state.Angle < angleMin) {
				// 	state.Angle = angleMin;
				// }

				// if (math.abs(state.AngleSpeed) < 0.0005f) {
				// 	// avoids "jumping balls" when two or more balls held on flipper (and more other balls are in play) //!! make dependent on physics update rate
				// 	return;
				// }
				//
				// var handleEvent = false;
				//
				// if (state.Angle >= angleMax) {
				// 	// hit stop?
				// 	if (state.AngleSpeed > 0) {
				// 		handleEvent = true;
				// 	}
				//
				// } else if (state.Angle <= angleMin) {
				// 	if (state.AngleSpeed < 0) {
				// 		handleEvent = true;
				// 	}
				// }

				// if (handleEvent) {
				// 	var angleSpeed = math.abs(math.degrees(state.AngleSpeed));
				// 	state.AngularMomentum *= -0.3f; // make configurable?
				// 	state.AngleSpeed = state.AngularMomentum / data.Inertia;
				//
				// 	if (state.EnableRotateEvent > 0) {
				// 		//_events.FireVoidEventParam(Event.LimitEventsEOS, angleSpeed); // send EOS event
				//
				// 	} else if (state.EnableRotateEvent < 0) {
				// 		//_events.FireVoidEventParam(Event.LimitEventsBOS, angleSpeed); // send Beginning of Stroke/Park event
				// 	}
				//
				// 	state.EnableRotateEvent = 0;
				// }
			}
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var flipperDisplacementJob = new FlipperDisplacement {
				DTime = Time.DeltaTime
			};
			return flipperDisplacementJob.Schedule(this, inputDeps);
		}
	}
}
