using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class CardController : MonoBehaviour, IPointerUpHandler, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Visual Prefab")]
    [SerializeField] private GameObject cardVisualPrefab;
    private CardVisual _cardVisual;
    public bool IsSelected { get; private set; } = false;
    private bool _isHovering = false;

    // --- Internal Data ---
    private NetworkPlayingCard _networkCard;
    private HandDisplayUI _handDisplayManager;

    // --- Events ---
    public UnityEvent PointerEnterEvent = new UnityEvent();
    public UnityEvent PointerExitEvent = new UnityEvent();
    public UnityEvent<bool> SelectEvent = new UnityEvent<bool>();

    public void Initialize(PlayingCardData cardData, NetworkPlayingCard networkCard, HandDisplayUI handDisplayManager)
    {
        _networkCard = networkCard;
        _handDisplayManager = handDisplayManager;

        if (cardVisualPrefab != null)
        {
            GameObject visualGO = Instantiate(cardVisualPrefab, this.transform);
            
            // --- Key Fix ---
            // Ensure the position of the newly created visual card perfectly overlaps with the logical card
            visualGO.transform.localPosition = Vector3.zero;
            visualGO.transform.localRotation = Quaternion.identity;
            
            _cardVisual = visualGO.GetComponent<CardVisual>();
            if (_cardVisual != null)
            {
                _cardVisual.Initialize(this, cardData); 
            }
        }
    }

    // ... (All other methods remain unchanged) ...

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovering = true;
        PointerEnterEvent.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovering = false;
        PointerExitEvent.Invoke();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Can trigger press animation here
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        
        IsSelected = !IsSelected;
        SelectEvent.Invoke(IsSelected);
        
        _handDisplayManager.OnCardSelectionChanged(this, IsSelected);
    }

    public NetworkPlayingCard GetNetworkCard()
    {
        return _networkCard;
    }
    
    public void Deselect()
    {
        if (IsSelected)
        {
            IsSelected = false;
            SelectEvent.Invoke(false);
        }
    }

    private void OnDestroy()
    {
        if (_cardVisual != null)
        {
            Destroy(_cardVisual.gameObject);
        }
    }
}