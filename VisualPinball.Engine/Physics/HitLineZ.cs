﻿using VisualPinball.Engine.Common;
using VisualPinball.Engine.Game;
using VisualPinball.Engine.Math;
using VisualPinball.Engine.VPT;
using VisualPinball.Engine.VPT.Ball;

namespace VisualPinball.Engine.Physics
{
	public class HitLineZ : HitObject
	{
		public readonly Vertex2D Xy;

		protected HitLineZ(Vertex2D xy, ItemType itemType) : base(itemType)
		{
			Xy = xy;
		}

		public HitLineZ(Vertex2D xy, float zLow, float zHigh, ItemType itemType) : this(xy, itemType)
		{
			HitBBox.ZLow = zLow;
			HitBBox.ZHigh = zHigh;
		}

		public HitLineZ Set(float x, float y)
		{
			Xy.X = x;
			Xy.Y = y;
			return this;
		}

		public override void CalcHitBBox()
		{
			HitBBox.Left = Xy.X;
			HitBBox.Right = Xy.X;
			HitBBox.Top = Xy.Y;
			HitBBox.Bottom = Xy.Y;

			// zlow and zhigh set in ctor
		}
	}
}
