using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using VisualPinball.Engine.Game;
using VisualPinball.Engine.VPT;
using VisualPinball.Unity.Extensions;
using VisualPinball.Unity.VPT;
using VisualPinball.Unity.VPT.Table;

namespace VisualPinball.Unity.Editor.Materials
{
	/// <summary>
	/// Editor UI for VPX materials, equivalent to VPX's "Material Manager" window
	/// </summary>
	public class MaterialEditor : EditorWindow
	{
		private TreeViewTest _treeView;
		private TreeViewState _treeViewState;

		private bool _foldoutVisual = true;
		private bool _foldoutPhysics = true;
		private bool _renaming = false;
		private string _renameBuffer = "";

		private TableBehavior _table;
		private Engine.VPT.Material _selectedMaterial;

		[MenuItem("Visual Pinball/Material Manager", false, 102)]
		public static void ShowWindow()
		{
			GetWindow<MaterialEditor>("Material Manager");
		}

		protected virtual void OnEnable()
		{
			// force gui draw when we perform an undo so we see the fields change back
			Undo.undoRedoPerformed -= UndoPerformed;
			Undo.undoRedoPerformed += UndoPerformed;

			if (_treeViewState == null) {
				_treeViewState = new TreeViewState();
			}

			FindTable();
		}

		protected virtual void OnHierarchyChange()
		{
			// if we don't have a table, look for one when stuff in the scene changes
			if (_table == null) {
				FindTable();
			}
		}

		protected virtual void OnGUI()
		{
			// if the table went away, clear the selected material as well
			if (_table == null) {
				_selectedMaterial = null;
			}

			EditorGUILayout.BeginHorizontal();

			// list
			GUILayout.FlexibleSpace();
			var r = GUILayoutUtility.GetLastRect();
			_treeView.OnGUI(new Rect(0, 0, r.width, position.height));

			// options
			EditorGUILayout.BeginVertical(GUILayout.MaxWidth(300));
			if (_selectedMaterial != null) {
				EditorGUILayout.BeginHorizontal();
				if (_renaming) {
					_renameBuffer = EditorGUILayout.TextField(_renameBuffer);
					if (GUILayout.Button("Save")) {
						Undo.RecordObject(_table, "Rename Material");
						_selectedMaterial.Name = _renameBuffer;
						_renaming = false;
						_treeView.Reload();
					}
					if (GUILayout.Button("Cancel")) {
						_renaming = false;
						GUI.FocusControl(""); // de-focus on cancel because unity will retain previous buffer text until focus changes
					}
				} else {
					EditorGUILayout.LabelField(_selectedMaterial.Name);
					if (GUILayout.Button("Rename")) {
						_renaming = true;
						_renameBuffer = _selectedMaterial.Name;
					}
				}
				EditorGUILayout.EndHorizontal();

				if (_foldoutPhysics = EditorGUILayout.BeginFoldoutHeaderGroup(_foldoutPhysics, "Physics")) {
					EditorGUI.indentLevel++;
					PhysicsOptions();
					EditorGUI.indentLevel--;
				}
				EditorGUILayout.EndFoldoutHeaderGroup();

				if (_foldoutVisual = EditorGUILayout.BeginFoldoutHeaderGroup(_foldoutVisual, "Visual")) {
					EditorGUI.indentLevel++;
					VisualOptions();
					EditorGUI.indentLevel--;
				}
				EditorGUILayout.EndFoldoutHeaderGroup();
			} else {
				EditorGUILayout.LabelField("Select material to edit");
			}
			EditorGUILayout.EndVertical();

			EditorGUILayout.EndHorizontal();
		}

		private void PhysicsOptions()
		{
			FloatField("Elasticity", ref _selectedMaterial.Elasticity);
			FloatField("Elasticity Falloff", ref _selectedMaterial.ElasticityFalloff);
			FloatField("Friction", ref _selectedMaterial.Friction);
			FloatField("Scatter Angle", ref _selectedMaterial.ScatterAngle);
		}

