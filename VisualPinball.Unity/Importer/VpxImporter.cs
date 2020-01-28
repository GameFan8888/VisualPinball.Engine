﻿// ReSharper disable ConvertIfStatementToReturnStatement

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using NLog;
using OpenMcdf;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;
using VisualPinball.Engine.Game;
using VisualPinball.Engine.IO;
using VisualPinball.Engine.VPT;
using VisualPinball.Engine.VPT.Primitive;
using VisualPinball.Engine.VPT.Table;
using VisualPinball.Unity.Extensions;
using Logger = NLog.Logger;
using Logging = VisualPinball.Unity.IO.Logging;
using Material = UnityEngine.Material;
using Texture = VisualPinball.Engine.VPT.Texture;

namespace VisualPinball.Unity.Importer
{
	public class VpxImporter : MonoBehaviour
	{
		private static readonly Quaternion GlobalRotation = Quaternion.Euler(-90, 0, 0);
		private const float GlobalScale = 0.01f;

		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		private VpxAsset _asset;
		private bool _saveToAssets;
		private string _tableFolder;
		private string _materialFolder;
		private string _textureFolder;
		private string _tableDataPath;
		private string _tablePrefabPath;

		private Patcher.Patcher.Patcher _patcher;

		private readonly Dictionary<IRenderable, RenderObjectGroup> _renderObjects = new Dictionary<IRenderable, RenderObjectGroup>();
		private readonly Dictionary<string, Texture2D> _textures = new Dictionary<string, Texture2D>();
		private readonly Dictionary<string, Material> _materials = new Dictionary<string, Material>();
		private readonly Dictionary<string, GameObject> _parents = new Dictionary<string, GameObject>();

		private Table _table;

		public static void ImportVpxRuntime(string path)
		{
			ImportVpx(path, false);
		}

		/// <summary>
		/// Imports a Visual Pinball File (.vpx) into the Unity Editor. <p/>
		///
		/// The goal of this is to be able to iterate rapidly without having to
		/// execute the runtime on every test. This importer also saves the
		/// imported data to the Assets folder so a project with an imported table
		/// can be saved and loaded
		/// </summary>
		/// <param name="menuCommand">Context provided by the Editor</param>
		[MenuItem("Visual Pinball/Import VPX", false, 10)]
		public static void ImportVpxEditor(MenuCommand menuCommand)
		{
			// TODO that somewhere else
			Logging.Setup();

			// open file dialog
			var vpxPath = EditorUtility.OpenFilePanelWithFilters("Import .VPX File", null, new[] { "Visual Pinball Table Files", "vpx" });
			if (vpxPath.Length == 0) {
				return;
			}

			Profiler.Start("VpxImporter.ImportVpxEditor()");
			var rootGameObj = ImportVpx(vpxPath, true);

			// if an object was selected in the editor, make it its parent
			GameObjectUtility.SetParentAndAlign(rootGameObj, menuCommand.context as GameObject);

			// register undo system
			Undo.RegisterCreatedObjectUndo(rootGameObj, "Import VPX table file");

			// select imported object
			Selection.activeObject = rootGameObj;

			Profiler.Stop("VpxImporter.ImportVpxEditor()");
			Logger.Info("[VpxImporter] Imported!");
			Profiler.Print();
			Profiler.Reset();
		}

		private static GameObject ImportVpx(string path, bool saveToAssets) {

			// create root object
			var rootGameObj = new GameObject();
			var importer = rootGameObj.AddComponent<VpxImporter>();

			importer.Import(path, saveToAssets);

			return rootGameObj;
		}

