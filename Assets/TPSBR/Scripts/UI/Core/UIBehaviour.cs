using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TPSBR.UI
{
	public class UIBehaviour : CoreBehaviour
	{
		// PUBLIC MEMBERS

		public CanvasGroup CanvasGroup
		{
			get
			{
				if (_canvasGroupChecked == false)
				{
					_cachedCanvasGroup = GetComponent<CanvasGroup>();
					_canvasGroupChecked = true;
				}

				return _cachedCanvasGroup;
			}
		}

		public RectTransform RectTransform
		{
			get
			{
				if (_rectTransformChecked == false)
				{
					_cachedRectTransform = transform as RectTransform;
					_rectTransformChecked = true;
				}

				return _cachedRectTransform;
			}
		}

		public Image Image
		{
			get
			{
				if (_imageChecked == false)
				{
					_cachedImage = GetComponent<Image>();
					_imageChecked = true;
				}

				return _cachedImage;
			}
		}

		public Animation Animation
		{
			get
			{
				if (_animationChecked == false)
				{
					_cachedAnimation = GetComponent<Animation>();
					_animationChecked = true;
				}

				return _cachedAnimation;
			}
		}

		public Animator Animator
		{
			get
			{
				if (_animatorChecked == false)
				{
					_cachedAnimator = GetComponent<Animator>();
					_animatorChecked = true;
				}

				return _cachedAnimator;
			}
		}
		
		public TextMeshProUGUI Text
		{
			get
			{
				if (_textChecked == false)
				{
					_cachedText = GetComponent<TextMeshProUGUI>();
					_textChecked = true;
				}
				
				return _cachedText;
			}
		}

		// PRIVATE MEMBERS

		private CanvasGroup _cachedCanvasGroup;
		private bool _canvasGroupChecked;

		private RectTransform _cachedRectTransform;
		private bool _rectTransformChecked;

		private Image _cachedImage;
		private bool _imageChecked;

		private Animation _cachedAnimation;
		private bool _animationChecked;

		private Animator _cachedAnimator;
		private bool _animatorChecked;
		
		private TextMeshProUGUI _cachedText;
		private bool _textChecked;
	}
}
