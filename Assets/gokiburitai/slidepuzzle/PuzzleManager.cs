using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PuzzleManager : MonoBehaviour
{
    // レベル単位の設定構造体
    [System.Serializable]
    public struct LevelData
    {
        [Tooltip("レベル番号（1～12）")]
        public int levelNumber;
        [Tooltip("このレベルで使用するパズル画像")]
        public Texture2D levelTexture;
        [Tooltip("分割数（例: 3なら3x3, 4なら4x4）")]
        public int gridSize;
        [Tooltip("制限手数")]
        public int maxMoves;
        [Tooltip("制限時間（秒数。0で制限時間なし）")]
        public float timeLimit;
    }

    [Header("レベル設定 (1～12)")]
    public List<LevelData> levelDataList = new List<LevelData>();

    [Header("画面パネルの参照")]
    [Tooltip("レベル選択画面のパネル")]
    public GameObject levelSelectPanel;
    [Tooltip("ゲームプレイ画面の親オブジェクトまたはCanvas")]
    public GameObject gamePlayPanel;

    [Header("UI表示コンポーネント")]
    public TextMeshProUGUI moveCountText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI levelTitleText; // 「Level 1」などの表示用（任意）

    [Header("参照")]
    public SlidePuzzleBoard puzzleBoard;

    private LevelData currentLevelData;
    private int remainingMoves;
    private float currentTimer;
    private float activeTimeLimit;
    private bool isGameActive = false;

    public bool IsGameActive => isGameActive;

    private void Start()
    {
        // 起動時はレベル選択画面を表示
        ShowLevelSelectScreen();
    }

    private void Update()
    {
        if (!isGameActive) return;

        // 制限時間の更新
        if (activeTimeLimit > 0f)
        {
            currentTimer -= Time.deltaTime;
            if (currentTimer <= 0f)
            {
                currentTimer = 0f;
                UpdateTimerUI();
                OnGameOver("TIME OVER");
                return;
            }
        }
        else
        {
            currentTimer += Time.deltaTime;
        }

        UpdateTimerUI();
    }

    /// <summary>
    /// レベル選択画面を表示する
    /// </summary>
    public void ShowLevelSelectScreen()
    {
        isGameActive = false;

        if (levelSelectPanel != null) levelSelectPanel.SetActive(true);
        if (gamePlayPanel != null) gamePlayPanel.SetActive(false);
    }

    /// <summary>
    /// レベル番号を指定してゲームを開始（ボタンのOnClickから呼び出す）
    /// </summary>
    public void StartLevel(int levelNumber)
    {
        // 該当するレベルのデータを検索
        LevelData data = levelDataList.Find(l => l.levelNumber == levelNumber);

        // データが見つからない場合のフォールバック（画像が未設定の場合は警告）
        if (data.levelTexture == null)
        {
            Debug.LogError($"レベル {levelNumber} の画像(Texture2D)が設定されていません！ Inspectorを確認してください。");
            return;
        }

        currentLevelData = data;
        remainingMoves = data.maxMoves;
        activeTimeLimit = data.timeLimit;
        currentTimer = (activeTimeLimit > 0f) ? activeTimeLimit : 0f;

        // 画面の切り替え
        if (levelSelectPanel != null) levelSelectPanel.SetActive(false);
        if (gamePlayPanel != null) gamePlayPanel.SetActive(true);

        if (levelTitleText != null)
        {
            levelTitleText.text = $"Level {data.levelNumber}";
        }

        isGameActive = true;

        UpdateMoveCountUI();
        UpdateTimerUI();

        // 指定画像とサイズでパズルを初期化
        if (puzzleBoard != null)
        {
            puzzleBoard.sourceTexture = data.levelTexture;
            puzzleBoard.InitializeBoard(data.gridSize);
        }
    }

    /// <summary>
    /// 現在選択中のレベルをリスタート（リトライボタン用）
    /// </summary>
    public void RestartCurrentLevel()
    {
        StartLevel(currentLevelData.levelNumber);
    }

    /// <summary>
    /// ピース移動時のカウントダウン処理
    /// </summary>
    public void OnPieceMoved()
    {
        if (!isGameActive) return;

        remainingMoves--;
        UpdateMoveCountUI();

        if (remainingMoves <= 0)
        {
            OnGameOver("GAME OVER");
        }
    }

    /// <summary>
    /// クリア時処理
    /// </summary>
    public void OnClear()
    {
        isGameActive = false;
    }

    /// <summary>
    /// ゲームオーバー処理
    /// </summary>
    private void OnGameOver(string message)
    {
        isGameActive = false;

        if (puzzleBoard != null && puzzleBoard.clearMessageText != null)
        {
            puzzleBoard.clearMessageText.text = message;
            puzzleBoard.clearMessageText.gameObject.SetActive(true);
        }

        Debug.Log($"ゲームオーバー: {message}");
    }

    private void UpdateMoveCountUI()
    {
        if (moveCountText != null)
        {
            moveCountText.text = $"残り手数: {remainingMoves}";
        }
    }

    private void UpdateTimerUI()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(currentTimer / 60f);
            int seconds = Mathf.FloorToInt(currentTimer % 60f);

            string label = (activeTimeLimit > 0f) ? "残り時間" : "時間";
            timerText.text = $"{label}: {minutes:00}:{seconds:00}";
        }
    }
}