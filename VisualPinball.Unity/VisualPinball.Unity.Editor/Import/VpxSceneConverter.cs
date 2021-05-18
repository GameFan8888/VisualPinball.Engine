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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using UnityEditor;
using UnityEngine;
using VisualPinball.Engine.Game;
using VisualPinball.Engine.VPT;
using VisualPinball.Engine.VPT.Bumper;
using VisualPinball.Engine.VPT.Flipper;
using VisualPinball.Engine.VPT.Gate;
using VisualPinball.Engine.VPT.HitTarget;
using VisualPinball.Engine.VPT.Kicker;
using VisualPinball.Engine.VPT.Plunger;
using VisualPinball.Engine.VPT.Primitive;
using VisualPinball.Engine.VPT.Ramp;
using VisualPinball.Engine.VPT.Rubber;
using VisualPinball.Engine.VPT.Spinner;
using VisualPinball.Engine.VPT.Surface;
using VisualPinball.Engine.VPT.Table;
using VisualPinball.Engine.VPT.Trigger;
using VisualPinball.Engine.VPT.Trough;
using VisualPinball.Unity.Playfield;
using Light = VisualPinball.Engine.VPT.Light.Light;
using Logger = NLog.Logger;
using Material = UnityEngine.Material;
using Texture = UnityEngine.Texture;

namespace VisualPinball.Unity.Editor
{
	public class VpxSceneConverter : ITextureProvider, IMaterialProvider
	{
		private readonly Table _table;
		private GameObject _tableGo;
		private TableAuthoring _tableAuthoring;

		private GameObject _playfieldGo;

		private string _assetsTextures;
		private string _assetsMaterials;

		private readonly Dictionary<string, GameObject> _groupParents = new Dictionary<string, GameObject>();
		private readonly Dictionary<string, Texture> _textures = new Dictionary<string, Texture>();
		private readonly Dictionary<string, Material> _materials = new Dictionary<string, Material>();

		private readonly IPatcher _patcher;

		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
		private static readonly Quaternion GlobalRotation = Quaternion.Euler(-90, 0, 0);
		public const float GlobalScale = 0.001f;

		public VpxSceneConverter(Table table, string fileName)
		{
			_table = table;
			_patcher = PatcherManager.GetPatcher();
			_patcher?.SetTable(_table, fileName);
		}

		public GameObject Convert(string tableName = null)
		{
			CreateRootHierarchy(tableName);

			try {
				// pause asset database refreshing
				AssetDatabase.StartAssetEditing();
				CreateFileHierarchy();

				ExtractTextures();

				ConvertGameItems();

			} finally {

				// resume asset database refreshing
				AssetDatabase.StopAssetEditing();
				AssetDatabase.Refresh();
			}

			return _tableGo;
		}

		private void ConvertGameItems()
		{
			var convertedItems = new Dictionary<string, ConvertedItem>();
			var renderableLookup = new Dictionary<string, IRenderable>();
			var renderables = from renderable in _table.Renderables
				orderby renderable.SubComponent
				select renderable;

			foreach (var renderable in renderables) {

				_patcher?.ApplyPrePatches(renderable);

				var lookupName = renderable.Name.ToLower();
				renderableLookup[lookupName] = renderable;

				if (renderable.SubComponent == ItemSubComponent.None) {
					// create object(s)
					convertedItems[lookupName] = CreateGameObjects(_table, renderable);

				} else {
					// if the object's names was parsed to be part of another object, re-link to other object.
					var parentName = renderable.ComponentName.ToLower();
					if (convertedItems.ContainsKey(parentName)) {
						var parent = convertedItems[parentName];

						var convertedItem = CreateGameObjects(_table, renderable);
						if (convertedItem.IsValidChild(parent)) {

							if (convertedItem.MeshAuthoring.Any()) {

								// move and rotate into parent
								if (parent.MainAuthoring.IItem is IRenderable parentRenderable) {
									renderable.Position -= parentRenderable.Position;
									renderable.RotationY -= parentRenderable.RotationY;
								}

								parent.DestroyMeshComponent();
							}
							if (convertedItem.ColliderAuthoring != null) {
								parent.DestroyColliderComponent();
							}
							convertedItem.MainAuthoring.gameObject.transform.SetParent(parent.MainAuthoring.gameObject.transform, false);
							convertedItems[lookupName] = convertedItem;

						} else {

							renderable.DisableSubComponent();

							// invalid parenting, re-convert the item, because it returned only the sub component.
							convertedItems[lookupName] = CreateGameObjects(_table, renderable);

							// ..and destroy the other one
							convertedItem.Destroy();
						}

					} else {
						Logger.Warn($"Cannot find component \"{parentName}\" that is supposed to be the parent of \"{renderable.Name}\".");
					}
				}
			}

			// now we have all renderables imported, patch them.
			foreach (var lookupName in convertedItems.Keys) {
				foreach (var meshMb in convertedItems[lookupName].MeshAuthoring) {
					_patcher?.ApplyPatches(renderableLookup[lookupName], meshMb.gameObject, _tableGo);
				}
			}

			// convert non-renderables
			foreach (var item in _table.NonRenderables) {

				// create object(s)
				CreateGameObjects(_table, item);
			}
		}