		private void VisualOptions()
		{
			ToggleField("Metal Material", ref _selectedMaterial.IsMetal, "disables Glossy Layer and has stronger reflectivity");
			EditorGUILayout.Space();

			ColorField("Base Color", ref _selectedMaterial.BaseColor, "Steers the basic Color of an Object. Wrap allows for light even if object is only lit from behind (0=natural)");
			SliderField("Wrap", ref _selectedMaterial.WrapLighting);
			EditorGUILayout.Space();

			ColorField("Glossy Layer", ref _selectedMaterial.Glossiness, "Add subtle reflections on non-metal materials (leave at non-black for most natural behavior)");
			SliderField("Use Image", ref _selectedMaterial.GlossyImageLerp);
			EditorGUILayout.Space();

			SliderField("Shininess", ref _selectedMaterial.Roughness, tooltip: "Sets from very dull (Shininess low) to perfect/mirror-like (Shininess high) reflections (for glossy layer or metal only)");
			EditorGUILayout.Space();

			ColorField("Clearcoat Layer", ref _selectedMaterial.ClearCoat, "Add an additional thin clearcoat layer on top of the material");
			EditorGUILayout.Space();

			SliderField("Edge Brightness", ref _selectedMaterial.Edge, tooltip: "Dims the silhouette \"glow\" when using Glossy or Clearcoat Layers (1=natural, 0=dark)");
			EditorGUILayout.Space();

			EditorGUILayout.LabelField(new GUIContent("Opacity", "will be modulated by Image/Alpha channel on Object"));
			EditorGUI.indentLevel++;
			ToggleField("Active", ref _selectedMaterial.IsOpacityActive);
			SliderField("Amount", ref _selectedMaterial.Opacity);
			SliderField("Edge Opacity", ref _selectedMaterial.EdgeAlpha, tooltip: "Increases the opacity on the silhouette (1=natural, 0=no change)");
			SliderField("Thickness", ref _selectedMaterial.Thickness, tooltip: "Interacts with Edge Opacity & Amount, can provide a more natural result for thick materials (1=thick, 0=no change)");
			EditorGUI.indentLevel--;
		}

		private void FloatField(string label, ref float field)
		{
			EditorGUI.BeginChangeCheck();
			float val = EditorGUILayout.FloatField(label, field);
			if (EditorGUI.EndChangeCheck()) {
				Undo.RecordObject(_table, "Edit Material: " + label);
				field = val;
			}
		}

		private void SliderField(string label, ref float field, float min = 0f, float max = 1f, string tooltip = "")
		{
			EditorGUI.BeginChangeCheck();
			float val = EditorGUILayout.Slider(new GUIContent(label, tooltip), field, min, max);
			if (EditorGUI.EndChangeCheck()) {
				Undo.RecordObject(_table, "Edit Material: " + label);
				field = val;
			}
		}

		private void ToggleField(string label, ref bool field, string tooltip = "")
		{
			EditorGUI.BeginChangeCheck();
			bool val = EditorGUILayout.Toggle(new GUIContent(label, tooltip), field);
			if (EditorGUI.EndChangeCheck()) {
				Undo.RecordObject(_table, "Edit Material: " + label);
				field = val;
			}
		}

		private void ColorField(string label, ref Engine.Math.Color field, string tooltip = "")
		{
			EditorGUI.BeginChangeCheck();
			Engine.Math.Color val = EditorGUILayout.ColorField(new GUIContent(label, tooltip), field.ToUnityColor()).ToEngineColor();
			if (EditorGUI.EndChangeCheck()) {
				Undo.RecordObject(_table, "Edit Material: " + label);
				field = val;
			}
		}

		private void UndoPerformed()
		{
			if (_treeView != null) {
				_treeView.Reload();
			}
		}

		private void FindTable()
		{
			_table = GameObject.FindObjectOfType<TableBehavior>();
			_treeView = new TreeViewTest(_treeViewState, _table, MaterialSelected);
		}

		private void MaterialSelected(List<Engine.VPT.Material> selectedMaterials)
		{
			_selectedMaterial = null;
			if (selectedMaterials.Count > 0) {
				_selectedMaterial = selectedMaterials[0]; // TODO: multi select stuff?
				_renaming = false;
			}
			Repaint();
		}
	}

	class TreeViewTest : TreeView // TODO: rename and move to its own file
	{
		public event Action<List<Engine.VPT.Material>> MaterialSelected;

		private TableBehavior _table;

