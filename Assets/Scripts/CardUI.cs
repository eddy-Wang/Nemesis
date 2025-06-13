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

    public TMP_Text cardInfoText;

    public Button cardButton;



    private PlayingCardData _cardData;

    private NetworkPlayingCard _networkCard;

    private HandDisplayUI _handDisplayManager;



    public bool IsSelected { get; private set; }

    public Image selectionIndicator;



    public void Initialize(PlayingCardData cardData, NetworkPlayingCard networkCard, HandDisplayUI handDisplayManager)

    {

        _cardData = cardData;

        _networkCard = networkCard;

        _handDisplayManager = handDisplayManager;



        if (cardData != null)

        {

            if (cardArtImage != null) cardArtImage.sprite = cardData.cardImage;

            if (cardInfoText != null) cardInfoText.text = cardData.cardName;

        }

        IsSelected = false;

        UpdateSelectionVisual();



        cardButton.onClick.RemoveAllListeners();

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

//         _handDisplayManager.OnCardSelectionChanged(this, IsSelected);

        Debug.Log($"Card clicked: {_cardData?.cardName}, Selected: {IsSelected}");

    }



    void UpdateSelectionVisual()

    {

        if (selectionIndicator != null)

        {

            selectionIndicator.gameObject.SetActive(IsSelected);

        }



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