		private void Import(string path, bool saveToAssets)
		{
			// parse table
			Profiler.Start("VpxImporter.Import()");
			_table = TableLoader.LoadTable(path);
			//var table = Table.Load(path);

			var go = gameObject;
			go.name = _table.Name;
			_patcher = new Patcher.Patcher.Patcher(_table, Path.GetFileName(path));

			// set paths
			_saveToAssets = saveToAssets;
			if (_saveToAssets) {
				_tableFolder = $"Assets/{Path.GetFileNameWithoutExtension(path)?.Trim()}";
				_materialFolder = $"{_tableFolder}/Materials";
				_textureFolder = $"{_tableFolder}/Textures";
				_tableDataPath = $"{_tableFolder}/{AssetUtility.StringToFilename(_table.Name)}_data.asset";
				_tablePrefabPath = $"{_tableFolder}/{AssetUtility.StringToFilename(_table.Name)}.prefab";
				AssetUtility.CreateFolders(_tableFolder, _materialFolder, _textureFolder);
			}

			// create asset object
			_asset = ScriptableObject.CreateInstance<VpxAsset>();
			AssetDatabase.SaveAssets();

			// generate meshes now
			var materials = new Dictionary<string, PbrMaterial>();
			foreach (var r in _table.Renderables) {
				_renderObjects[r] = r.GetRenderObjects(_table, Origin.Original, false);
				foreach (var ro in _renderObjects[r].RenderObjects) {
					if (!materials.ContainsKey(ro.Material.Id)) {
						materials[ro.Material.Id] = ro.Material;
					}
				}
			}

			// import textures
			Profiler.Start("ImportTextures via Job");
			var textureImporter = new TextureImporter(_table.Textures.Values.Concat(Texture.LocalTextures).ToArray());
			textureImporter.ImportTextures(_textureFolder);
			Profiler.Stop("ImportTextures via Job");
			// ImportTextures(table);

			// import materials
			Profiler.Start("ImportMaterials via Job");
			var materialImporter = new MaterialImporter(materials.Values.ToArray());
			materialImporter.ImportMaterials(_materialFolder, _textureFolder);
			Profiler.Stop("ImportMaterials via Job");

			// import table
			ImportGameItems();

			// import lights
			ImportGiLights();

			// set root transformation
			go.transform.localRotation = GlobalRotation;
			go.transform.localPosition = new Vector3(-_table.Width / 2 * GlobalScale, 0f, -_table.Height / 2 * GlobalScale);
			go.transform.localScale = new Vector3(GlobalScale, GlobalScale, GlobalScale);
			//ScaleNormalizer.Normalize(go, GlobalScale);
			Profiler.Stop("VpxImporter.Import()");
		}

		// private void ImportTextures(Table table)
		// {
		// 	Profiler.Start("VpxImporter.ImportTextures()");
		// 	Profiler.Start("Table");
		// 	foreach (var texture in table.Textures.Values) {
		// 		SaveTexture(texture);
		// 	}
		// 	Profiler.Stop("Table");
		//
		// 	// also import local textures
		// 	Profiler.Start("Local");
		// 	foreach (var texture in Texture.LocalTextures) {
		// 		SaveTexture(texture);
		// 	}
		// 	Profiler.Stop("Local");
		// 	Profiler.Stop("VpxImporter.ImportTextures()");
		// }

		private void ImportGameItems()
		{
			Profiler.Start("VpxImporter.ImportGameItems()");

			// save game objects to asset folder
			if (_saveToAssets) {
				AssetDatabase.CreateAsset(_asset, _tableDataPath);
				AssetDatabase.SaveAssets();
			}

			// import game objects
			ImportRenderables();

			if (_saveToAssets) {
				Profiler.Start("ItemsAssets");
				Profiler.Start("PrefabUtility.SaveAsPrefabAssetAndConnect");
				PrefabUtility.SaveAsPrefabAssetAndConnect(gameObject, _tablePrefabPath, InteractionMode.UserAction);
				Profiler.Stop("PrefabUtility.SaveAsPrefabAssetAndConnect");
				Profiler.Start("AssetDatabase.SaveAssets");
				AssetDatabase.SaveAssets();
				Profiler.Stop("AssetDatabase.SaveAssets");
				Profiler.Start("AssetDatabase.Refresh");
				AssetDatabase.Refresh();
				Profiler.Stop("AssetDatabase.Refresh");
				Profiler.Stop("ItemsAssets");
			}

			Profiler.Stop("VpxImporter.ImportGameItems()");
		}

