using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace TPSBR
{
	[Serializable]
	public sealed class OptionsData
	{
		// PUBLIC MEMBERS

		public List<OptionsValue> Values => _allValues;

		// PRIVATE MEMBERS

		[SerializeField]
		private List<OptionsValue> _values;

		[NonSerialized]
		private List<OptionsValue> _allValues = new List<OptionsValue>(64);

		private bool _isInitialized;

		// PUBLIC METHODS

		public void Initialize()
		{
			if (_isInitialized == true)
				return;

			_allValues.AddRange(_values);
			_isInitialized = true;
		}

		public void AddRuntimeValue(OptionsValue value)
		{
			Assert.IsTrue(_isInitialized);

			if (Contains(value.Key) == true)
				return;

			_allValues.Add(value);
		}

		public bool Contains(string key)
		{
			Assert.IsTrue(_isInitialized);

			return TryGet(key, out OptionsValue value);
		}

		public bool TryGet(string key, out OptionsValue value)
		{
			Assert.IsTrue(_isInitialized);

			value = default;

			for (int i = 0; i < _allValues.Count; i++)
			{
				if (_allValues[i].Key == key)
				{
					value = _allValues[i];
					return true;
				}
			}

			return false;
		}
	}

	[Serializable]
	public unsafe struct OptionsValue
	{
		public string            Key;
		public EOptionsValueType Type;
		public bool              BoolValue;
		public OptionsValueFloat FloatValue;
		public OptionsValueInt   IntValue;
		public string            StringValue;

		public OptionsValue(string key, EOptionsValueType type) : this()
		{
			Key  = key;
			Type = type;
		}

		public OptionsValue(string key, bool value) : this()
		{
			Key       = key;
			Type      = EOptionsValueType.Bool;
			BoolValue = value;
		}

		public OptionsValue(string key, int value) : this()
		{
			Key            = key;
			Type           = EOptionsValueType.Int;
			IntValue.Value = value;
		}

		public OptionsValue(string key, float value) : this()
		{
			Key              = key;
			Type             = EOptionsValueType.Float;
			FloatValue.Value = value;
		}

		public OptionsValue(string key, string value) : this()
		{
			Key         = key;
			Type        = EOptionsValueType.String;
			StringValue = value;
		}

		public bool Equals(OptionsValue other)
		{
			if (Key != other.Key)
				return false;

			if (Type != other.Type)
				return false;

			switch (Type)
			{
				case EOptionsValueType.Bool:
					return BoolValue == other.BoolValue;
				case EOptionsValueType.Float:
					return FloatValue.Value == other.FloatValue.Value;
				case EOptionsValueType.Int:
					return IntValue.Value == other.IntValue.Value;
				case EOptionsValueType.String:
					return StringValue == other.StringValue;
			}

			return true;
		}
	}

	public enum EOptionsValueType : byte
	{
		None,
		Bool,
		Float,
		Int,
		String,
	}

	[Serializable]
	public struct OptionsValueFloat
	{
		public float Value;
		public float MinValue;
		public float MaxValue;
	}

	[Serializable]
	public struct OptionsValueInt
	{
		public int Value;
		public int MinValue;
		public int MaxValue;
	}
}
