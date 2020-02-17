namespace VisualPinball.Engine.Math
{
	public class Rect3D
	{
		public float Left = 0;
		public float Top = 0;
		public float Right = 0;
		public float Bottom = 0;
		public float ZLow = 0;
		public float ZHigh = 0;

		public float Width => MathF.Abs(Left - Right);
		public float Height => MathF.Abs(Top - Bottom);
		public float Depth => MathF.Abs(ZLow - ZHigh);

		public Rect3D()
		{
			Clear();
		}

		public Rect3D(float left, float right, float top, float bottom, float zLow, float zHigh)
		{
			Left = left;
			Right = right;
			Top = top;
			Bottom = bottom;
			ZLow = zLow;
			ZHigh = zHigh;
		}

		public void Clear()
		{
			Left = Constants.FloatMax;
			Right = -Constants.FloatMax;
			Top = Constants.FloatMax;
			Bottom = -Constants.FloatMax;
			ZLow = Constants.FloatMax;
			ZHigh = -Constants.FloatMax;
		}

		public void Extend(Rect3D other)
		{
			Left = MathF.Min(Left, other.Left);
			Right = MathF.Max(Right, other.Right);
			Top = MathF.Min(Top, other.Top);
			Bottom = MathF.Max(Bottom, other.Bottom);
			ZLow = MathF.Min(ZLow, other.ZLow);
			ZHigh = MathF.Max(ZHigh, other.ZHigh);
		}

		public bool IntersectSphere(Vertex3D sphereP, float sphereRsqr)
		{
			var ex = MathF.Max(Left - sphereP.X, 0) + MathF.Max(sphereP.X - Right, 0);
			var ey = MathF.Max(Top - sphereP.Y, 0) + MathF.Max(sphereP.Y - Bottom, 0);
			var ez = MathF.Max(ZLow - sphereP.Z, 0) + MathF.Max(sphereP.Z - ZHigh, 0);
			ex *= ex;
			ey *= ey;
			ez *= ez;
			return ex + ey + ez <= sphereRsqr;
		}

		public bool IntersectRect(Rect3D rc)
		{
			return Right >= rc.Left
			       && Bottom >= rc.Top
			       && Left <= rc.Right
			       && Top <= rc.Bottom
			       && ZLow <= rc.ZHigh
			       && ZHigh >= rc.ZLow;
		}
	}
}