		private void ImportGiLights()
		{
			var lightsObj = new GameObject("Lights");
			lightsObj.transform.parent = gameObject.transform;
			foreach (var vpxLight in _table.Lights.Values.Where(l => l.Data.IsBulbLight)) {
				var unityLight = vpxLight.ToUnityPointLight();
				unityLight.transform.parent = lightsObj.transform;
			}
		}

		private void ImportRenderables()
		{
			Profiler.Start("VpxImporter.ImportRenderables()");
			foreach (var renderable in _renderObjects.Keys) {
				var ro = _renderObjects[renderable];
				if (!_parents.ContainsKey(ro.Parent)) {
					var parent = new GameObject(ro.Parent);
					parent.transform.parent = gameObject.transform;
					_parents[ro.Parent] = parent;
				}
				Profiler.Start("Convert");
				ImportRenderObjects(renderable, ro, _parents[ro.Parent]);
				Profiler.Stop("Convert");
			}
			Profiler.Stop("VpxImporter.ImportRenderables()");
		}

		private void ImportRenderObjects(IRenderable item, RenderObjectGroup rog, GameObject parent)
		{
			var obj = new GameObject(rog.Name);
			obj.transform.parent = parent.transform;

			if (rog.HasOnlyChild) {
				ImportRenderObject(item, rog.RenderObjects[0], obj);

			} else if (rog.HasChildren) {
				foreach (var ro in rog.RenderObjects) {
					var subObj = new GameObject(ro.Name);
					subObj.transform.parent = obj.transform;
					ImportRenderObject(item, ro, subObj);
				}
			}

			// apply transformation
			if (rog.HasChildren) {
				SetTransform(obj.transform, rog.TransformationMatrix.ToUnityMatrix());
			}
		}

		private void ImportRenderObject(IRenderable item, RenderObject ro, GameObject obj)
		{
			if (ro.Mesh == null) {
				Logger.Warn($"No mesh for object {obj.name}, skipping.");
				return;
			}

			Profiler.Start("ToUnityMesh");
			var mesh = ro.Mesh.ToUnityMesh($"{obj.name}_mesh");
			Profiler.Stop("ToUnityMesh");
			obj.SetActive(ro.IsVisible);

			// apply mesh to game object
			var mf = obj.AddComponent<MeshFilter>();
			mf.sharedMesh = mesh;

			// apply material
			var mr = obj.AddComponent<MeshRenderer>();
			mr.sharedMaterial = GetMaterial(ro.Material);
			if (mr.sharedMaterial.name == PbrMaterial.NameNoMaterial) {
				mr.enabled = false;
			}

			// patch
			Profiler.Start("Patch & Assets");
			_patcher.ApplyPatches(item, ro, obj);

			// add mesh to asset
			if (_saveToAssets) {
				AssetDatabase.AddObjectToAsset(mesh, _asset);
			}
			Profiler.Stop("Patch & Assets");
		}

		private Material GetMaterial(PbrMaterial mat)
		{
			Profiler.Start("GetMaterial");
			var material = LoadMaterial(mat);
			// if (material == null) {
			// 	material = ro.Material.ToUnityMaterial();
			// 	// if (ro.Map != null) {
			// 	// 	Profiler.Start("SetTexture");
			// 	// 	material.SetTexture(MainTex, LoadTexture(ro.Map, TextureImporterType.Default));
			// 	// 	Profiler.Stop("SetTexture");
			// 	// }
			// 	// if (ro.NormalMap != null) {
			// 	// 	Profiler.Start("SetTexture");
			// 	// 	material.SetTexture(BumpMap, LoadTexture(ro.NormalMap, TextureImporterType.NormalMap));
			// 	// 	Profiler.Stop("SetTexture");
			// 	// }
			// 	Profiler.Start("SaveMaterial");
			// 	SaveMaterial(ro, material);
			// 	Profiler.Stop("SaveMaterial");
			// }
			Profiler.Stop("GetMaterial");

			return material;
		}

