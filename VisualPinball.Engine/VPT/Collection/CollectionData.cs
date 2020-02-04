#region ReSharper
// ReSharper disable UnassignedField.Global
// ReSharper disable StringLiteralTypo
// ReSharper disable FieldCanBeMadeReadOnly.Global
#endregion

using System.Collections.Generic;
using System.IO;
using VisualPinball.Engine.IO;
using VisualPinball.Engine.VPT.Table;

namespace VisualPinball.Engine.VPT.Collection
{
	public class CollectionData : ItemData
	{
		[BiffString("NAME", IsWideString = true)]
		public override string Name { get; set; }

		[BiffString("ITEM")]
		public string ItemNames;

		[BiffBool("EVNT")]
		public bool FireEvents = false;

		[BiffBool("GREL")]
		public bool GroupElements = true;

		[BiffBool("SSNG")]
		public bool StopSingleEvents = false;

		#region BIFF

		static CollectionData()
		{
			Init(typeof(CollectionData), Attributes);
		}

		public CollectionData(BinaryReader reader, string storageName) : base(storageName)
		{
			Load(this, reader, Attributes);
		}

		public override void Write(BinaryWriter writer, HashWriter hashWriter)
		{
			writer.Write(ItemType.Collection);
			Write(writer, Attributes, hashWriter);
			WriteEnd(writer, hashWriter);
		}

		private static readonly Dictionary<string, List<BiffAttribute>> Attributes = new Dictionary<string, List<BiffAttribute>>();

		#endregion
	}
}
