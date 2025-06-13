using Mirror;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class PlayerNetObject : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnPlayerNumberChanged))]
    public int playerNumber = 0;

    [SyncVar(hook = nameof(OnScoreChanged))]
    public int score = 0;

    [SyncVar(hook = nameof(OnScoreTierChanged))]
    public int scoreTier = 0;

    [System.NonSerialized]
    public List<NetworkPlayingCard> Server_AuthoritativeHand;

    public readonly List<PlayingCardData> Client_LocalHand = new List<PlayingCardData>();
    public event System.Action Client_OnHandUpdated;

    #region Mirror Life cycle function
    public override void OnStartServer()
    {
        base.OnStartServer();
        Server_AuthoritativeHand = new List<NetworkPlayingCard>();
        score = 0;
        scoreTier = 0;
        Debug.Log($"[Server] PlayerNetObject {netId} OnStartServer: Data initialized.");
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Client_LocalHand.Clear();
        Debug.Log($"[Client] PlayerNetObject {netId} OnStartClient: Local hand cards cleared.");
        if (isLocalPlayer)
        {
            GameHUDController.Instance?.UpdateMyScoreDisplay(score);
        }
        else
        {
            GameHUDController.Instance?.UpdateOpponentScoreTierDisplay(scoreTier);
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        gameObject.name = $"LocalPlayer_{netId}";
        Debug.Log($"I am the local player! My netId is: {netId}. My player number is: {playerNumber}");
    }
    #endregion

    #region SyncVar Hooks
    void OnScoreChanged(int oldScore, int newScore)
    {
        if (isLocalPlayer)
        {
            Debug.Log($"[My Client] My score changed from {oldScore} to {newScore}");
            GameHUDController.Instance?.UpdateMyScoreDisplay(newScore);
        }
    }

    void OnScoreTierChanged(int oldTier, int newTier)
    {
        if (!isLocalPlayer)
        {
            Debug.Log($"[My Client] Opponent's progress bar changed from {oldTier} to {newTier}");
            GameHUDController.Instance?.UpdateOpponentScoreTierDisplay(newTier);
        }
    }

    void OnPlayerNumberChanged(int oldNum, int newNum)
    {
        if (isLocalPlayer)
        {
            gameObject.name = $"LocalPlayer_{netId} (Player {newNum})";
        }
        else
        {
            gameObject.name = $"OpponentPlayer_{netId} (Player {newNum})";
        }
    }
    #endregion

    #region Server commands and client RPCs

    /// <summary>
    /// [Added back] This method is called by GameManager on the server to deal initial hand cards.
    /// </summary>
    [Server]
    public void Server_DealInitialHand(List<NetworkPlayingCard> initialCards)
    {
        Server_AuthoritativeHand.Clear();
        Server_AuthoritativeHand.AddRange(initialCards);
        Debug.Log($"[Server] Player {netId}'s authoritative hand set, count: {initialCards.Count}.");

        // Call TargetRpc to send hand information only to the client belonging to this player
        Target_ReceiveInitialHand(initialCards);
    }

    [TargetRpc]
    public void Target_ReceiveInitialHand(List<NetworkPlayingCard> initialNetCards)
    {
        Debug.Log($"[Client {netId}] RPC: Received initial hand, count: {initialNetCards?.Count ?? 0}.");
        Client_LocalHand.Clear();
        if (initialNetCards != null)
        {
            foreach (var netCard in initialNetCards)
            {
                PlayingCardData cardData = CardLibrary.Instance?.GetCardData(netCard);
                if (cardData != null)
                {
                    Client_LocalHand.Add(cardData);
                }
            }
        }
        Client_OnHandUpdated?.Invoke();
    }

    [TargetRpc]
    public void Target_AddCardsToClientHand(List<NetworkPlayingCard> netCards)
    {
        if (netCards == null || netCards.Count == 0) return;
        Debug.Log($"[Client {netId}] RPC: Received replenishment cards, count: {netCards.Count}.");
        foreach (var netCard in netCards)
        {
            PlayingCardData cardData = CardLibrary.Instance?.GetCardData(netCard);
            if (cardData != null)
            {
                Client_LocalHand.Add(cardData);
            }
        }
        Client_OnHandUpdated?.Invoke();
    }

    [TargetRpc]
    public void Target_AcknowledgePlayAndRemoveCards(List<NetworkPlayingCard> playedNetCards)
    {
        if (playedNetCards == null || playedNetCards.Count == 0) return;
        Debug.Log($"[Client {netId}] RPC: Acknowledged play, removing {playedNetCards.Count} cards.");
        bool handChanged = false;
        foreach (var netCardToRemove in playedNetCards)
        {
            PlayingCardData cardToRemove = Client_LocalHand.FirstOrDefault(card => card.suit == netCardToRemove.suit && card.rank == netCardToRemove.rank);
            if (cardToRemove != null)
            {
                Client_LocalHand.Remove(cardToRemove);
                handChanged = true;
            }
        }
        if (handChanged)
        {
            Client_OnHandUpdated?.Invoke();
        }
    }
    #endregion

    #region Client card playing instructions
    [Command]
    public void CmdPlayCards(List<NetworkPlayingCard> playedNetCards)
    {
        if (GameManager.Instance == null || GameManager.Instance.currentPlayerNetId != this.netId)
        {
            Debug.LogWarning($"[Server] Player {netId} attempts to play cards in other turns");
            return;
        }

        if (playedNetCards == null || playedNetCards.Count == 0 || playedNetCards.Count > 5)
        {
            Debug.LogError($"[Server] Player {netId} sent an invalid number of cards to play: {playedNetCards?.Count ?? 0}.");
            return;
        }

        foreach (var playedCard in playedNetCards)
        {
            if (!Server_AuthoritativeHand.Contains(playedCard))
            {
                Debug.LogError($"[Server] Player {netId} tried to play cards that don't exist: {playedCard}. Could be cheating or being out of sync");
                Target_ReceiveInitialHand(new List<NetworkPlayingCard>(Server_AuthoritativeHand));
                return;
            }
        }

        GameManager.Instance.ProcessPlayerPlay(this, playedNetCards);

        // Remove from server hand
        foreach (var card in playedNetCards) { Server_AuthoritativeHand.Remove(card); }
        Target_AcknowledgePlayAndRemoveCards(playedNetCards);

        // Replenish hand logic
        ReplenishHand();
    }

    [Command]
    public void CmdDiscardCards(List<NetworkPlayingCard> discardedNetCards)
    {
        // Validate turn
        if (GameManager.Instance == null || GameManager.Instance.currentPlayerNetId != this.netId)
        {
            Debug.LogWarning($"[Server] Player {netId} attempts to discard cards not on their turn.");
            return;
        }

        if (discardedNetCards == null || discardedNetCards.Count == 0 || discardedNetCards.Count > 5)
        {
            Debug.LogError($"[Server] Player {netId} sent an invalid number of cards to discard: {discardedNetCards?.Count ?? 0}.");
            return;
        }

        // Validate card ownership
        if (discardedNetCards == null || discardedNetCards.Count == 0) return;
        foreach (var card in discardedNetCards)
        {
            if (!Server_AuthoritativeHand.Contains(card))
            {
                Debug.LogError($"[Server] Player {netId} tried to discard cards that don't exist: {card}.");
                Target_ReceiveInitialHand(new List<NetworkPlayingCard>(Server_AuthoritativeHand)); // Force hand sync
                return;
            }
        }

        // Delegate turn end to GameManager
        GameManager.Instance.ProcessPlayerDiscard(this);

        // Server-side hand update
        foreach (var card in discardedNetCards) { Server_AuthoritativeHand.Remove(card); }
        Target_AcknowledgePlayAndRemoveCards(discardedNetCards);

        // Replenish hand logic
        ReplenishHand();
    }

    [Server]
    private void ReplenishHand()
    {
        // --- Change point: Use GameManager's handSize ---
        int cardsToDraw = GameManager.Instance.handSize - Server_AuthoritativeHand.Count;
        if (cardsToDraw > 0 && DeckManager.Instance != null)
        {
            List<PlayingCardData> drawnCardsData = DeckManager.Instance.DrawMultipleCards(cardsToDraw);
            if (drawnCardsData.Count > 0)
            {
                // Convert PlayingCardData to NetworkPlayingCard
                List<NetworkPlayingCard> netCardsToDeal = drawnCardsData.Select(data => new NetworkPlayingCard(data.suit, data.rank)).ToList();
                // Update server authoritative hand
                Server_AuthoritativeHand.AddRange(netCardsToDeal);
                // RPC notify client to add new cards
                Target_AddCardsToClientHand(netCardsToDeal);
            }
        }
    }
    #endregion

    void OnDestroy()
    {
        Debug.Log($"[LIFECYCLE] PlayerNetObject with netId {netId} and playerNumber {playerNumber} is being DESTROYED.");
    }

}