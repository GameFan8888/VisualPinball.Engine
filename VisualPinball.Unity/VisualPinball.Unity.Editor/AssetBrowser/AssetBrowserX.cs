// Visual Pinball Engine
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

// ReSharper disable InconsistentNaming

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace VisualPinball.Unity.Editor
{
	public partial class AssetBrowserX : EditorWindow
	{
		[SerializeField]
		private int _selectedIndex = -1;

		[SerializeField]
		private int _thumbnailSize = 150;

		private List<LibraryAsset> _assets;
		private List<AssetLibrary> _assetLibraries;

		private readonly Dictionary<LibraryAsset, VisualElement> _elements = new();

		[MenuItem("Visual Pinball/Asset Browser")]
		public static void ShowWindow()
		{
			var wnd = GetWindow<AssetBrowserX>("Asset Browser");

			// Limit size of the window
			wnd.minSize = new Vector2(450, 200);
			wnd.maxSize = new Vector2(1920, 720);
		}

		private void OnEnable()
		{
			_assetLibraries = AssetDatabase.FindAssets($"t:{typeof(AssetLibrary)}")
				.Select(AssetDatabase.GUIDToAssetPath)
				.Select(AssetDatabase.LoadAssetAtPath<AssetLibrary>)
				.Where(asset => asset != null).ToList();
		}

		private void Search()
		{
			_assets = _assetLibraries.SelectMany(lib => lib.GetAssets()).ToList();
			_bottomLabel.text = $"Found {_assets.Count} assets.";
		}

		private void Init()
		{
			Search();

			// _rightPane.makeItem = () => new Label();
			// _rightPane.bindItem = (item, index) => {
			// 	(item as Label)!.text = Path.GetFileName(_assets[index].Path);
			// };
			// _rightPane.itemsSource = _assets;
			// _rightPane.onSelectionChange += OnAssetSelectionChange;
			// _rightPane.onSelectionChange += _ => {
			// 	selectedIndex = _rightPane.selectedIndex;
			// };
			// _rightPane.selectedIndex = selectedIndex;
		}

		private void Refresh()
		{
			OnDestroy();
			rootVisualElement.Clear();
			CreateGUI();
			_midPane.Clear();
			_elements.Clear();
			foreach (var a in _assets) {
				var obj = AssetDatabase.LoadAssetAtPath(a.Path, TypeByName(a.Type));
				var tex = AssetPreview.GetAssetPreview(obj);
				_elements[a] = NewItem(tex, Path.GetFileNameWithoutExtension(a.Path));
				_midPane.Add(_elements[a]);
			}
		}


		private void OnDragUpdatedEvent(DragUpdatedEvent evt)
		{
			DragAndDrop.visualMode = DragAndDrop.objectReferences != null
				? DragAndDropVisualMode.Move
				: DragAndDropVisualMode.Copy;
		}

		private void OnDragPerformEvent(DragPerformEvent evt)
		{
			DragAndDrop.AcceptDrag();

			// Disallow adding from outside of Unity
			foreach (var path in DragAndDrop.paths) {
				var libraryFound = false;
				foreach (var assetLibrary in _assetLibraries) {
					if (path.Replace('\\', '/').StartsWith(assetLibrary.LibraryRoot.Replace('\\', '/'))) {
						libraryFound = true;
						var guid = AssetDatabase.AssetPathToGUID(path);
						var type = AssetDatabase.GetMainAssetTypeAtPath(path);

						if (assetLibrary.AddAsset(guid, type, path)) {
							Debug.Log($"{Path.GetFileName(path)} added to library {assetLibrary.Name}.");
						} else {
							Debug.Log($"{Path.GetFileName(path)} updated in library {assetLibrary.Name}.");
						}

						Refresh();
					}
				}
				if (!libraryFound) {
					Debug.LogError($"Cannot find a VPE library at path {Path.GetDirectoryName(path)}, ignoring asset {Path.GetFileName(path)}.");
				}
			}
		}
		private void OnThumbSizeChanged(ChangeEvent<float> evt)
		{
			_thumbnailSize = (int)evt.newValue;
			foreach (var e in _elements.Values) {
				e.style.width = _thumbnailSize;
				e.style.height = _thumbnailSize;
			}
		}
	}
}
