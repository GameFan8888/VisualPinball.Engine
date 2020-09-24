﻿// Visual Pinball Engine
// Copyright (C) 2020 freezy and VPE Team
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

// ReSharper disable ConvertIfStatementToReturnStatement
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable RedundantAssignment

using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using UnityEngine;
using VisualPinball.Engine.Game;
using VisualPinball.Engine.VPT;
using VisualPinball.Engine.VPT.Bumper;
using VisualPinball.Engine.VPT.Decal;
using VisualPinball.Engine.VPT.DispReel;
using VisualPinball.Engine.VPT.Flasher;
using VisualPinball.Engine.VPT.Flipper;
using VisualPinball.Engine.VPT.Gate;
using VisualPinball.Engine.VPT.HitTarget;
using VisualPinball.Engine.VPT.Kicker;
using VisualPinball.Engine.VPT.LightSeq;
using VisualPinball.Engine.VPT.Plunger;
using VisualPinball.Engine.VPT.Primitive;
using VisualPinball.Engine.VPT.Ramp;
using VisualPinball.Engine.VPT.Rubber;
using VisualPinball.Engine.VPT.Spinner;
using VisualPinball.Engine.VPT.Surface;
using VisualPinball.Engine.VPT.Table;
using VisualPinball.Engine.VPT.TextBox;
using VisualPinball.Engine.VPT.Timer;
using VisualPinball.Engine.VPT.Trigger;
using Logger = NLog.Logger;

namespace VisualPinball.Unity
{
	public class VpxConverter : MonoBehaviour
	{
		private static readonly Quaternion GlobalRotation = Quaternion.Euler(-90, 0, 0);
		public const float GlobalScale = 0.001f;
		public const int ChildObjectsLayer = 16;

		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		private readonly Dictionary<IRenderable, RenderObjectGroup> _renderObjects = new Dictionary<IRenderable, RenderObjectGroup>();
		private readonly Dictionary<string, GameObject> _parents = new Dictionary<string, GameObject>();

		private Table _table;
		private TableAuthoring _tableAuthoring;
		private bool _applyPatch = true;

		public void Convert(string fileName, Table table, bool applyPatch = true, string tableName = null)
		{
			_table = table;

			// TODO: implement disabling patching; not so obvious because of the static methods being used for the import
			if( !applyPatch)
				Logger.Warn("Disabling patch import not implemented yet!");

			var go = gameObject;

			MakeSerializable(go, table);

			// set the gameobject name; this needs to happen after MakeSerializable because the name is set there as well
			if( string.IsNullOrEmpty( tableName))
			{
				go.name = _table.Name;
			}
			else
			{
				go.name = tableName
					.Replace("%TABLENAME%", _table.Name)
					.Replace("%INFONAME%", _table.InfoName);
			}

			_tableAuthoring.Patcher = new Patcher.Patcher(_table, fileName);

			// generate meshes and save (pbr) materials
			var materials = new Dictionary<string, PbrMaterial>();
			foreach (var r in _table.Renderables) {
				_renderObjects[r] = r.GetRenderObjects(_table, Origin.Original, false);
				foreach (var ro in _renderObjects[r].RenderObjects) {
					if (!materials.ContainsKey(ro.Material.Id)) {
						materials[ro.Material.Id] = ro.Material;
					}
				}
			}

			// import
			ConvertGameItems(go);

			// set root transformation
			go.transform.localRotation = GlobalRotation;
			go.transform.localPosition = new Vector3(-_table.Width / 2 * GlobalScale, 0f, _table.Height / 2 * GlobalScale);
			go.transform.localScale = new Vector3(GlobalScale, GlobalScale, GlobalScale);
			//ScaleNormalizer.Normalize(go, GlobalScale);

			// add the player script and default game engine
			go.AddComponent<Player>();
			var dga = go.AddComponent<DefaultGameEngineAuthoring>();

			// populate mappings
			if (_table.Mappings.IsEmpty()) {
				_table.Mappings.PopulateSwitches((dga.GameEngine as IGamelogicEngineWithSwitches).AvailableSwitches, table.Switchables);
				_table.Mappings.PopulateCoils((dga.GameEngine as IGamelogicEngineWithCoils).AvailableCoils, table.Coilables);
			}

			// don't need that anymore.
			DestroyImmediate(this);
		}

