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

// ReSharper disable BuiltInTypeReferenceStyle

using System;

namespace VisualPinball.Engine.Game.Engines
{
	public class GamelogicEngineSwitch
	{
		public readonly string Id;
		public readonly int InternalId;
		public bool NormallyClosed;
		public string Description;
		public string InputActionHint;
		public string InputMapHint;
		public string PlayfieldItemHint;
		public string DeviceHint;
		public string DeviceItemHint;
		public Boolean ConstantHint;

		public GamelogicEngineSwitch(string id)
		{
			Id = id;
			InternalId = int.TryParse(id, out var internalId) ? internalId : 0;
		}

		public GamelogicEngineSwitch(string id, int internalId)
		{
			Id = id;
			InternalId = internalId;
		}
	}
}
