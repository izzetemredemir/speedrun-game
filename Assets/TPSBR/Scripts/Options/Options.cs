using System;
using System.Collections.Generic;
using UnityEngine;

namespace TPSBR
{
	public sealed class Options
	{
		// PUBLIC MEMBERS

		public bool         HasUnsavedChanges { get { return _dirtyValues.Count > 0; } }
		public string       PersistencyPrefix { get { return _persistencyPrefix; } }

		public event Action ChangesSaved;
		public event Action ChangesDiscarded;

		// PRIVATE MEMBERS

		private Dictionary<string, OptionsValue> _values      = new Dictionary<string, OptionsValue>(128);
		private Dictionary<string, OptionsValue> _dirtyValues = new Dictionary<string, OptionsValue>(128);

		private OptionsData  _optionsData;
		private bool         _enablePersistency;
		private string       _persistencyPrefix;

		// PUBLIC METHODS

		public void Initialize(OptionsData optionsData, bool enablePersistency, string persistencyPrefix)
		{
			_optionsData       = optionsData;
			_enablePersistency = enablePersistency;
			_persistencyPrefix = persistencyPrefix;

			optionsData.Initialize();

			LoadValues();
		}

		public void SaveChanges()
		{
			if (_dirtyValues.Count == 0)
				return;

			foreach (var pair in _dirtyValues)
			{
				var value = pair.Value;

				AddValue(value);

				if (_enablePersistency == false)
					continue;

				if (_optionsData.TryGet(value.Key, out OptionsValue defaultValue) == false)
					continue; // Only values that are stored in options data are stored as persistent

				string key = _persistencyPrefix + value.Key;

				if (value.Equals(defaultValue) == true)
				{
					// Value is default, remove it from persistent storage
					PersistentStorage.Delete(key);
					continue;
				}

				switch (value.Type)
				{
					case EOptionsValueType.Bool:
						PersistentStorage.SetBool(key, value.BoolValue, false);
						break;
					case EOptionsValueType.Float:
						PersistentStorage.SetFloat(key, value.FloatValue.Value, false);
						break;
					case EOptionsValueType.Int:
						PersistentStorage.SetInt(key, value.IntValue.Value, false);
						break;
					case EOptionsValueType.String:
						PersistentStorage.SetString(key, value.StringValue, false);
						break;
				}
			}

			if (_enablePersistency == true)
			{
				PersistentStorage.Save();
			}

			_dirtyValues.Clear();

			ChangesSaved?.Invoke();
		}

		public void DiscardChanges()
		{
			_dirtyValues.Clear();

			ChangesDiscarded?.Invoke();
		}

		public void ResetValueToDefault(string key, bool saveImmediately)
		{
			if (string.IsNullOrEmpty(key) == true)
				return;

			if (_optionsData.TryGet(key, out OptionsValue value) == true)
			{
				Set(value.Key, value, saveImmediately);
			}
		}

		public void ResetAllValuesToDefault()
		{
			var values = _optionsData.Values;

			for (int i = 0; i < values.Count; i++)
			{
				var value = values[i];
				Set(value.Key, value, false);
			}

			SaveChanges();
		}

		public void AddDefaultValue(OptionsValue value)
		{
			if (_values.ContainsKey(value.Key) == true)
				return;

			_optionsData.AddRuntimeValue(value);
			LoadValue(value);
		}

		public OptionsValue GetValue(string key)
		{
			if (_dirtyValues.TryGetValue(key, out OptionsValue dirtyValue) == true)
				return dirtyValue;

			if (_values.TryGetValue(key, out OptionsValue value) == true)
				return value;

			Debug.LogError($"Missing options value with key {key}");

			return default;
		}

		public bool GetBool(string key)
		{
			return GetValue(key).BoolValue;
		}

		public float GetFloat(string key)
		{
			return GetValue(key).FloatValue.Value;
		}

		public int GetInt(string key)
		{
			return GetValue(key).IntValue.Value;
		}

		public string GetString(string key)
		{
			return GetValue(key).StringValue;
		}

		public void Set<T>(string key, T value, bool saveImmediately)
		{
			if (string.IsNullOrEmpty(key) == true)
			{
				Debug.LogError("PlayerOptions.Set - Missing key");
				return;
			}

			OptionsValue originalValue = default;

			_values.TryGetValue(key, out originalValue);

			OptionsValue newValue = originalValue;

			if (value is bool boolValue)
			{
				newValue.Type = EOptionsValueType.Bool;
				newValue.BoolValue = boolValue;
			}
			else if (value is float floatValue)
			{
				newValue.Type = EOptionsValueType.Float;
				newValue.FloatValue.Value = floatValue;
			}
			else if (value is int intValue)
			{
				newValue.Type = EOptionsValueType.Int;
				newValue.IntValue.Value = intValue;
			}
			else if (value is string stringValue)
			{
				newValue.Type = EOptionsValueType.String;
				newValue.StringValue = stringValue;
			}
			else if (value is OptionsValue optionsValueNew)
			{
				newValue = optionsValueNew;
			}
			else
			{
				throw new NotSupportedException(string.Format("Unsupported type, Type: {0} Key: {1}", typeof(T), key));
			}

			if (newValue.Equals(originalValue) == true)
			{
				_dirtyValues.Remove(key); // Remove previous modification if exists
				return;
			}

			if (originalValue.Type == EOptionsValueType.None || originalValue.Type == newValue.Type)
			{
				_dirtyValues[newValue.Key] = newValue;
			}
			else
			{
				Debug.LogError($"Trying to write incorrect type of value, Type: {typeof(T)} Key: {key}");
			}

			if (saveImmediately == true)
			{
				SaveChanges();
			}
		}

		public bool IsDirty(string key)
		{
			return _dirtyValues.ContainsKey(key);
		}

		// PRIVATE METHODS

		private void AddValue(OptionsValue value)
		{
			_values[value.Key] = value;
		}

		private void LoadValues()
		{
			_values.Clear();
			_dirtyValues.Clear();

			var values = _optionsData.Values;

			for (int i = 0; i < values.Count; i++)
			{
				LoadValue(values[i]);
			}
		}

		private void LoadValue(OptionsValue value)
		{
			if (value.Type == EOptionsValueType.None || string.IsNullOrEmpty(value.Key) == true)
			{
				Debug.LogError($"Incorrect options value, Type: {value.Type} Key: {value.Key}");
				return;
			}

			if (_enablePersistency == true)
			{
				string key = _persistencyPrefix + value.Key;

				switch (value.Type)
				{
					case EOptionsValueType.Bool:
						value.BoolValue = PersistentStorage.GetBool(key, value.BoolValue);
						break;
					case EOptionsValueType.Float:
						value.FloatValue.Value = PersistentStorage.GetFloat(key, value.FloatValue.Value);
						break;
					case EOptionsValueType.Int:
						value.IntValue.Value = PersistentStorage.GetInt(key, value.IntValue.Value);
						break;
					case EOptionsValueType.String:
						value.StringValue = PersistentStorage.GetString(key, value.StringValue);
						break;
				}
			}

			AddValue(value);
		}
	}
}