		private void ConvertGameItems(GameObject tableGameObject)
		{
			// convert game objects
			ConvertRenderables(tableGameObject);
		}

		private void ConvertRenderables(GameObject tableGameObject)
		{
			var createMainObjs = new Dictionary<string, GameObject>();
			var createdMainMbs = new Dictionary<string, MonoBehaviour>();
			var createdObjs = new Dictionary<IRenderable, IEnumerable<Tuple<GameObject, RenderObject>>>();
			var renderObjects = from entry
				in _renderObjects orderby entry.Value.SubComponent select entry;

			foreach (var kv in renderObjects) {
				var renderable = kv.Key;
				var rog = kv.Value;

				// create item type parent
				if (!_parents.ContainsKey(rog.Parent)) {
					var parent = new GameObject(rog.Parent);
					parent.transform.parent = gameObject.transform;
					_parents[rog.Parent] = parent;
				}

				// create object(s)
				createdObjs[renderable] = ConvertRenderObjects(rog, _parents[rog.Parent], _tableAuthoring, out var rootObj);

				// if the object's names was parsed to be part of another object, re-link to other object.
				if (rog.SubComponent != RenderObjectGroup.ItemSubComponent.None) {
					if (!createMainObjs.ContainsKey(rog.ComponentName.ToLower())) {
						Logger.Warn($"Cannot find component \"{rog.ComponentName.ToLower()}\" that is supposed to be the parent of \"{rog.Name}\".");
						SetupGameObjectComponents(renderable, rootObj, rog);

					} else {
						var mainObj = createMainObjs[rog.ComponentName.ToLower()];
						var mainMb = createdMainMbs[rog.ComponentName.ToLower()];
						rootObj.transform.SetParent(mainObj.transform, false);
						SetupGameObjectComponents(renderable, rootObj, rog, mainMb);
					}
				} else {
					var rootMb = SetupGameObjectComponents(renderable, rootObj, rog);
					createMainObjs[rog.Name.ToLower()] = rootObj;
					createdMainMbs[rog.Name.ToLower()] = rootMb;
				}
			}

			// now we have all renderables imported, patch them.
			foreach (var renderable in createdObjs.Keys) {
				foreach (var (obj, ro) in createdObjs[renderable]) {
					_tableAuthoring.Patcher.ApplyPatches(renderable, ro, obj, tableGameObject);
				}
			}
		}

		public static IEnumerable<Tuple<GameObject, RenderObject>> ConvertRenderObjects(RenderObjectGroup rog,
			GameObject parent, TableAuthoring tb, out GameObject obj)
		{
			obj = new GameObject(rog.Name);
			obj.transform.parent = parent.transform;

			var createdObjs = rog.SubComponent == RenderObjectGroup.ItemSubComponent.Collider
				? new Tuple<GameObject, RenderObject>[0]
				: SetupRenderObject(obj, rog, tb);

			// apply transformation
			obj.transform.SetFromMatrix(rog.TransformationMatrix.ToUnityMatrix());

			return createdObjs;
		}

		public static MonoBehaviour SetupGameObjectComponents(IRenderable item, GameObject obj,
			RenderObjectGroup rog, MonoBehaviour mainMb = null)
		{
			MonoBehaviour mb = null;
			switch (item) {
				case Bumper bumper:             bumper.SetupGameObject(obj, rog); break;
				case Flipper flipper:           flipper.SetupGameObject(obj, rog); break;
				case Gate gate:                 gate.SetupGameObject(obj, rog); break;
				case HitTarget hitTarget:       hitTarget.SetupGameObject(obj, rog); break;
				case Kicker kicker:             kicker.SetupGameObject(obj, rog); break;
				case Engine.VPT.Light.Light lt: lt.SetupGameObject(obj, rog); break;
				case Plunger plunger:           plunger.SetupGameObject(obj, rog); break;
				case Primitive primitive:       primitive.SetupGameObject(obj, rog); break;
				case Ramp ramp:                 ramp.SetupGameObject(obj, rog); break;
				case Rubber rubber:             mb = rubber.SetupGameObject(obj, rog, mainMb); break;
				case Spinner spinner:           spinner.SetupGameObject(obj, rog); break;
				case Surface surface:           mb = surface.SetupGameObject(obj, rog, mainMb); break;
				case Table table:               table.SetupGameObject(obj, rog); break;
				case Trigger trigger:           trigger.SetupGameObject(obj, rog); break;
			}

			return mb;
		}