		// private void SaveTexture(Texture texture)
		// {
		// 	if (_saveToAssets) {
		// 		AssetUtility.CreateTexture(texture, _textureFolder);
		// 	} else {
		// 		_textures[texture.Name] = texture.ToUnityTexture();
		// 	}
		// }

		// private Texture2D LoadTexture(Texture texture, TextureImporterType type)
		// {
		// 	Profiler.Start("LoadTexture");
		// 	if (_saveToAssets) {
		// 		Profiler.Start("LoadAssetAtPath");
		// 		var unityTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texture.GetUnityFilename(_textureFolder));
		// 		Profiler.Stop("LoadAssetAtPath");
		// 		//ImportTextureAs(texture, type, AssetDatabase.GetAssetPath(unityTex));
		// 		Profiler.Stop("LoadTexture");
		// 		return unityTex;
		// 	}
		// 	return _textures[texture.Name];
		// }

		// private static void ImportTextureAs(Texture map, TextureImporterType type, string path)
		// {
		// 	Profiler.Start("ImportTextureAs");
		// 	var textureImporter = AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
		// 	if (textureImporter != null) {
		// 		textureImporter.textureType = type;
		// 		Profiler.Start("HasTransparentPixels");
		// 		textureImporter.alphaIsTransparency = map.HasTransparentPixels;
		// 		Profiler.Stop("HasTransparentPixels");
		// 		textureImporter.isReadable = true;
		// 		textureImporter.mipmapEnabled = true;
		// 		textureImporter.filterMode = FilterMode.Bilinear;
		// 		EditorUtility.CompressTexture(AssetDatabase.LoadAssetAtPath<Texture2D>(path), map.HasTransparentPixels ? TextureFormat.ARGB32 : TextureFormat.RGB24, UnityEditor.TextureCompressionQuality.Best);
		// 		Profiler.Start("AssetDatabase.ImportAsset");
		// 		AssetDatabase.ImportAsset(path);
		// 		Profiler.Stop("AssetDatabase.ImportAsset");
		// 	}
		// 	Profiler.Stop("ImportTextureAs");
		// }

		// private void SaveMaterial(RenderObject ro, Material material)
		// {
		// 	if (_saveToAssets) {
		// 		Profiler.Start("AssetDatabase.CreateAsset");
		// 		AssetDatabase.CreateAsset(material, ro.Material.GetUnityFilename(_materialFolder));
		// 		Profiler.Stop("AssetDatabase.CreateAsset");
		// 	} else {
		// 		_materials[ro.Material.Id] = material;
		// 	}
		// }

		private Material LoadMaterial(PbrMaterial mat)
		{
			if (_saveToAssets) {
				return AssetDatabase.LoadAssetAtPath<Material>(mat.GetUnityFilename(_materialFolder));
			}

			return _materials.ContainsKey(mat.Id) ? _materials[mat.Id] : null;
		}

		private static void SetTransform(Transform tf, Matrix4x4 trs)
		{
			tf.localScale = new Vector3(
				trs.GetColumn(0).magnitude,
				trs.GetColumn(1).magnitude,
				trs.GetColumn(2).magnitude
			);
			//Logger.Info($"Scaling at {trs.GetColumn(0).magnitude}/{trs.GetColumn(1).magnitude}/{trs.GetColumn(2).magnitude}");
			tf.localPosition = trs.GetColumn(3);
			tf.localRotation = Quaternion.LookRotation(
				trs.GetColumn(2),
				trs.GetColumn(1)
			);
		}
	}

	internal class VpxAsset : ScriptableObject
	{
	}
}
