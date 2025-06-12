using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;

/// <summary>
/// UI总控制器。
/// 负责更新游戏内HUD（分数、回合），也负责切换主UI面板（主菜单、游戏界面、结束界面）。
/// </summary>
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
        // 为“返回”按钮添加点击事件监听器
        if (BackButton != null)
        {
            BackButton.onClick.AddListener(OnBackButtonClicked);
        }
        // 游戏启动时，默认只显示主菜单
        ShowMainMenuPanel();
    }

    /// <summary>
    /// 当“返回”按钮被点击时调用。
    /// </summary>
    private void OnBackButtonClicked()
    {
        // 按钮的职责现在非常纯粹：只负责发起“停止”的指令。
        if (NetworkServer.active && NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopHost();
            // 同样，停止主机后的UI重置也应该放在 OnStopServer 中处理，以保持一致性。
        }
        else if (NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopClient();
        }
    }

    #region Panel Switching Methods

    /// <summary>
    /// 只显示主菜单，隐藏其他所有面板。
    /// </summary>
    public void ShowMainMenuPanel()
    {
        MainMenuPanel?.SetActive(true);
        CardPlayPanel?.SetActive(false);
        GameOverPanel?.SetActive(false);
    }

    /// <summary>
    /// 只显示游戏界面，隐藏其他所有面板。
    /// </summary>
    public void ShowCardPlayPanel()
    {
        MainMenuPanel?.SetActive(false);
        CardPlayPanel?.SetActive(true);
        GameOverPanel?.SetActive(false);
    }

    /// <summary>
    /// 显示游戏结束界面。
    /// </summary>
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
        if(TurnDisplayText != null)
        {
            TurnDisplayText.text = text;
        }
    }
    
    #endregion
}
