﻿// MIT License
//
// Copyright (c) 2019 James
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//
// https://github.com/PassivePicasso/VisualTemplates/tree/master/Editor

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace VisualPinball.Unity.Editor
{
	public class SearchSuggest : VisualElement
	{
		internal enum ShowMode
		{
			// Show as a normal window with max, min & close buttons.
			NormalWindow = 0,
			// Used for a popup menu. On mac this means light shadow and no titlebar.
			PopupMenu = 1,
			// Utility window - floats above the app. Disappears when app loses focus.
			Utility = 2,
			// Window has no shadow or decorations. Used internally for dragging stuff around.
			NoShadow = 3,
			// The Unity main window. On mac, this is the same as NormalWindow, except window doesn't have a close button.
			MainWindow = 4,
			// Aux windows. The ones that close the moment you move the mouse out of them.
			AuxWindow = 5,
			// Like PopupMenu, but without keyboard focus
			Tooltip = 6,
			// Modal Utility window
			ModalUtility = 7
		}

		public new class UxmlFactory : UxmlFactory<SearchSuggest, UxmlTraits>
		{
			public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription {
				get {
					yield return new UxmlChildElementDescription(typeof(SuggestOption));
					yield return new UxmlChildElementDescription(typeof(SuggestOptions));
				}
			}

			public override VisualElement Create(IUxmlAttributes bag, CreationContext cc)
			{
				var searchSuggest = (SearchSuggest)base.Create(bag, cc);
				return searchSuggest;
			}
		}

		public new class UxmlTraits : VisualElement.UxmlTraits
		{
		}

		public delegate void SuggestionSelected(SuggestOption pickedSuggestion);

		public event SuggestionSelected OnSuggestedSelected;

		public List<SuggestOption> MatchedSuggestOption { get; set; }
		private Func<SuggestOption, bool> _matchingSuggestOptions;
		public SuggestOption[] SuggestOption { get; set; }

		private EditorWindow _popupWindow;
		private ListView _optionList;
		private readonly ToolbarSearchField _textEntry;

		private PropertyInfo _ownerObjectProperty;
		private PropertyInfo _screenPositionProperty;
		private MethodInfo _showPopupNonFocus;
		private object[] _showValueArray;
		private object _ownerObject;
		private object _showValue;

		private bool _hasFocus = false;
		private bool _popupVisible = false;
		private Rect _popupPosition;

		public SearchSuggest()
		{
			AddToClassList("search-suggest");

			_textEntry = new ToolbarSearchField { name = "search-suggest-input" };
			MatchedSuggestOption = new List<SuggestOption>();

			ConfigureOptionList();


			_textEntry.style.flexGrow = 1;

			_matchingSuggestOptions = suggestOption => suggestOption.DisplayName.ToLower().Contains(_textEntry.value.ToLower());

			RegisterCallback<AttachToPanelEvent>(OnAttached);
			RegisterCallback<DetachFromPanelEvent>(OnDetached);

			Add(_textEntry);
		}

		private void CreateNewWindow()
		{
			if (_popupWindow == null) {
				_popupWindow = ScriptableObject.CreateInstance<EditorWindow>();
				_popupWindow.rootVisualElement.hierarchy
					.Add(_optionList);
			}
		}

		private void ConfigureOptionList()
		{
			if (_optionList == null) {
				_optionList = new ListView { name = "search-suggest-list", fixedItemHeight = 20 };

				_optionList.makeItem = () => {
					var label = new Label();
					label.AddToClassList("suggestion");
					label.RegisterCallback<MouseDownEvent>(OnLabelMouseDown);

					return label;
				};
				_optionList.bindItem = (v, i) => {
					Label label = v as Label;
					var suggestOption = (SuggestOption)_optionList.itemsSource[i];

					label.text = suggestOption.DisplayName;
					label.userData = suggestOption;
				};
				_optionList.selectionType = SelectionType.Single;
				OppahOptionStyle(_optionList);
			}
		}

		private void OppahOptionStyle(VisualElement element)
		{
			element.style.left = 0;
			element.style.right = 0;
			element.style.height = 100;
			element.style.backgroundColor = Color.Lerp(Color.gray, Color.white, 0.5f);

			element.style.borderTopWidth =
				element.style.borderLeftWidth =
					element.style.borderRightWidth =
						element.style.borderBottomWidth = 1;

			element.style.borderTopColor =
				element.style.borderLeftColor =
					element.style.borderRightColor =
						element.style.borderBottomColor
							= Color.Lerp(Color.gray, Color.black, 0.3f);
		}

		private void OnDetached(DetachFromPanelEvent evt)
		{

			_textEntry.UnregisterValueChangedCallback(OnTextChanged);
			_textEntry.UnregisterCallback<FocusOutEvent>(OnLostFocus);
			_textEntry.UnregisterCallback<FocusInEvent>(OnGainedFocus);
			_textEntry.UnregisterCallback<KeyDownEvent>(OnKeyDown);

			Cleanup();
		}

		private void OnAttached(AttachToPanelEvent evt)
		{
			_ownerObjectProperty = evt.destinationPanel.GetType().GetProperty("ownerObject");
			_ownerObject = _ownerObjectProperty.GetValue(evt.destinationPanel);

			_screenPositionProperty = _ownerObject.GetType().GetProperty("screenPosition");

			var showMode = typeof(EditorWindow).Assembly.GetType("UnityEditor.ShowMode");

			_showPopupNonFocus = typeof(EditorWindow).GetMethod("ShowPopupWithMode", BindingFlags.Instance | BindingFlags.NonPublic);

			_showValue = Enum.GetValues(showMode).GetValue((int)ShowMode.Tooltip);
			_showValueArray = new[] { _showValue, false };

			// var suggestOptions = Children().OfType<SuggestOptions>().SelectMany(sos => sos.Options)
			// 	.Union(Children().OfType<SuggestOption>()).ToArray();

			//SuggestOption = suggestOptions;


			_textEntry.RegisterValueChangedCallback(OnTextChanged);
			_textEntry.RegisterCallback<FocusOutEvent>(OnLostFocus);
			_textEntry.RegisterCallback<FocusInEvent>(OnGainedFocus);
			_textEntry.RegisterCallback<GeometryChangedEvent>(OnGeometryChange);
			_textEntry.RegisterCallback<KeyDownEvent>(OnKeyDown);
		}

		private void OnKeyDown(KeyDownEvent evt)
		{

			switch (evt.keyCode) {
				case KeyCode.Return:
				case KeyCode.KeypadEnter:
					var suggestOption = MatchedSuggestOption[_optionList.selectedIndex];
					OnSuggestedSelected?.Invoke(suggestOption);
					_textEntry.SetValueWithoutNotify("");
					_hasFocus = false;
					UpdateVisibility();
					return;

				case KeyCode.UpArrow:
					evt.PreventDefault();
					if (_optionList.selectedIndex > 0)
						_optionList.selectedIndex--;
					break;
				case KeyCode.DownArrow:
					evt.PreventDefault();
					if (_optionList.selectedIndex < MatchedSuggestOption.Count - 1)
						_optionList.selectedIndex++;
					break;
				default:
					_optionList.selectedIndex = -1;
					break;
			}

			_optionList.ScrollToItem(_optionList.selectedIndex);
		}

		private void OnGeometryChange(GeometryChangedEvent evt)
		{
			UpdatePosition();
		}

		private void OnGainedFocus(FocusInEvent evt)
		{
			_hasFocus = true;
			UpdateOptionList();
			UpdateVisibility();
			UpdatePosition();
		}

		private void OnLostFocus(FocusOutEvent evt)
		{
			if (evt.relatedTarget == null) return;
			_hasFocus = false;
			UpdateVisibility();
		}

		private void OnTextChanged(ChangeEvent<string> evt)
		{
			UpdateOptionList();
			UpdateVisibility();
			UpdatePosition();
		}

		private void UpdatePosition()
		{
			if (_popupWindow == null) return;
			var worldSpaceTextLayout = _textEntry.LocalToWorld(_textEntry.layout);

			var windowPosition = (Rect)_screenPositionProperty.GetValue(_ownerObject);

			var topLeft = windowPosition.position + worldSpaceTextLayout.position;
			topLeft = new Vector2(topLeft.x - 3, topLeft.y + worldSpaceTextLayout.height);
			_popupPosition = new Rect(topLeft, new Vector2(worldSpaceTextLayout.width, 100));


			_popupWindow.rootVisualElement.style.height = 100;
			_popupWindow.rootVisualElement.style.width = worldSpaceTextLayout.width;

			_popupWindow.position = _popupPosition;
		}

		private void UpdateOptionList()
		{
			MatchedSuggestOption.Clear();
			_optionList.itemsSource = MatchedSuggestOption;
			_optionList.selectedIndex = -1;

			if (string.IsNullOrEmpty(_textEntry.value)) {
				_optionList.Rebuild();
				return;
			}

			MatchedSuggestOption.AddRange(SuggestOption.Where(_matchingSuggestOptions));

			_optionList.Rebuild();
		}

		private void UpdateVisibility()
		{
			if (_hasFocus && _optionList.itemsSource.Count > 0) {
				if (_popupVisible) return;
				CreateNewWindow();
				_showPopupNonFocus.Invoke(_popupWindow, _showValueArray);
				_popupVisible = true;
			} else Cleanup();
		}

		private void Cleanup()
		{
			_optionList.RemoveFromHierarchy();
			if (_popupWindow != null) {
				_popupWindow.Close();
				ScriptableObject.DestroyImmediate(_popupWindow);
			}
			_popupVisible = false;
		}

		private void OnLabelMouseDown(MouseDownEvent evt)
		{
			var pickedLabel = evt.target as VisualElement;
			var suggestOption = (SuggestOption)pickedLabel.userData;
			OnSuggestedSelected?.Invoke(suggestOption);
			_textEntry.SetValueWithoutNotify("");
			_hasFocus = false;
			UpdateVisibility();
		}
	}

	public class SuggestOption : VisualElement
	{
		public string DisplayName { get; set; }

		public object Data { get; set; }
		public new class UxmlFactory : UxmlFactory<SuggestOption, UxmlTraits> { }
		public new class UxmlTraits : BindableElement.UxmlTraits
		{
			UxmlStringAttributeDescription _mDisplayName = new UxmlStringAttributeDescription { name = "display-name" };

			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);

				var suggestOption = (SuggestOption)ve;

				suggestOption.DisplayName = _mDisplayName.GetValueFromBag(bag, cc);
			}

			public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
			{
				get { yield break; }
			}
		}
	}

	public abstract class SuggestOptions : VisualElement
	{
		public virtual IEnumerable<SuggestOption> Options => Children().OfType<SuggestOption>();
		//public new class UxmlFactory : UxmlFactory<SuggestOptions, UxmlTraits> { }
		public new class UxmlTraits : BindableElement.UxmlTraits
		{
			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);
			}

			public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
			{
				get
				{
					yield return new UxmlChildElementDescription(typeof(SuggestOption));
					yield break;
				}
			}
		}
	}
}
