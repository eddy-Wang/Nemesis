using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;

public class GameHUDController : MonoBehaviour
{
    public static GameHUDController Instance { get; private set; }

    [Header("Top-Level Panels")]
    public GameObject MainMenuPanel;
    public GameObject CardPlayPanel;
    public GameObject GameOverPanel;

    [Header("In-Game HUD References")]
    public TMP_Text myExactScoreText;
    public Slider opponentScoreSlider;
    public TMP_Text TurnDisplayText;
    public TMP_Text targetScoreText;
    public TMP_Text selectedHandInfoText;
    [Header("Game Over UI References")]
    public TMP_Text ResultText;
    public Button BackButton;

    [Header("Settings")]
    public int maxScoreTiers = 3;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (BackButton != null)
        {
            BackButton.onClick.AddListener(OnBackButtonClicked);
        }
        ShowMainMenuPanel();
    }


    private void OnBackButtonClicked()
    {
        if (NetworkServer.active && NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopHost();
        }
        else if (NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopClient();
        }
    }

    #region Panel Switching Methods

    public void ShowMainMenuPanel()
    {
        MainMenuPanel?.SetActive(true);
        CardPlayPanel?.SetActive(false);
        GameOverPanel?.SetActive(false);
    }

    public void ShowCardPlayPanel()
    {
        MainMenuPanel?.SetActive(false);
        CardPlayPanel?.SetActive(true);
        GameOverPanel?.SetActive(false);
        if (targetScoreText != null && GameManager.Instance != null)
        {
            targetScoreText.text = $"Aim Score: {GameManager.Instance.targetScore}";
        }
    }

    public void ShowGameOverScreen(bool didIWin)
    {
        MainMenuPanel?.SetActive(false);
        CardPlayPanel?.SetActive(false);
        GameOverPanel?.SetActive(true);

        if (ResultText != null)
        {
            ResultText.text = didIWin ? "You Win" : "Lose";
        }
    }

    #endregion

    #region In-Game HUD Update Methods

    public void UpdateMyScoreDisplay(int newScore)
    {
        if (myExactScoreText != null)
        {
            myExactScoreText.text = newScore.ToString();
        }
    }

    public void UpdateOpponentScoreTierDisplay(int newTier)
    {
        if (opponentScoreSlider != null)
        {
            opponentScoreSlider.maxValue = maxScoreTiers;
            opponentScoreSlider.value = newTier;
        }
    }

    public void UpdateTurnDisplay(string text)
    {
        if (TurnDisplayText != null)
        {
            TurnDisplayText.text = text;
        }
    }

    public void UpdateSelectedHandInfo(string info)
    {
        if (selectedHandInfoText != null)
        {
            selectedHandInfoText.gameObject.SetActive(!string.IsNullOrEmpty(info));
            selectedHandInfoText.text = info;
        }
    }
    
    #endregion
}
