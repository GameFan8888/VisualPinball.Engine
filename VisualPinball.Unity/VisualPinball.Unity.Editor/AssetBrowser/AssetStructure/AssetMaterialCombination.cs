﻿// Visual Pinball Engine
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

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VisualPinball.Unity.Editor
{
	public class AssetMaterialCombination
	{
		public string Name => string.Join(", ", Variations.Select(v => $"{v.Item2.Name} {v.Item1.Name}"));

		public string ThumbId => GenerateThumbID();

		public readonly Asset Asset;
		public readonly (AssetMaterialVariation, AssetMaterialOverride)[] Variations;

		private string _thumbId;


		public AssetMaterialCombination(Asset asset)
		{
			Asset = asset;
			Variations = Array.Empty<(AssetMaterialVariation, AssetMaterialOverride)>();
		}

		public static IEnumerable<AssetMaterialCombination> GetCombinations(Asset asset)
		{
			var variations = asset.MaterialVariations;
			var counters = new Counter[variations.Count];
			Counter nextCounter = null;
			for (var i = variations.Count - 1; i >= 0; i--) {
				counters[i] = new Counter(variations[i].Overrides.Count, nextCounter);
				nextCounter = counters[i];
			}

			var combinations = new List<AssetMaterialCombination>();
			if (counters.Length == 0) {
				return combinations;
			}
			do {
				combinations.Add(new AssetMaterialCombination(asset, counters, variations));
			} while (counters[0].Increase());

			return combinations;
		}

		public void Apply(GameObject go)
		{
			foreach (var (materialVariation, materialOverride) in Variations) {
				var obj = go!.transform.Find(materialVariation.Object.name);
				var materials = obj.gameObject.GetComponent<MeshRenderer>().sharedMaterials;
				materials[materialVariation.Slot] = materialOverride.Material;
				obj.gameObject.GetComponent<MeshRenderer>().sharedMaterials = materials;
			}
		}

		/// <summary>
		/// We just munch all the override guids together on top of the asset guid.
		/// </summary>
		/// <returns></returns>
		private string GenerateThumbID()
		{
			if (_thumbId != null) {
				return _thumbId;
			}

			if (Variations.Length == 0) {
				_thumbId = Asset.GUID;

			} else {
				const int byteCount = 16;
				var guid1 = new Guid(Asset.GUID);
				foreach (var (v, o) in Variations) {
					var guid2 = new Guid(o.Id);
					var destByte = new byte[byteCount];
					var guid1Byte = guid1.ToByteArray();
					var guid2Byte = guid2.ToByteArray();
					for (var i = 0; i < byteCount; i++) {
						destByte[i] = (byte) (guid1Byte[i] ^ guid2Byte[i]);
					}
					guid1 = new Guid(destByte);
				}
				_thumbId = guid1.ToString();
			}
			return _thumbId;
		}

		private AssetMaterialCombination(Asset asset, IReadOnlyList<Counter> counters, IReadOnlyList<AssetMaterialVariation> variations)
		{
			Asset = asset;
			Variations = new (AssetMaterialVariation, AssetMaterialOverride)[counters.Count];
			for (var i = 0; i < counters.Count; i++) {
				var overrideIndex = counters[i].Value;
				Variations[i] = (
					overrideIndex == 0 ? null : variations[i],
					overrideIndex == 0 ? null : variations[i].Overrides[overrideIndex - 1]
				);
			}
			Variations = Variations.Where(mv => mv.Item1 != null).ToArray();
		}

		private class Counter
		{
			public int Value;
			private readonly int _size;
			private readonly Counter _nextCounter;

			public Counter(int size, Counter nextCounter)
			{
				_size = size;
				_nextCounter = nextCounter;
			}

			public bool Increase()
			{
				if (Value == _size) {
					if (_nextCounter != null) {
						Value = 0;
						return _nextCounter.Increase();
					}
					return false;
				}
				Value++;
				return true;
			}
		}

		public override string ToString() => Name;
	}
}
