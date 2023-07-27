using UnityEngine;
using UnityEditor;
using TPSBR;

[CustomEditor(typeof(RecoilPattern))]
public class RecoilPatternEditor : UnityEditor.Editor
{
	private int _textureSize;
	private Texture2D _texture;
	private Color[] _pixels;

	private Vector2 _inputValue = Vector2.one;

	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		var rect = GetTextureRect();
		if (rect.width > 1f && (int)rect.width != _textureSize)
		{
			_textureSize = (int)rect.width;
			_pixels = new Color[_textureSize * _textureSize];
		}

		if (Event.current.type == EventType.Repaint)
		{
			_texture = GetTexture();
		}

		GUILayout.Box(_texture, GUILayout.Width(_textureSize), GUILayout.Height(_textureSize));

		GUILayout.Label("Batch Change", EditorStyles.boldLabel);
		_inputValue = EditorGUILayout.Vector2Field("Input Value", _inputValue);

		if (GUILayout.Button("Multiply Start Values") == true)
		{
			Undo.RecordObject(target, "Multiply Start Values");

			var pattern = target as RecoilPattern;

			for (int i = 0; i < pattern.RecoilStartValues.Length; i++)
			{
				pattern.RecoilStartValues[i] *= _inputValue;
			}

			EditorUtility.SetDirty(target);
		}

		if (GUILayout.Button("Multiply Endless Values") == true)
		{
			Undo.RecordObject(target, "Multiply Endless Values");

			var pattern = target as RecoilPattern;

			for (int i = 0; i < pattern.RecoilEndlessValues.Length; i++)
			{
				pattern.RecoilEndlessValues[i] *= _inputValue;
			}

			EditorUtility.SetDirty(target);
		}
	}

	// PRIVATE METHODS

	private Rect GetTextureRect()
	{
		var lastRect = GUILayoutUtility.GetLastRect();

		var textureRect = new Rect();
		textureRect.width = lastRect.width * 0.9f;
		textureRect.height = textureRect.width;
		textureRect.position = lastRect.position + new Vector2(lastRect.width * 0.05f, lastRect.height + 10f);

		return textureRect;
	}

	private Texture2D GetTexture()
	{
		var texture = new Texture2D(_textureSize, _textureSize);

		if (_textureSize < 10)
			return texture;

		var pattern = target as RecoilPattern;

		FillColor(Color.black);

		int center = (int) (_textureSize * 0.5f);
		int bottom = (int) (_textureSize * 0.1f);
		int top = _textureSize - bottom;

		DrawVerticalLine(center);
		DrawHorizontalLine(bottom);

		int textureRange = top - bottom;
		int minRange = 7;
		int range = Mathf.Max(Mathf.CeilToInt(GetRange(pattern)), minRange);

		float scale = Mathf.Max(1f, textureRange / range);

		Vector2 position = new Vector2Int(center, bottom);

		// Center point
		DrawPoint((int)position.x, (int)position.y);

		// Start pattern
		int count = pattern.RecoilStartValues.SafeCount();
		for (int i = 0; i < count; i++)
		{
			position += pattern.RecoilStartValues[i] * scale;

			if (i == count - 1 && pattern.RecoilEndlessValues.SafeCount() > 0)
			{
				DrawHorizontalLine((int)position.y, true);
			}

			DrawPoint((int)position.x, (int)position.y);
		}

		// Endless pattern
		count = pattern.RecoilEndlessValues.SafeCount();
		for (int i = 0; i < count; i++)
		{
			position += pattern.RecoilEndlessValues[i] * scale;

			if (i == pattern.RecoilEndlessValues.Length - 1)
			{
				DrawHorizontalLine((int)position.y, true);
			}

			DrawPoint((int)position.x, (int)position.y);
		}

		// Continue with endless pattern
		for (int i = 0; i < count; i++)
		{
			position += pattern.RecoilEndlessValues[i] * scale;
			DrawPoint((int)position.x, (int)position.y);
		}

		texture.SetPixels(_pixels);
		texture.Apply();

		return texture;
	}

	private void DrawVerticalLine(int x)
	{
		for (int y = 0; y < _textureSize; y++)
		{
			SetPixel(x, y, Color.blue);
		}
	}

	private void DrawHorizontalLine(int y, bool dotted = false)
	{
		int dotCount = 0;
		bool draw = true;

		for (int x = 0; x < _textureSize; x++)
		{
			if (dotted == true && dotCount > 5)
			{
				draw = !draw;
				dotCount = 0;
			}

			dotCount++;

			if (draw == true)
			{
				SetPixel(x, y, Color.blue);
			}
		}
	}

	private void DrawPoint(int x, int y)
	{
		SetPixel(x, y, Color.red);
		SetPixel(x + 1, y + 1, Color.red);
		SetPixel(x - 1, y + 1, Color.red);
		SetPixel(x + 1, y - 1, Color.red);
		SetPixel(x - 1, y - 1, Color.red);
	}

	private void SetPixel(int x, int y, Color color)
	{
		if (x < 0 || y < 0)
			return;

		if (x >= _textureSize || y >= _textureSize)
			return;

		_pixels[x + y * _textureSize] = color;
	}

	private void FillColor(Color color)
	{
		for (int i = 0; i < _pixels.Length; i++)
		{
			_pixels[i] = color;
		}
	}

	private float GetRange(RecoilPattern recoil)
	{
		float range = 0f;

		for (int i = 0; i < recoil.RecoilStartValues.SafeCount(); i++)
		{
			range += recoil.RecoilStartValues[i].y;
		}

		for (int i = 0; i < recoil.RecoilEndlessValues.SafeCount(); i++)
		{
			range += recoil.RecoilEndlessValues[i].y;
		}

		return range;
	}
}
