﻿// ReSharper disable StaticMemberInGenericType

using System;
using System.Collections.Generic;
using System.Linq;
using NLog;

namespace VisualPinball.Engine.Common
{
	public static class EngineProvider<T> where T : IEngine
	{
		public static bool Exists { get; private set; }

		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		private static T _selectedEngine;
		private static Dictionary<string, T> _availableEngines;

		public static IEnumerable<T> GetAll()
		{
			var t = typeof(T);

			if (_availableEngines == null) {
				var engines = AppDomain.CurrentDomain.GetAssemblies()
					.Where(x => x.FullName.StartsWith("VisualPinball."))
					.SelectMany(x => x.GetTypes())
					.Where(x => x.IsClass && t.IsAssignableFrom(x))
					.Select(x => (T) Activator.CreateInstance(x));

				_availableEngines = new Dictionary<string, T>();
				foreach (var engine in engines) {
					_availableEngines[GetId(engine)] = engine;
				}

				// be kind: if there's only one, set it.
				if (_availableEngines.Count == 1) {
					_selectedEngine = _availableEngines.Values.First();
				}
			}
			return _availableEngines.Values;
		}

		public static void Set(string id)
		{
			if (id == null) {
				return;
			}
			if (_availableEngines == null) {
				GetAll();
			}
			if (!_availableEngines.ContainsKey(id)) {
				throw new ArgumentException($"Unknown {typeof(T)} engine {id} (available: [ {string.Join(", ", _availableEngines.Keys)} ]).");
			}
			_selectedEngine = _availableEngines[id];
			Logger.Info("Set {0} engine to {1}.", typeof(T), id);
			Exists = true;
		}

		public static T Get()
		{
			if (_selectedEngine == null) {
				throw new InvalidOperationException($"Must select {typeof(T)} engine before retrieving!");
			}
			return _selectedEngine;
		}

		public static string GetId(object obj)
		{
			return obj.GetType().FullName;
		}
	}

	public interface IEngine
	{
		string Name { get; }
	}
}
