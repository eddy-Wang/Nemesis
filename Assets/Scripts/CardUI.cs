using UnityEngine;

using UnityEngine.UI;

using TMPro;

using UnityEngine.EventSystems;

public class CardUI : MonoBehaviour

    // ,IDragHandler, IBeginDragHandler, IEndDragHandler,

    // IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler,

    // IPointerDownHandler

{

    public Image cardArtImage;

    public TMP_Text cardInfoText; // 可选，用于显示牌名等

    public Button cardButton;



    private PlayingCardData _cardData;

    private NetworkPlayingCard _networkCard; // 存储对应的NetworkPlayingCard，方便出牌时传递

    private HandDisplayUI _handDisplayManager; // 用于通知手牌管理器它被选中了



    public bool IsSelected { get; private set; }

    public Image selectionIndicator; // 可选: 一个子Image，用于显示选中状态



    public void Initialize(PlayingCardData cardData, NetworkPlayingCard networkCard, HandDisplayUI handDisplayManager)

    {

        _cardData = cardData;

        _networkCard = networkCard;

        _handDisplayManager = handDisplayManager;



        if (cardData != null)

        {

            if (cardArtImage != null) cardArtImage.sprite = cardData.cardImage;

            if (cardInfoText != null) cardInfoText.text = cardData.cardName; // 或 $"{cardData.rank} of {cardData.suit}"

        }

        IsSelected = false;

        UpdateSelectionVisual();



        cardButton.onClick.RemoveAllListeners(); // 清除旧监听器

        cardButton.onClick.AddListener(OnCardClicked);

    }



    public NetworkPlayingCard GetNetworkCard()

    {

        return _networkCard;

    }



    void OnCardClicked()

    {

        IsSelected = !IsSelected;

        UpdateSelectionVisual();

        _handDisplayManager.OnCardSelectionChanged(this, IsSelected);

        Debug.Log($"Card clicked: {_cardData?.cardName}, Selected: {IsSelected}");

    }



    void UpdateSelectionVisual()

    {

        if (selectionIndicator != null)

        {

            selectionIndicator.gameObject.SetActive(IsSelected);

        }

        // 或者改变背景色等

        // GetComponent<Image>().color = IsSelected ? Color.yellow : Color.white;

    }

    // public void OnBeginDrag(PointerEventData eventData)

    // {

    //     BeginDragEvent.Invoke(this);

    //     isDragging = true;

    // }

    // public void OnEndDrag(PointerEventData eventData)

    // {

    //     EndDragEvent.Invoke(this);

    //     isDragging = false;

    // }

}