		public ConvertedItem CreateGameObjects(Table table, IItem item)
		{
			var parentGo = GetGroupParent(item);
			var itemGo = new GameObject(item.Name);
			itemGo.transform.parent = parentGo.transform;

			var importedObject = SetupGameObjects(item, itemGo);
			foreach (var meshAuthoring in importedObject.MeshAuthoring) {
				meshAuthoring.CreateMesh(this, this);
			}

			// apply transformation
			if (item is IRenderable renderable) {
				itemGo.transform.SetFromMatrix(renderable.TransformationMatrix(table, Origin.Original).ToUnityMatrix());
			}

			return importedObject;
		}

		private static ConvertedItem SetupGameObjects(IItem item, GameObject obj)
		{
			switch (item) {
				case Bumper bumper:             return bumper.SetupGameObject(obj);
				case Flipper flipper:           return flipper.SetupGameObject(obj);
				case Gate gate:                 return gate.SetupGameObject(obj);
				case HitTarget hitTarget:       return hitTarget.SetupGameObject(obj);
				case Kicker kicker:             return kicker.SetupGameObject(obj);
				case Light lt:                  return lt.SetupGameObject(obj);
				case Plunger plunger:           return plunger.SetupGameObject(obj);
				case Primitive primitive:       return primitive.SetupGameObject(obj);
				case Ramp ramp:                 return ramp.SetupGameObject(obj);
				case Rubber rubber:             return rubber.SetupGameObject(obj);
				case Spinner spinner:           return spinner.SetupGameObject(obj);
				case Surface surface:           return surface.SetupGameObject(obj);
				case Table table:               return table.SetupGameObject(obj);
				case Trigger trigger:           return trigger.SetupGameObject(obj);
				case Trough trough:             return trough.SetupGameObject(obj);
			}

			throw new InvalidOperationException("Unknown item " + item + " to setup!");
		}

		private void ExtractTextures()
		{
			foreach (var texture in _table.Textures) {
				var path = texture.GetUnityFilename(_assetsTextures);
				File.WriteAllBytes(path, texture.Content);
				var unityTexture = texture.IsHdr
					? (Texture)AssetDatabase.LoadAssetAtPath<Cubemap>(path)
					: AssetDatabase.LoadAssetAtPath<Texture2D>(path);
				_textures[texture.Name.ToLower()] = unityTexture;
			}
		}

		private void CreateFileHierarchy()
		{
			if (!Directory.Exists("Assets/Tables/")) {
				Directory.CreateDirectory("Assets/Tables/");
			}

			var assetsTableRoot = $"Assets/Tables/{_table.Name}/";
			if (!Directory.Exists(assetsTableRoot)) {
				Directory.CreateDirectory(assetsTableRoot);
			}

			_assetsTextures = $"{assetsTableRoot}/Textures/";
			if (!Directory.Exists(_assetsTextures)) {
				Directory.CreateDirectory(_assetsTextures);
			}

			_assetsMaterials = $"{assetsTableRoot}/Materials/";
			if (!Directory.Exists(_assetsMaterials)) {
				Directory.CreateDirectory(_assetsMaterials);
			}
		}

		private void CreateRootHierarchy(string tableName)
		{
			// set the GameObject name; this needs to happen after MakeSerializable because the name is set there as well
			if (string.IsNullOrEmpty(tableName)) {
				tableName = _table.Name;

			} else {
				tableName = tableName
					.Replace("%TABLENAME%", _table.Name)
					.Replace("%INFONAME%", _table.InfoName);
			}

			_tableGo = new GameObject(tableName);
			_playfieldGo = new GameObject("Playfield");
			var backglassGo = new GameObject("Backglass");
			var cabinetGo = new GameObject("Cabinet");

			_tableAuthoring = _tableGo.AddComponent<TableAuthoring>();
			_tableAuthoring.SetItem(_table);

			_playfieldGo.transform.SetParent(_tableGo.transform, false);
			backglassGo.transform.SetParent(_tableGo.transform, false);
			cabinetGo.transform.SetParent(_tableGo.transform, false);

			_playfieldGo.transform.localRotation = GlobalRotation;
			_playfieldGo.transform.localPosition = new Vector3(-_table.Width / 2 * GlobalScale, 0f, _table.Height / 2 * GlobalScale);
			_playfieldGo.transform.localScale = new Vector3(GlobalScale, GlobalScale, GlobalScale);
		}

		private GameObject GetGroupParent(IItem item)
		{
			// create group parent if not created (if null, attach it to the table directly).
			if (!string.IsNullOrEmpty(item.ItemGroupName)) {
				if (!_groupParents.ContainsKey(item.ItemGroupName)) {
					var parent = new GameObject(item.ItemGroupName);
					parent.transform.SetParent(_playfieldGo.transform, false);
					_groupParents[item.ItemGroupName] = parent;
				}
			}
			var groupParent = !string.IsNullOrEmpty(item.ItemGroupName)
				? _groupParents[item.ItemGroupName]
				: _playfieldGo;

			return groupParent;
		}

		#region ITextureProvider

		public Texture GetTexture(string name)
		{
			if (!_textures.ContainsKey(name.ToLower())) {
				throw new ArgumentException($"Texture \"{name.ToLower()}\" not loaded!");
			}
			return _textures[name.ToLower()];
		}

		#endregion

		#region IMaterialProvider

		public bool HasMaterial(string name) => _materials.ContainsKey(name);
		public Material GetMaterial(string name) => _materials[name];
		public void SaveMaterial(PbrMaterial vpxMaterial, Material material)
		{
			_materials[vpxMaterial.Id] = material;
			AssetDatabase.CreateAsset(material, vpxMaterial.GetUnityFilename(_assetsMaterials));
		}

		#endregion
	}
}
