using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq; // 用于 CmdPlayCards 的日志

public class HandDisplayUI : MonoBehaviour
{
    public GameObject cardUIPrefab; // 拖入您创建的 CardUIPrefab_Base
    public Transform handContainer; // 拖入 PlayerHandPanel GameObject
    public Button playSelectedButton; // 创建一个“出牌”按钮并链接到这里

    private PlayerNetObject _localPlayerNetObject;
    private List<CardUI> _displayedCardUIs = new List<CardUI>();
    private List<NetworkPlayingCard> _selectedCardsForPlay = new List<NetworkPlayingCard>();

    void Start()
    {
        // 检查Inspector引用是否设置
        if (playSelectedButton == null)
        {
            Debug.LogError("HandDisplayUI Start: PlaySelectedButton is not assigned in Inspector!");
        }
        else
        {
            playSelectedButton.onClick.AddListener(OnPlaySelectedCardsClicked);
            playSelectedButton.gameObject.SetActive(false); // 初始隐藏
        }

        if (cardUIPrefab == null) Debug.LogError("HandDisplayUI Start: cardUIPrefab is not assigned in Inspector!");
        if (handContainer == null) Debug.LogError("HandDisplayUI Start: handContainer is not assigned in Inspector!");
    }

    void Update()
    {
        // 使用Update来查找本地玩家，直到找到为止。这比Invoke更可靠。
        if (_localPlayerNetObject == null && NetworkClient.active && NetworkClient.localPlayer != null)
        {
            FindAndSubscribeToLocalPlayer();
        }
    }

    void FindAndSubscribeToLocalPlayer()
    {
        Debug.Log("[Client HandDisplayUI] Attempting to find local player...");
        if (NetworkClient.localPlayer != null)
        {
            Debug.Log($"[Client HandDisplayUI] Found NetworkClient.localPlayer with netId: {NetworkClient.localPlayer.netId}");
            _localPlayerNetObject = NetworkClient.localPlayer.GetComponent<PlayerNetObject>();
            if (_localPlayerNetObject != null)
            {
                Debug.Log($"[Client HandDisplayUI] Successfully got PlayerNetObject. Subscribing to OnHandUpdated. Initial Client_LocalHand count: {_localPlayerNetObject.Client_LocalHand.Count}");
                _localPlayerNetObject.Client_OnHandUpdated -= UpdateHandDisplay; // 先移除，防止重复订阅
                _localPlayerNetObject.Client_OnHandUpdated += UpdateHandDisplay;
                UpdateHandDisplay(); // 订阅后立即刷新一次手牌显示
            }
            else
            {
                Debug.LogError("[Client HandDisplayUI] PlayerNetObject script NOT FOUND on local player object (NetworkClient.localPlayer)!");
            }
        }
        else
        {
            Debug.LogWarning("[Client HandDisplayUI] NetworkClient.localPlayer is still NULL when FindAndSubscribeToLocalPlayer was called.");
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
        if (_localPlayerNetObject == null || handContainer == null || cardUIPrefab == null)
        {
            Debug.LogError("[Client HandDisplayUI UpdateHandDisplay] A critical reference is null, cannot update display.");
            return;
        }

        Debug.Log($"[Client HandDisplayUI UpdateHandDisplay] Called. Local player ({_localPlayerNetObject.netId}) hand count: {_localPlayerNetObject.Client_LocalHand.Count}");

        // 清理旧的卡牌UI
        foreach (Transform child in handContainer)
        {
            Destroy(child.gameObject);
        }
        _displayedCardUIs.Clear();
        _selectedCardsForPlay.Clear();
        UpdatePlayButtonState();

        if (_localPlayerNetObject.Client_LocalHand.Count == 0)
        {
            Debug.Log("[Client HandDisplayUI UpdateHandDisplay] No cards in local hand to display.");
        }

        // 为手牌中的每张牌创建新的UI实例
        foreach (PlayingCardData cardData in _localPlayerNetObject.Client_LocalHand)
        {
            if (cardData == null)
            {
                Debug.LogWarning("[Client HandDisplayUI UpdateHandDisplay] Encountered a null cardData in local hand list.");
                continue;
            }
            Debug.Log($"[Client HandDisplayUI UpdateHandDisplay] Instantiating card UI for: {cardData.cardName}");
            GameObject cardGO = Instantiate(cardUIPrefab, handContainer);
            CardUI cardUIComponent = cardGO.GetComponent<CardUI>();
            if (cardUIComponent != null)
            {
                NetworkPlayingCard netCard = new NetworkPlayingCard(cardData.suit, cardData.rank);
                cardUIComponent.Initialize(cardData, netCard, this);
                _displayedCardUIs.Add(cardUIComponent);
            }
            else
            {
                Debug.LogError("[Client HandDisplayUI UpdateHandDisplay] Instantiated card prefab is missing CardUI component!");
            }
        }
        Debug.Log($"[Client HandDisplayUI UpdateHandDisplay] Finished instantiating {_displayedCardUIs.Count} card UIs.");
    }

    public void OnCardSelectionChanged(CardUI cardUI, bool isSelected)
    {
        NetworkPlayingCard netCard = cardUI.GetNetworkCard();
        if (isSelected)
        {
            if (!_selectedCardsForPlay.Contains(netCard))
            {
                _selectedCardsForPlay.Add(netCard);
            }
        }
        else
        {
            _selectedCardsForPlay.Remove(netCard);
        }
        UpdatePlayButtonState();
        Debug.Log($"Selected cards: {_selectedCardsForPlay.Count}");
    }

    void UpdatePlayButtonState()
    {
        if (playSelectedButton != null)
        {
            playSelectedButton.gameObject.SetActive(_selectedCardsForPlay.Count > 0);
        }
    }

    void OnPlaySelectedCardsClicked()
    {
        if (_localPlayerNetObject != null && _selectedCardsForPlay.Count > 0)
        {
            if (GameManager.Instance != null && GameManager.Instance.currentPlayerNetId == _localPlayerNetObject.netId)
            {
                Debug.Log($"[UI] Attempting to play selected cards: {string.Join(", ", _selectedCardsForPlay)}");
                _localPlayerNetObject.CmdPlayCards(new List<NetworkPlayingCard>(_selectedCardsForPlay));
            }
            else
            {
                Debug.LogWarning("Not your turn to play!");
                // 可以在此显示一个UI提示给玩家
            }
        }
    }
    public void ClearHandDisplay()
    {
        Debug.Log("[Client HandDisplayUI] Clearing hand display.");
        foreach (Transform child in handContainer)
        {
            Destroy(child.gameObject);
        }
        _displayedCardUIs.Clear();
        _selectedCardsForPlay.Clear();
        if (playSelectedButton != null)
        {
            playSelectedButton.gameObject.SetActive(false);
        }
    }
}