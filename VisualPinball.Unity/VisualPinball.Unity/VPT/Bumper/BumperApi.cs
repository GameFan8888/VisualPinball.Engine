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
using Unity.Entities;
using VisualPinball.Engine.VPT.Bumper;

namespace VisualPinball.Unity
{
	public class BumperApi : ItemApi<Bumper, BumperData>, IApiInitializable, IApiHittable, IApiSwitch, IApiCoil
	{
		/// <summary>
		/// Event emitted when the table is started.
		/// </summary>
		public event EventHandler Init;

		/// <summary>
		/// Event emitted when the ball hits the bumper.
		/// </summary>
		public event EventHandler<HitEventArgs> Hit;

		/// <summary>
		/// Event emitted when the trigger is switched on or off.
		/// </summary>
		public event EventHandler<SwitchEventArgs> Switch;

		public BumperApi(Bumper item, Entity entity, Player player) : base(item, entity, player)
		{
		}

		IApiSwitchStatus IApiSwitch.AddSwitchDest(SwitchConfig switchConfig) => AddSwitchDest(switchConfig.WithPulse(Item.IsPulseSwitch));
		void IApiSwitch.AddWireDest(WireDestConfig wireConfig) => AddWireDest(wireConfig.WithPulse(Item.IsPulseSwitch));
		void IApiSwitch.DestroyBall(Entity ballEntity) => DestroyBall(ballEntity);

		void IApiCoil.OnCoil(bool enabled, bool _)
		{
			if (enabled) {
				var bumperData = EntityManager.GetComponentData<BumperStaticData>(Entity);
				var ringAnimation = EntityManager.GetComponentData<BumperRingAnimationData>(bumperData.RingEntity);
				ringAnimation.IsHit = true;
				EntityManager.SetComponentData(bumperData.RingEntity, ringAnimation);
			}
		}

		void IApiWireDest.OnChange(bool enabled) => (this as IApiCoil).OnCoil(enabled, false);

		#region Events

		void IApiInitializable.OnInit(BallManager ballManager)
		{
			base.OnInit(ballManager);
			Init?.Invoke(this, EventArgs.Empty);
		}

		void IApiHittable.OnHit(Entity ballEntity, bool isUnHit)
		{
			Hit?.Invoke(this, new HitEventArgs(ballEntity));
			Switch?.Invoke(this, new SwitchEventArgs(!isUnHit, ballEntity));
			OnSwitch(true);
		}

		#endregion
	}
}