		public TreeViewTest(TreeViewState treeViewState, TableBehavior table, Action<List<Engine.VPT.Material>> materialSelected) : base(treeViewState)
		{
			MaterialSelected += materialSelected;
			_table = table;

			var columns = new[]
			{
				new MultiColumnHeaderState.Column
				{
					headerContent = new GUIContent("Name"),
					headerTextAlignment = UnityEngine.TextAlignment.Left,
					canSort = true,
					sortedAscending = true,
					sortingArrowAlignment = UnityEngine.TextAlignment.Right,
					width = 300,
					minWidth = 100,
					maxWidth = float.MaxValue,
					autoResize = true,
					allowToggleVisibility = false,
				},
				new MultiColumnHeaderState.Column
				{
					headerContent = new GUIContent("In use"),
					headerTextAlignment = UnityEngine.TextAlignment.Left,
					canSort = true,
					sortedAscending = true,
					sortingArrowAlignment = UnityEngine.TextAlignment.Right,
					width = 50,
					minWidth = 50,
					maxWidth = 50,
					autoResize = true,
					allowToggleVisibility = false,
				},
			};

			var headerState = new MultiColumnHeaderState(columns);
			this.multiColumnHeader = new MultiColumnHeader(headerState);
			this.multiColumnHeader.SetSorting(0, true);
			this.multiColumnHeader.sortingChanged += SortingChanged;
			this.showAlternatingRowBackgrounds = true;
			this.showBorder = true;

			Reload();
			if (GetRows().Count > 0) {
				SetSelection(new List<int> { 0 }, TreeViewSelectionOptions.FireSelectionChanged);
			}
		}

		private void SortingChanged(MultiColumnHeader multiColumnHeader)
		{
			Reload();
		}

		public override void OnGUI(Rect rect)
		{
			// if the table went away, force a rebuild to empty out the list
			if (_table == null && GetRows().Count > 0) {
				Reload();
			}
			base.OnGUI(rect);
		}

		protected override TreeViewItem BuildRoot()
		{
			return new TreeViewItem { id = -1, depth = -1, displayName = "Root" };
		}

		protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
		{
			var items = new List<TreeViewItem>();
			if (_table == null) return items;

			// collect list of in use materials
			List<string> inUseMaterials = new List<string>();
			var renderables = _table.GetComponentsInChildren<IItemBehaviorWithMaterials>();
			foreach (var renderable in renderables) {
				var mats = renderable.UsedMaterials;
				if (mats != null) {
					foreach (var mat in mats) {
						if (!string.IsNullOrEmpty(mat)) {
							inUseMaterials.Add(mat);
						}
					}
				}
			}

			// get row data for each material
			for (int i = 0; i < _table.Item.Data.Materials.Length; i++) {
				var mat = _table.Item.Data.Materials[i];
				items.Add(new RowData(i, mat, inUseMaterials.Contains(mat.Name)));
			}

			var sortedColumns = this.multiColumnHeader.state.sortedColumns;
			if (sortedColumns.Length > 0) {
				items.Sort((baseA, baseB) => {
					var a = baseA as RowData;
					var b = baseB as RowData;
					// sort based on multiple columns
					foreach (var column in sortedColumns) {
						bool ascending = multiColumnHeader.IsSortedAscending(column);
						// flip for descending
						if (!ascending) {
							var tmp = b;
							b = a;
							a = tmp;
						}
						int compareResult = 0;
						switch (column) {
							case 0:
								compareResult = a.Material.Name.CompareTo(b.Material.Name);
								break;
							case 1:
								compareResult = a.InUse.CompareTo(b.InUse);
								break;
						}
						// not equal in this column, then return that
						if (compareResult != 0) {
							return compareResult;
						}
					}
					return a.CompareTo(b);
				});
			}

			return items;
		}

		protected override void RowGUI(RowGUIArgs args)
		{
			for (int i = 0; i < args.GetNumVisibleColumns(); ++i) {
				CellGUI(args.GetCellRect(i), args.item, args.GetColumn(i));
			}
		}

		private void CellGUI(Rect cellRect, TreeViewItem item, int column)
		{
			CenterRectUsingSingleLineHeight(ref cellRect);
			var rowData = item as RowData;
			switch (column) {
				case 0: // todo: make an enum for the columns
					GUI.Label(cellRect, rowData.Material.Name);
					break;
				case 1:
					GUI.Label(cellRect, rowData.InUse ? "X" : "");
					break;
			}
		}

		protected override void SelectionChanged(IList<int> selectedIds)
		{
			List<Engine.VPT.Material> selectedMats = new List<Engine.VPT.Material>();
			var rows = GetRows();
			foreach (var row in rows) {
				if (selectedIds.Contains(row.id)) {
					selectedMats.Add((row as RowData).Material);
				}
			}
			MaterialSelected?.Invoke(selectedMats);
		}

		private class RowData : TreeViewItem
		{
			public readonly Engine.VPT.Material Material;
			public readonly bool InUse;

			public RowData(int id, Engine.VPT.Material mat, bool inUse) : base(id, 0) {
				Material = mat;
				InUse = inUse;
			}
		}
	}
}
