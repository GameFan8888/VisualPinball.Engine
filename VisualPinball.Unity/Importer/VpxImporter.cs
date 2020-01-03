﻿using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using NLog;
using UnityEditor;
using UnityEngine;
using VisualPinball.Engine.VPT.Table;
using VisualPinball.Unity.Extensions;
using VisualPinball.Unity.IO;
using Logger = NLog.Logger;
using Material = UnityEngine.Material;

namespace VisualPinball.Unity.Importer
{

	public class VpxImporter : MonoBehaviour
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		private bool _saveToAssets = false;
		private string _tableFolder;
		private string _materialFolder;
		private string _tableDataPath;
		private string _tablePrefabPath;

		private readonly Dictionary<string, Material> _materials = new Dictionary<string, Material>();

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
			var watch = Stopwatch.StartNew();

			// open file dialog
			var vpxPath = EditorUtility.OpenFilePanelWithFilters("Import .VPX File", "Assets/", new[] { "Visual Pinball Table Files", "vpx" });
			if (vpxPath.Length == 0) {
				return;
			}

			var rootGameObj = ImportVpx(vpxPath, true);

			// if an object was selected in the editor, make it its parent
			GameObjectUtility.SetParentAndAlign(rootGameObj, menuCommand.context as GameObject);

			// register undo system
			Undo.RegisterCreatedObjectUndo(rootGameObj, "Import VPX table file");

			// select imported object
			Selection.activeObject = rootGameObj;

			watch.Stop();
			Logger.Info("[VpxImporter] Imported in {0}ms.", watch.ElapsedMilliseconds);
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
			var table = Table.Load(path);
			gameObject.name = table.Name;

			// set paths
			_saveToAssets = saveToAssets;
			if (_saveToAssets) {
				_tableFolder = $"Assets/{Path.GetFileNameWithoutExtension(path)}";
				_materialFolder = $"{_tableFolder}/Materials";
				_tableDataPath = $"{_tableFolder}/{AssetUtility.StringToFilename(table.Name)}_data.asset";
				_tablePrefabPath = $"{_tableFolder}/{AssetUtility.StringToFilename(table.Name)}.prefab";
				AssetUtility.CreateFolders(_tableFolder, _materialFolder);
			}

			// create asset object
			var asset = ScriptableObject.CreateInstance<VpxAsset>();

			// import materials
			ImportMaterials(table);

			// import table
			ImportGameItems(table, asset);
		}

		private void ImportMaterials(Table table)
		{
			foreach (var material in table.Materials) {
				SaveMaterial(material);
			}
		}

		private void ImportGameItems(Table table, VpxAsset asset)
		{
			// save game objects to asset folder
			if (_saveToAssets) {
				AssetDatabase.CreateAsset(asset, _tableDataPath);
				AssetDatabase.SaveAssets();
			}

			// import game objects
			ImportPrimitives(table, asset);

			if (_saveToAssets) {
				PrefabUtility.SaveAsPrefabAssetAndConnect(gameObject, _tablePrefabPath, InteractionMode.UserAction);
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
			}
		}

		private void ImportPrimitives(Table table, VpxAsset asset)
		{
			var primitivesObj = new GameObject("Primitives");
			primitivesObj.transform.parent = gameObject.transform;

			foreach (var primitive in table.Primitives.Values) {

				// convert mesh
				var mesh = primitive.GetMesh(table).ToUnityMesh();
				if (mesh == null) {
					continue;
				}
				mesh.name = $"{primitive.Name}_mesh";

				// create game object for primitive
				var obj = new GameObject(primitive.Name);
				obj.transform.parent = primitivesObj.transform;

				// apply mesh to game object
				var mf = obj.AddComponent<MeshFilter>();
				mf.sharedMesh = mesh;

				// apply loaded material
				var materialVpx = primitive.GetMaterial(table);
				if (materialVpx != null) {
					var materialUnity = LoadMaterial(materialVpx);
					var mr = obj.AddComponent<MeshRenderer>();
					mr.sharedMaterial = materialUnity;
				}

				// add mesh to asset
				if (_saveToAssets) {
					AssetDatabase.AddObjectToAsset(mesh, asset);
				}
			}
		}

		private void SaveMaterial(Engine.VPT.Material material)
		{
			if (_saveToAssets) {
				AssetDatabase.CreateAsset(material.ToUnityMaterial(), material.GetUnityFilename(_materialFolder));
			} else {
				_materials[material.Name] = material.ToUnityMaterial();
			}
		}

		private Material LoadMaterial(Engine.VPT.Material material)
		{
			if (_saveToAssets) {
				return AssetDatabase.LoadAssetAtPath(material.GetUnityFilename(_materialFolder), typeof(Material)) as Material;
			}
			return _materials[material.Name];
		}
	}

	internal class VpxAsset : ScriptableObject
	{
	}
}
