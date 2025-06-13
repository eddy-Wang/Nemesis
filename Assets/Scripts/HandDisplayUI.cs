using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;

public class HandDisplayUI : MonoBehaviour
{
    public GameObject cardPrefab; 
    public Transform handContainer;
    public Button playSelectedButton;
    public Button discardButton; 

    private PlayerNetObject _localPlayerNetObject;
    private List<CardController> _displayedCardControllers = new List<CardController>();
    private List<CardController> _selectedCardControllers = new List<CardController>();

    void Start()
    {
        if (playSelectedButton != null)
        {
            playSelectedButton.onClick.AddListener(OnPlaySelectedCardsClicked);
            playSelectedButton.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogError("HandDisplayUI Start: PlaySelectedButton is not assigned in Inspector!");
        }
        if (discardButton != null)
        {
            discardButton.onClick.AddListener(OnDiscardSelectedCardsClicked);
        }
        else
        {
            Debug.LogError("HandDisplayUI Start: DiscardButton is not assigned in Inspector!");
        }
        UpdateActionButtonsState();
        if (cardPrefab == null) Debug.LogError("HandDisplayUI Start: Card Prefab is not assigned in Inspector!");
        if (handContainer == null) Debug.LogError("HandDisplayUI Start: Hand Container is not assigned in Inspector!");
    }
    private void OnDiscardSelectedCardsClicked()
    {
        if (_localPlayerNetObject != null && _selectedCardControllers.Count > 0)
        {
            if (GameManager.Instance != null && GameManager.Instance.currentPlayerNetId == _localPlayerNetObject.netId)
            {
                List<NetworkPlayingCard> cardsToDiscard = _selectedCardControllers.Select(c => c.GetNetworkCard()).ToList();
                _localPlayerNetObject.CmdDiscardCards(cardsToDiscard);
                GameHUDController.Instance?.UpdateSelectedHandInfo("");
            }
            else
            {
                Debug.LogWarning("Not your turn to discard!");
            }
        }
    }

    void UpdateActionButtonsState()
    {
        bool hasSelection = _selectedCardControllers.Count > 0;
        playSelectedButton?.gameObject.SetActive(hasSelection);
        discardButton?.gameObject.SetActive(hasSelection);
    }
    void Update()
    {
        if (_localPlayerNetObject == null && NetworkClient.active && NetworkClient.localPlayer != null)
        {
            FindAndSubscribeToLocalPlayer();
        }
    }

    void FindAndSubscribeToLocalPlayer()
    {
        _localPlayerNetObject = NetworkClient.localPlayer.GetComponent<PlayerNetObject>();
        if (_localPlayerNetObject != null)
        {
            _localPlayerNetObject.Client_OnHandUpdated -= UpdateHandDisplay;
            _localPlayerNetObject.Client_OnHandUpdated += UpdateHandDisplay;
            UpdateHandDisplay();
        }
    }

    void OnDestroy()
    {
        if (_localPlayerNetObject != null)
        {
            _localPlayerNetObject.Client_OnHandUpdated -= UpdateHandDisplay;
        }
    }

    void UpdateHandDisplay()
    {
        if (_localPlayerNetObject == null || handContainer == null || cardPrefab == null) return;


        foreach (Transform child in handContainer)
        {
            Destroy(child.gameObject);
        }
        _displayedCardControllers.Clear();
        _selectedCardControllers.Clear();
        UpdateActionButtonsState();


        foreach (PlayingCardData cardData in _localPlayerNetObject.Client_LocalHand)
        {
            if (cardData == null) continue;


            GameObject cardGO = Instantiate(cardPrefab, handContainer);
            CardController cardController = cardGO.GetComponent<CardController>();
            
            if (cardController != null)
            {
                NetworkPlayingCard netCard = new NetworkPlayingCard(cardData.suit, cardData.rank);
                cardController.Initialize(cardData, netCard, this);
                _displayedCardControllers.Add(cardController);
            }
            else
            {
                Debug.LogError("[Client HandDisplayUI] Instantiated card prefab is missing CardController component!");
            }
        }
    }

    public void OnCardSelectionChanged(CardController cardController, bool isSelected)
    {
        if (isSelected)
        {   
            if (_selectedCardControllers.Count >= 5)
            {
                Debug.Log("Cannot select more than 5 cards.");
                cardController.Deselect(); 
                return;
            }
            if (!_selectedCardControllers.Contains(cardController))
            {
                _selectedCardControllers.Add(cardController);
            }
        }
        else
        {
            _selectedCardControllers.Remove(cardController);
        }
        UpdateActionButtonsState();
        CalculateAndDisplaySelectedHandInfo();
    }
    
    private void CalculateAndDisplaySelectedHandInfo()
    {
        if (_selectedCardControllers.Count == 0)
        {
            GameHUDController.Instance?.UpdateSelectedHandInfo("");
            return;
        }
        List<NetworkPlayingCard> selectedNetCards = _selectedCardControllers.Select(c => c.GetNetworkCard()).ToList();

        PokerHandType handType = PokerHandEvaluator.EvaluateHand(selectedNetCards);

        HandScoreData scoreData = GameManager.Instance.GetHandScoreData(handType);

        string handInfoText = $"{handType} (Chips: {scoreData.baseChips} x Mult: {scoreData.multiplier})";
        
        GameHUDController.Instance?.UpdateSelectedHandInfo(handInfoText);
    }

    void OnPlaySelectedCardsClicked()
    {
        if (_localPlayerNetObject != null && _selectedCardControllers.Count > 0)
        {
            if (GameManager.Instance != null && GameManager.Instance.currentPlayerNetId == _localPlayerNetObject.netId)
            {
                List<NetworkPlayingCard> cardsToPlay = _selectedCardControllers.Select(c => c.GetNetworkCard()).ToList();

                _localPlayerNetObject.CmdPlayCards(cardsToPlay);
                GameHUDController.Instance?.UpdateSelectedHandInfo("");
            }
            else
            {
                Debug.LogWarning("Not your turn to play!");
            }
        }
    }

    public void ClearHandDisplay()
    {
        foreach (Transform child in handContainer)
        {
            Destroy(child.gameObject);
        }
        _displayedCardControllers.Clear();
        _selectedCardControllers.Clear();
        if (playSelectedButton != null)
        {
            playSelectedButton.gameObject.SetActive(false);
        }
        UpdateActionButtonsState();
        GameHUDController.Instance?.UpdateSelectedHandInfo("");
    }
}
