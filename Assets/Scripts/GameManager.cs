using Mirror;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public enum GameState
{
    WaitingForPlayers,
    GameInProgress,
    GameOver
}

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    public int targetScore = 1000;
    public int handSize = 6;

    [Header("Game State")]
    [SyncVar(hook = nameof(OnGameStateChanged))]
    private GameState currentGameState = GameState.WaitingForPlayers;

    [SyncVar(hook = nameof(OnCurrentPlayerNetIdChanged))]
    public uint currentPlayerNetId;

    [SyncVar(hook = nameof(OnGameOverStateChanged))]
    public uint winnerNetId = 0;

    private List<PlayerNetObject> gamePlayers = new List<PlayerNetObject>();
    private int currentPlayerIndex = -1;
    private Dictionary<PokerHandType, HandScoreData> handScores;

    #region Unity & Mirror Lifecycle
    void Awake()
    {
        if (Instance == null) Instance = this; else Destroy(gameObject);
        InitializeHandScores();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        currentGameState = GameState.WaitingForPlayers;
        gamePlayers.Clear();
        currentPlayerNetId = 0;
        currentPlayerIndex = -1;
        winnerNetId = 0;
        // InitializeHandScores();
    }
    #endregion

    #region Server-Side Logic
    [Server]
    private void EndGame(PlayerNetObject winner)
    {
        if (currentGameState == GameState.GameOver) return;
        Debug.Log($"[Server] Game Over! Winner is Player {winner?.playerNumber} (netId: {winner?.netId}).");
        currentGameState = GameState.GameOver;
        currentPlayerNetId = 0;
        this.winnerNetId = winner != null ? winner.netId : 0;
    }

    [Server]
    void StartGame()
    {
        Debug.Log("[Server] Two players connected. Starting the game.");

        Debug.LogWarning("--- PRE-RESET SCORE CHECK ---");
        foreach (PlayerNetObject player in gamePlayers)
        {
            Debug.LogWarning($"Checking player netId: {player.netId}, playerNumber: {player.playerNumber}. Their current score is: {player.score}");
        }
        Debug.LogWarning("--- END OF PRE-RESET CHECK ---");
        
        foreach (PlayerNetObject player in gamePlayers)
        {
            player.score = 0;
            player.scoreTier = 0;
        }
        DeckManager.Instance.ResetAndShuffleDeck();

        foreach (PlayerNetObject player in gamePlayers)
        {
            List<PlayingCardData> drawnCardsData = DeckManager.Instance.DrawMultipleCards(handSize);
            List<NetworkPlayingCard> netCardsToDeal = new List<NetworkPlayingCard>();
            foreach (var cardData in drawnCardsData)
            {
                netCardsToDeal.Add(new NetworkPlayingCard(cardData.suit, cardData.rank));
            }
            player.Server_DealInitialHand(netCardsToDeal);
        }

        currentPlayerIndex = 0;
        PlayerNetObject firstPlayer = gamePlayers[currentPlayerIndex];
        if (firstPlayer != null)
        {
            currentPlayerNetId = firstPlayer.netId;
            Debug.Log($"[Server] Game Starting! First player is netId {currentPlayerNetId} (Player {firstPlayer.playerNumber}).");
        }
        
        currentGameState = GameState.GameInProgress;
    }
    
    [Server]
    public void ProcessPlayerPlay(PlayerNetObject playingPlayer, List<NetworkPlayingCard> playedCards)
    {
        if (currentGameState != GameState.GameInProgress || playingPlayer.netId != currentPlayerNetId) return;
        PokerHandType handType = PokerHandEvaluator.EvaluateHand(playedCards);
        int scoreGained = CalculateScore(handType, playedCards);
        playingPlayer.score += scoreGained;
        UpdatePlayerScoreTier(playingPlayer);
        Debug.Log($"[Server] Player {playingPlayer.netId} played a {handType}, gained {scoreGained} score. Total score: {playingPlayer.score}");
        CheckForWinner();
        if (currentGameState == GameState.GameInProgress) EndTurn();
    }

    [Server]
    public void ProcessPlayerDiscard(PlayerNetObject discardingPlayer)
    {
        if (currentGameState != GameState.GameInProgress || discardingPlayer.netId != currentPlayerNetId) return;

        Debug.Log($"[Server] Player {discardingPlayer.netId} discarded cards. Their turn now ends.");
        EndTurn();
    }

    [Server]
    private int CalculateScore(PokerHandType handType, List<NetworkPlayingCard> playedCards)
    {
        if (!handScores.TryGetValue(handType, out HandScoreData scoreData)) return 0;
        int totalChips = scoreData.baseChips;
        totalChips += GetScoringCardsChipValue(handType, playedCards);
        int extraChipsFromEffects = 0;
        totalChips += extraChipsFromEffects;
        int totalMultiplier = scoreData.multiplier;
        int extraMultiplierFromEffects = 0;
        totalMultiplier += extraMultiplierFromEffects;
        int xMultiplierFromEffects = 1;
        return totalChips * totalMultiplier * xMultiplierFromEffects;
    }
    private int GetCardChipValue(CardRank rank)
    {
        switch (rank)
        {
            case CardRank.Ace: return 11;
            case CardRank.King:
            case CardRank.Queen:
            case CardRank.Jack: return 10;
            default: return (int)rank;
        }
    }
    private int GetScoringCardsChipValue(PokerHandType handType, List<NetworkPlayingCard> playedCards)
    {
        int chips = 0;
        var rankGroups = playedCards.GroupBy(card => card.rank).OrderByDescending(g => g.Count()).ThenByDescending(g => g.Key).ToList();
        switch (handType)
        {
            case PokerHandType.HighCard:
                var highestCard = playedCards.OrderByDescending(card => card.rank).First();
                chips += GetCardChipValue(highestCard.rank);
                break;
            case PokerHandType.Pair:
                var pairGroup = rankGroups.First(g => g.Count() >= 2);
                foreach (var card in pairGroup) { chips += GetCardChipValue(card.rank); }
                break;
            case PokerHandType.TwoPair:
                var twoPairGroups = rankGroups.Where(g => g.Count() >= 2).Take(2);
                foreach (var group in twoPairGroups) { foreach (var card in group) { chips += GetCardChipValue(card.rank); } }
                break;
            case PokerHandType.ThreeOfAKind:
                var threeGroup = rankGroups.First(g => g.Count() >= 3);
                foreach (var card in threeGroup) { chips += GetCardChipValue(card.rank); }
                break;
            case PokerHandType.FourOfAKind:
                var fourGroup = rankGroups.First(g => g.Count() >= 4);
                foreach (var card in fourGroup) { chips += GetCardChipValue(card.rank); }
                break;
            case PokerHandType.Straight:
            case PokerHandType.Flush:
            case PokerHandType.FullHouse:
            case PokerHandType.StraightFlush:
                foreach (var card in playedCards) { chips += GetCardChipValue(card.rank); }
                break;
            default: break;
        }
        return chips;
    }
    [Server]
    private void UpdatePlayerScoreTier(PlayerNetObject player)
    {
        float scorePercentage = (float)player.score / targetScore;
        int newTier = 0;
        if (scorePercentage >= 0.75f) newTier = 3;
        else if (scorePercentage >= 0.5f) newTier = 2;
        else if (scorePercentage >= 0.25f) newTier = 1;

        player.scoreTier = newTier;
    }
    [Server]
    private void CheckForWinner()
    {
        PlayerNetObject winner = gamePlayers.FirstOrDefault(p => p.score >= targetScore);
        if (winner != null) { EndGame(winner); return; }
        if (DeckManager.Instance != null && DeckManager.Instance.IsDeckEmpty()) { PlayerNetObject highScorer = gamePlayers.OrderByDescending(p => p.score).FirstOrDefault(); EndGame(highScorer); }
    }
    [Server]
    public void EndTurn()
    {
        if (currentGameState != GameState.GameInProgress || gamePlayers.Count < 2) return;
        currentPlayerIndex = (currentPlayerIndex + 1) % gamePlayers.Count;
        PlayerNetObject nextPlayer = gamePlayers[currentPlayerIndex];
        if (nextPlayer != null) { currentPlayerNetId = nextPlayer.netId; Debug.Log($"[Server] Turn ended. Next player is netId {currentPlayerNetId} (Player {nextPlayer.playerNumber})."); }
        else { Debug.LogError($"[Server] EndTurn: Next player at index {currentPlayerIndex} is null!"); }
    }


    void InitializeHandScores()
    {
        handScores = new Dictionary<PokerHandType, HandScoreData>
        {
            { PokerHandType.HighCard, new HandScoreData { baseChips = 5, multiplier = 1 } },
            { PokerHandType.Pair, new HandScoreData { baseChips = 10, multiplier = 2 } },
            { PokerHandType.TwoPair, new HandScoreData { baseChips = 20, multiplier = 2 } },
            { PokerHandType.ThreeOfAKind, new HandScoreData { baseChips = 30, multiplier = 3 } },
            { PokerHandType.Straight, new HandScoreData { baseChips = 30, multiplier = 4 } },
            { PokerHandType.Flush, new HandScoreData { baseChips = 35, multiplier = 4 } },
            { PokerHandType.FullHouse, new HandScoreData { baseChips = 40, multiplier = 4 } },
            { PokerHandType.FourOfAKind, new HandScoreData { baseChips = 60, multiplier = 7 } },
            { PokerHandType.StraightFlush, new HandScoreData { baseChips = 100, multiplier = 8 } }
        };
    }

    public HandScoreData GetHandScoreData(PokerHandType handType)
    {
        if (handScores != null && handScores.TryGetValue(handType, out HandScoreData data))
        {
            return data;
        }
        return new HandScoreData { baseChips = 0, multiplier = 0 };
    }
    #endregion
    
    #region Player Management
    [Server]
    public void RegisterPlayer(PlayerNetObject playerNetObj, NetworkConnectionToClient conn)
    {
        if (playerNetObj == null) return;
        if (!gamePlayers.Any(p => p.netId == playerNetObj.netId))
        {
            gamePlayers.Add(playerNetObj);
            playerNetObj.playerNumber = gamePlayers.Count;
            Debug.Log($"[Server] Player object (netId: {playerNetObj.netId}, connId: {conn.connectionId}) registered as Player {playerNetObj.playerNumber}. Total players: {gamePlayers.Count}");
            if (gamePlayers.Count == 2 && currentGameState == GameState.WaitingForPlayers)
            {
                StartGame();
            }
        }
    }

    [Server]
    public void UnregisterPlayer(PlayerNetObject playerNetObj)
    {
        if (playerNetObj == null) return;
        PlayerNetObject foundPlayer = gamePlayers.FirstOrDefault(p => p.netId == playerNetObj.netId);
        if (foundPlayer != null)
        {
            gamePlayers.Remove(foundPlayer);
            Debug.Log($"[Server] Player object (netId: {playerNetObj.netId}) unregistered. Total players: {gamePlayers.Count}");
            if (NetworkServer.active && currentGameState == GameState.GameInProgress)
            {
                Debug.LogWarning("A player disconnected during the game. Ending game.");
                EndGame(gamePlayers.FirstOrDefault());
            }
        }
    }
    #endregion

    #region Client-Side Hooks
    void OnGameStateChanged(GameState oldState, GameState newState)
    {
        if (GameHUDController.Instance == null) return;

        Debug.Log($"[Client Hook] GameState changed from {oldState} to {newState}. Updating UI panels.");

        switch (newState)
        {
            case GameState.GameInProgress:
                GameHUDController.Instance.ShowCardPlayPanel();
                break;
            case GameState.WaitingForPlayers:
                GameHUDController.Instance.ShowMainMenuPanel();
                break;
            case GameState.GameOver:
                break;
        }
    }

    void OnGameOverStateChanged(uint oldWinnerId, uint newWinnerId)
    {
        if (newWinnerId == 0) return;
        if (GameHUDController.Instance == null) return;

        Debug.Log($"[Client Hook] GameOver state changed. Winner netId: {newWinnerId}");
        bool amITheWinner = NetworkClient.localPlayer.netId == newWinnerId;
        GameHUDController.Instance.ShowGameOverScreen(amITheWinner);
    }

    void OnCurrentPlayerNetIdChanged(uint oldPlayerNetId, uint newPlayerNetId)
    {
        Debug.Log($"[GameManager Hook] OnCurrentPlayerNetIdChanged fired on CLIENT. New netId: {newPlayerNetId}");
        if (GameHUDController.Instance == null)
        {
            Debug.LogError("[GameManager Hook] GameHUDController.Instance is NULL at this point! Cannot update turn display.");
            return;
        }
        if (newPlayerNetId == 0)
        {  
            GameHUDController.Instance.UpdateTurnDisplay("Game Over");
            return;
        }

        bool isMyTurn = NetworkClient.localPlayer.netId == newPlayerNetId;
        string turnText = isMyTurn ? "Your Turn" : "Opponent's Turn";

        GameHUDController.Instance.UpdateTurnDisplay(turnText);
    }
    #endregion
}
