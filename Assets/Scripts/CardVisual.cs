using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class CardVisual : MonoBehaviour
{
    private CardController _parentController;

    [Header("UI References")]
    public Image cardArtImage;

    [Header("Animation Settings")]
    [SerializeField] private float hoverScale = 1.15f;
    [SerializeField] private float selectedYOffset = 50f;
    [SerializeField] private float animationDuration = 0.2f;
    [SerializeField] private Ease animationEase = Ease.OutBack;

    private RectTransform _rectTransform;
    private Canvas _canvas;
    private bool _isHovering = false;

    public void Initialize(CardController controller, PlayingCardData cardData)
    {
        _parentController = controller;
        _rectTransform = GetComponent<RectTransform>();
        _canvas = gameObject.AddComponent<Canvas>();
        gameObject.AddComponent<GraphicRaycaster>();

        if (cardArtImage != null && cardData != null)
        {
            cardArtImage.sprite = cardData.cardImage;
        }

        _parentController.PointerEnterEvent.AddListener(OnPointerEnter);
        _parentController.PointerExitEvent.AddListener(OnPointerExit);
        _parentController.SelectEvent.AddListener(OnSelect);
    }

    private void OnDestroy()
    {
        _rectTransform.DOKill();
    }

    private void OnPointerEnter()
    {
        _isHovering = true;
        if (_canvas != null)
        {
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = 10;
        }
        _rectTransform.DOScale(hoverScale, animationDuration).SetEase(animationEase);
    }

    private void OnPointerExit()
    {
        _isHovering = false;
        if (!_parentController.IsSelected)
        {
             if (_canvas != null)
            {
                _canvas.overrideSorting = false;
            }
            _rectTransform.DOScale(1f, animationDuration).SetEase(animationEase);
        }
    }

    private void OnSelect(bool isSelected)
    {
        _rectTransform.DOKill();

        Vector3 targetLocalPosition = Vector3.zero;
        float targetScale;

        if (isSelected)
        {
            targetLocalPosition.y = selectedYOffset;
            targetScale = hoverScale;
            if (_canvas != null)
            {
                _canvas.overrideSorting = true;
                _canvas.sortingOrder = 10;
            }
        }
        else
        {
            targetScale = _isHovering ? hoverScale : 1f;
            if (!_isHovering && _canvas != null)
            {
                _canvas.overrideSorting = false;
            }
        }
        
        _rectTransform.DOLocalMove(targetLocalPosition, animationDuration).SetEase(animationEase);
        _rectTransform.DOScale(targetScale, animationDuration).SetEase(animationEase);
    }
}
