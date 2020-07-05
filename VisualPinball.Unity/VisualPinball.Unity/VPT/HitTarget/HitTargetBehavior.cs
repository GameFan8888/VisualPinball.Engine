#region ReSharper
// ReSharper disable CompareOfFloatsByEqualityOperator
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBePrivate.Global
#endregion

using Unity.Entities;
using UnityEngine;
using VisualPinball.Engine.VPT.HitTarget;
using VisualPinball.Unity.Extensions;

namespace VisualPinball.Unity.VPT.HitTarget
{
	[AddComponentMenu("Visual Pinball/Hit Target")]
	public class HitTargetBehavior : ItemBehavior<Engine.VPT.HitTarget.HitTarget, HitTargetData>, IConvertGameObjectToEntity
	{
		protected override string[] Children => null;

		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			Convert(entity, dstManager);
		}

		protected override Engine.VPT.HitTarget.HitTarget GetItem()
		{
			return new Engine.VPT.HitTarget.HitTarget(data);
		}

		public override ItemDataTransformType EditorPositionType => ItemDataTransformType.ThreeD;
		public override Vector3 GetEditorPosition() => data.Position.ToUnityVector3();
		public override void SetEditorPosition(Vector3 pos) => data.Position = pos.ToVertex3D();

		public override ItemDataTransformType EditorRotationType => ItemDataTransformType.OneD;
		public override Vector3 GetEditorRotation() => new Vector3(data.RotZ, 0f, 0f);
		public override void SetEditorRotation(Vector3 rot) => data.RotZ = rot.x;

		public override ItemDataTransformType EditorScaleType => ItemDataTransformType.ThreeD;
		public override Vector3 GetEditorScale() => data.Size.ToUnityVector3();
		public override void SetEditorScale(Vector3 scale) => data.Size = scale.ToVertex3D();

		public override void HandleMaterialRenamed(string undoName, string oldName, string newName)
		{
			TryRenameField(undoName, ref data.Material, oldName, newName);
			TryRenameField(undoName, ref data.PhysicsMaterial, oldName, newName);
		}
	}
}
