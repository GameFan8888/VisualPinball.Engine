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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace VisualPinball.Unity.Editor
{
	public partial class AssetBrowserX : EditorWindow, IDragHandler
	{
		[SerializeField]
		private int _thumbnailSize = 150;

		public AssetLibrary ActiveLibrary;
		public List<AssetLibrary> Libraries;

		private List<AssetData> _assets;
		private AssetQuery _query;

		private AssetData LastSelectedAsset {
			set => _detailsElement.Asset = value;
		}

		private AssetData _firstSelectedAsset;
		private readonly HashSet<AssetData> _selectedAssets = new();

		private readonly Dictionary<AssetData, VisualElement> _elementByAsset = new();
		private readonly Dictionary<VisualElement, AssetData> _assetsByElement = new();

		[MenuItem("Visual Pinball/Asset Browser")]
		public static void ShowWindow()
		{
			var wnd = GetWindow<AssetBrowserX>("Asset Browser");

			// Limit size of the window
			wnd.minSize = new Vector2(450, 200);
			wnd.maxSize = new Vector2(1920, 720);
		}

		private void Refresh()
		{
			RefreshLibraries();
			RefreshCategories();
			RefreshAssets();
		}

		private void RefreshLibraries()
		{
			// find library assets
			Libraries = AssetDatabase.FindAssets($"t:{typeof(AssetLibrary)}")
				.Select(AssetDatabase.GUIDToAssetPath)
				.Select(AssetDatabase.LoadAssetAtPath<AssetLibrary>)
				.Where(asset => asset != null).ToList();

			// setup query
			_query = new AssetQuery(Libraries);
			_query.OnQueryUpdated += OnQueryUpdated;

			// update left column
			_libraryList.Clear();
			foreach (var assetLibrary in Libraries) {
				_libraryList.Add(NewAssetLibrary(assetLibrary));
			}

			// update top dropdown
			_activeLibraryDropdown.choices = Libraries.Select(l => l.Name).ToList();
			if (ActiveLibrary != null && Libraries.Count > 0) {
				_activeLibraryDropdown.index = System.Math.Max(0, _activeLibraryDropdown.choices.IndexOf(ActiveLibrary.Name));
			}
		}

		private void RefreshCategories()
		{
			_categoryView.Refresh(this);
		}

		private void RefreshAssets()
		{
			_query.Run();
		}

		private void OnQueryUpdated(object sender, AssetQueryResult e)
		{
			UpdateQueryResults(e.Rows);
		}

		private void UpdateQueryResults(List<AssetData> assets)
		{
			_bottomLabel.text = $"Found {assets.Count} assets.";
			_assets = assets;
			_gridContent.Clear();
			_elementByAsset.Clear();
			_assetsByElement.Clear();
			_selectedAssets.Clear();
			_firstSelectedAsset = null;
			LastSelectedAsset = null;
			foreach (var row in assets) {
				var obj = AssetDatabase.LoadAssetAtPath(row.Asset.Path, AssetLibrary.TypeByName(row.Asset.Type));
				var tex = AssetPreview.GetAssetPreview(obj);
				var element = NewItem(tex, Path.GetFileNameWithoutExtension(row.Asset.Path));
				_elementByAsset[row] = element;
				_assetsByElement[element] = row;
				_gridContent.Add(_elementByAsset[row]);
			}
		}

		private void OnItemClicked(MouseUpEvent evt, VisualElement element)
		{
			var clickedAsset = _assetsByElement[element];

			// no modifier pressed
			if (!evt.shiftKey && !evt.ctrlKey) {
				// already selected?
				if (_selectedAssets.Contains(clickedAsset)) {
					if (_selectedAssets.Count != 1) {
						SelectOnly(clickedAsset);
					} // if count is 1, and user clicks on it, do nothing.
				} else {
					SelectOnly(clickedAsset);
				}
			}

			// only CTRL pressed
			if (!evt.shiftKey && evt.ctrlKey) {
				// already selected?
				if (_selectedAssets.Contains(clickedAsset)) {
					UnSelect(clickedAsset);
				} else {
					Select(clickedAsset);
				}
			}

			// only SHIFT pressed
			if (evt.shiftKey && !evt.ctrlKey) {
				var startIndex = _firstSelectedAsset != null ? _assets.IndexOf(_firstSelectedAsset) : 0;
				var endIndex = _assets.IndexOf(clickedAsset);
				LastSelectedAsset = clickedAsset;
				SelectRange(startIndex, endIndex);
			}


			// both SHIFT and CTRL pressed
			if (evt.shiftKey && evt.ctrlKey) {
				// todo
			}
		}

		#region Selection

		private void SelectRange(int start, int end)
		{
			if (start > end) {
				(start, end) = (end, start);
			}
			for (var i = 0; i < _assets.Count; i++) {
				var asset = _assets[i];
				if (i >= start && i <= end) {
					if (!_selectedAssets.Contains(asset)) {
						_selectedAssets.Add(asset);
						ToggleSelectionClass(_elementByAsset[asset]);
					}
				} else if (_selectedAssets.Contains(asset)) {
					_selectedAssets.Remove(asset);
					ToggleSelectionClass(_elementByAsset[asset]);
				}
			}
		}

		private void SelectOnly(AssetData asset)
		{
			var wasAlreadySelected = false;
			foreach (var selectedAsset in _selectedAssets) {
				if (selectedAsset != asset) {
					ToggleSelectionClass(_elementByAsset[selectedAsset]);
				} else {
					wasAlreadySelected = true;
				}
			}
			_selectedAssets.Clear();
			_selectedAssets.Add(asset);
			if (!wasAlreadySelected) {
				ToggleSelectionClass(_elementByAsset[asset]);
			}
			_firstSelectedAsset = asset;
			LastSelectedAsset = asset;
		}

		private void UnSelect(AssetData asset)
		{
			_selectedAssets.Remove(asset);
			ToggleSelectionClass(_elementByAsset[asset]);
			_firstSelectedAsset = _selectedAssets.Count > 0 ? _selectedAssets.FirstOrDefault() : null;
			LastSelectedAsset = _selectedAssets.Count > 0 ? _selectedAssets.LastOrDefault() : null;
		}


		private void Select(AssetData asset)
		{
			_selectedAssets.Add(asset);
			ToggleSelectionClass(_elementByAsset[asset]);
			LastSelectedAsset = asset;
		}

		private static void ToggleSelectionClass(VisualElement element) => element.ToggleInClassList("selected");

		#endregion Selection

		public void OnCategoriesUpdated(Dictionary<AssetLibrary, List<LibraryCategory>> categories) => _query.Filter(categories);
		private void OnSearchQueryChanged(ChangeEvent<string> evt) => _query.Search(evt.newValue);
		private void OnLibraryToggled(AssetLibrary lib, bool enabled) => _query.Toggle(lib, enabled);

		private void OnDragUpdatedEvent(DragUpdatedEvent evt)
		{
			DragAndDrop.visualMode = DragAndDrop.objectReferences != null
				? DragAndDropVisualMode.Move
				: DragAndDropVisualMode.Copy;
		}

		private void OnDragPerformEvent(DragPerformEvent evt)
		{
			// can only drag onto the asset grid if only one category is selected.
			if (_categoryView.NumSelectedCategories != 1) {
				Debug.Log("Only one category must be selected when dragging onto the main asset panel.");
				return;
			}

			DragAndDrop.AcceptDrag();

			// Disallow adding from outside of Unity
			foreach (var path in DragAndDrop.paths) {
				var libraryFound = false;
				foreach (var assetLibrary in Libraries) {
					if (path.Replace('\\', '/').StartsWith(assetLibrary.LibraryRoot.Replace('\\', '/'))) {
						libraryFound = true;
						var guid = AssetDatabase.AssetPathToGUID(path);
						var type = AssetDatabase.GetMainAssetTypeAtPath(path);
						var category = _categoryView.GetOrCreateSelected(assetLibrary);

						if (assetLibrary.AddAsset(guid, type, path, category)) {
							Debug.Log($"{Path.GetFileName(path)} added to library {assetLibrary.Name}.");
						} else {
							Debug.Log($"{Path.GetFileName(path)} updated in library {assetLibrary.Name}.");
						}

						//Setup();
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
			foreach (var e in _elementByAsset.Values) {
				e.style.width = _thumbnailSize;
				e.style.height = _thumbnailSize;
			}
		}
		private string OnActiveLibraryChanged(string libraryName)
		{
			if (Libraries == null) {
				return libraryName;
			}
			var library = Libraries.FirstOrDefault(l => l.Name == libraryName);
			if (library != null) {
				ActiveLibrary = library;
			}
			return libraryName;
		}

		public void AttachData()
		{
			DragAndDrop.objectReferences = _selectedAssets.Select(row => row.Asset.LoadAsset()).ToArray();
			DragAndDrop.SetGenericData("assets", _selectedAssets);
		}
	}

	public interface IDragHandler
	{
		void AttachData();
	}

}
