using System;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using VisualPinball.Unity.Editor.Utils.TreeView;
using UnityObject = UnityEngine.Object;

namespace VisualPinball.Unity.Editor.Layers
{
	/// <summary>
	/// Populates the VPX Layers structure as provided by the <see cref="LayerHandler"/>. <p/>
	///
	/// It notifies the LayerHandler of any change in the layers structure, such as addition, removal, renaming,
	/// or toggling.
	/// </summary>
	///
	/// <remarks>
	/// It a first structure draft mirroring the table structure for now, will be changed to fit the LayersHandler
	/// afterwards.
	/// </remarks>
	internal class LayerTreeView : TreeView<LayerTreeElement>
	{
		/// <summary>
		/// Emitted when a layer is renamed.
		/// </summary>
		public event Action<int, string> LayerRenamed = delegate { };

		public LayerTreeView(LayerTreeElement root) : base(new TreeViewState(), root)
		{
			showBorder = true;
			showAlternatingRowBackgrounds = true;
			customFoldoutYOffset = 3f;
		}

		// Custom GUI
		protected override void RowGUI(RowGUIArgs args)
		{
			Event evt = Event.current;
			extraSpaceBeforeIconAndLabel = 18f;

			// Visibility Toggle
			Rect toggleRect = args.rowRect;
			toggleRect.x += GetContentIndent(args.item);
			toggleRect.width = 16f;

			// Ensure row is selected before using the toggle (usability)
			if (evt.type == EventType.MouseDown && toggleRect.Contains(evt.mousePosition))
				SelectionClick(args.item, false);

			if (args.item is TreeViewItem<LayerTreeElement> treeViewItem) {
				EditorGUI.BeginChangeCheck();
				bool isVisible = EditorGUI.Toggle(toggleRect, treeViewItem.Data.IsVisible);
				if (EditorGUI.EndChangeCheck()) {
					treeViewItem.Data.IsVisible = isVisible;
				}
			}

			// Text
			base.RowGUI(args);
		}

		internal void OnTreeRebuilt()
		{
			Reload();
		}

		protected override bool ValidateRename(TreeViewItem<LayerTreeElement> item) => item.Data?.Type == LayerTreeViewElementType.Layer;

		protected override void RenameEnded(RenameEndedArgs args)
		{
			// Set the backend name and reload the tree to reflect the new model
			if (args.acceptedRename) {
				LayerRenamed(args.itemID, args.newName);
				Reload();
			}
		}
 	}
}