		private static IEnumerable<Tuple<GameObject, RenderObject>> SetupRenderObject(GameObject obj, RenderObjectGroup rog, TableAuthoring tb)
		{
			var createdObjs = new Tuple<GameObject, RenderObject>[0];
			if (rog.HasOnlyChild && !rog.ForceChild) {
				SetupMesh(obj, rog.RenderObjects[0], tb);
				createdObjs = new[] { new Tuple<GameObject, RenderObject>(obj, rog.RenderObjects[0]) };

			} else if (rog.HasChildren) {
				createdObjs = new Tuple<GameObject, RenderObject>[rog.RenderObjects.Length];
				var i = 0;
				foreach (var ro in rog.RenderObjects) {
					var subObj = new GameObject(ro.Name);
					subObj.transform.SetParent(obj.transform, false);
					subObj.layer = ChildObjectsLayer;
					SetupMesh(subObj, ro, tb);
					createdObjs[i++] = new Tuple<GameObject, RenderObject>(subObj, ro);
				}
			}

			return createdObjs;
		}

		private static void SetupMesh(GameObject obj, RenderObject ro, TableAuthoring ta)
		{
			if (ro.Mesh == null) {
				Logger.Warn($"No mesh for object {obj.name}, skipping.");
				return;
			}

			var mesh = ro.Mesh.ToUnityMesh($"{obj.name}_mesh");

			// apply mesh to game object
			var mf = obj.AddComponent<MeshFilter>();
			mf.sharedMesh = mesh;

			// apply material
			if (ro.Mesh.AnimationFrames.Count > 0) {
				var smr = obj.AddComponent<SkinnedMeshRenderer>();
				smr.sharedMaterial = ro.Material.ToUnityMaterial(ta);
				smr.sharedMesh = mesh;
				smr.enabled = ro.IsVisible;
			} else {
				var mr = obj.AddComponent<MeshRenderer>();
				mr.sharedMaterial = ro.Material.ToUnityMaterial(ta);
				mr.enabled = ro.IsVisible;
			}
		}

		private void MakeSerializable(GameObject go, Table table)
		{
			// add table component (plus other data)
			_tableAuthoring = go.AddComponent<TableAuthoring>();
			_tableAuthoring.SetItem(table);

			var sidecar = _tableAuthoring.GetOrCreateSidecar();

			foreach (var key in table.TableInfo.Keys) {
				sidecar.tableInfo[key] = table.TableInfo[key];
			}

			// copy each serializable ref into the sidecar's serialized storage
			sidecar.textures.AddRange(table.Textures);
			sidecar.sounds.AddRange(table.Sounds);

			// and tell the engine's table to now use the sidecar as its container so we can all operate on the same underlying container
			table.SetTextureContainer(sidecar.textures);
			table.SetSoundContainer(sidecar.sounds);

			sidecar.customInfoTags = table.CustomInfoTags;
			sidecar.collections = table.Collections.Values.Select(c => c.Data).ToList();
			sidecar.mappings = table.Mappings.Data;
			sidecar.decals = table.GetAllData<Decal, DecalData>();
			sidecar.dispReels = table.GetAllData<DispReel, DispReelData>();
			sidecar.flashers = table.GetAllData<Flasher, FlasherData>();
			sidecar.lightSeqs = table.GetAllData<LightSeq, LightSeqData>();
			sidecar.plungers = table.GetAllData<Plunger, PlungerData>();
			sidecar.textBoxes = table.GetAllData<TextBox, TextBoxData>();
			sidecar.timers = table.GetAllData<Timer, TimerData>();

			Logger.Info("Collections saved: [ {0} ] [ {1} ]",
				string.Join(", ", table.Collections.Keys),
				string.Join(", ", sidecar.collections.Select(c => c.Name))
			);
		}
	}
}
