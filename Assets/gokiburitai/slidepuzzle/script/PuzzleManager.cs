using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

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

public class PuzzleManager : MonoBehaviour
{
    [Header("レベル設定 (1～12)")]
    public List<LevelData> levelDataList = new List<LevelData>();

    [Header("画面パネルの参照")]
    [Tooltip("レベル選択画面のパネル")]
    public GameObject levelSelectPanel;
    [Tooltip("ゲームプレイ画面の親オブジェクトまたはCanvas")]
    public GameObject gamePlayPanel;

    [Header("UI表示コンポーネント（ゲーム中）")]
    public TextMeshProUGUI moveCountText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI levelTitleText;

    [Header("①・② リザルト画面UI（クリア/ゲームオーバー兼用）")]
    [Tooltip("結果を表示するパネル")]
    public GameObject resultPanel;
    [Tooltip("「GAME CLEAR!」や「GAME OVER...」を表示")]
    public TextMeshProUGUI resultTitleText;
    [Tooltip("クリア／終了時の時間を表示")]
    public TextMeshProUGUI resultTimeText;
    [Tooltip("クリア／終了時の手数を表示")]
    public TextMeshProUGUI resultMoveText;

    [Header("★ リザルト時に非表示にしたいオブジェクト")]
    [Tooltip("リザルト画面表示時に非表示にしたいオブジェクト（タイマーや残り手数UI、またはゲーム盤面など）")]
    public List<GameObject> hideObjectsOnResult = new List<GameObject>();

    [Header("参照")]
    public SlidePuzzleBoard puzzleBoard;

    private LevelData currentLevelData;
    private int remainingMoves;
    private int moveCount = 0;       // 実際に動かした手数
    private float currentTimer;
    private float elapsedTime = 0f;  // 実際にかかった時間
    private float activeTimeLimit;
    private bool isGameActive = false;

    // SlidePuzzleBoard側から状態を確認するためのプロパティ
    public bool IsGameActive => isGameActive;

    private void Start()
    {
        if (resultPanel != null) resultPanel.SetActive(false);

        // PuzzleGameManager で保存されているレベル番号を参照します
        if (PuzzleGameManager.SelectedLevel > 0)
        {
            StartLevel(PuzzleGameManager.SelectedLevel);
        }
        else
        {
            ShowLevelSelectScreen();
        }
    }

    private void Update()
    {
        if (!isGameActive) return;

        // 経過時間と残り時間の計算
        elapsedTime += Time.deltaTime;

        if (activeTimeLimit > 0f)
        {
            currentTimer -= Time.deltaTime;
            if (currentTimer <= 0f)
            {
                currentTimer = 0f;
                UpdateTimerUI();
                OnGameOver("時間切れ！");
                return;
            }
        }
        else
        {
            currentTimer += Time.deltaTime;
        }

        UpdateTimerUI();
    }

    public void ShowLevelSelectScreen()
    {
        isGameActive = false;

        if (levelSelectPanel != null) levelSelectPanel.SetActive(true);
        if (gamePlayPanel != null) gamePlayPanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);
    }

    public void StartLevel(int levelNumber)
    {
        LevelData data = levelDataList.Find(l => l.levelNumber == levelNumber);

        if (data.levelTexture == null)
        {
            Debug.LogError($"レベル {levelNumber} の画像(Texture2D)が設定されていません！ Inspectorを確認してください。");
            return;
        }

        currentLevelData = data;
        remainingMoves = data.maxMoves;
        moveCount = 0;
        elapsedTime = 0f;
        activeTimeLimit = data.timeLimit;
        currentTimer = (activeTimeLimit > 0f) ? activeTimeLimit : 0f;

        if (levelSelectPanel != null) levelSelectPanel.SetActive(false);
        if (gamePlayPanel != null) gamePlayPanel.SetActive(true);
        if (resultPanel != null) resultPanel.SetActive(false);

        // ★ リスタート時などに隠していたオブジェクトを再表示する
        foreach (GameObject obj in hideObjectsOnResult)
        {
            if (obj != null) obj.SetActive(true);
        }

        if (levelTitleText != null)
        {
            levelTitleText.text = $"Level {data.levelNumber}";
        }

        isGameActive = true;

        UpdateMoveCountUI();
        UpdateTimerUI();

        if (puzzleBoard != null)
        {
            puzzleBoard.sourceTexture = data.levelTexture;
            puzzleBoard.InitializeBoard(data.gridSize);
        }
    }

    /// <summary>
    /// ピース移動時にSlidePuzzleBoardから呼び出されます
    /// </summary>
    public void OnPieceMoved()
    {
        if (!isGameActive) return;

        moveCount++;
        remainingMoves--;
        UpdateMoveCountUI();

        if (remainingMoves <= 0 && activeTimeLimit > 0f)
        {
            OnGameOver("手数オーバー！");
        }
    }

    /// <summary>
    /// クリア時にSlidePuzzleBoardから呼び出されます
    /// </summary>
    public void OnClear()
    {
        if (!isGameActive) return;
        isGameActive = false;

        ShowResult("GAME CLEAR!!", Color.white);
    }

    /// <summary>
    /// ゲームオーバー処理
    /// </summary>
    public void OnGameOver(string reason)
    {
        if (!isGameActive) return;
        isGameActive = false;

        ShowResult($"GAME OVER", Color.red);
    }

    /// <summary>
    /// リザルト画面（パネル）の表示処理
    /// </summary>
    private void ShowResult(string title, Color titleColor)
    {
        // ★ 指定されたオブジェクトを非表示（SetActive(false)）にする
        foreach (GameObject obj in hideObjectsOnResult)
        {
            if (obj != null) obj.SetActive(false);
        }

        if (resultPanel == null) return;

        if (resultTitleText != null)
        {
            resultTitleText.text = title;
            resultTitleText.color = titleColor;
        }

        if (resultTimeText != null)
        {
            int minutes = Mathf.FloorToInt(elapsedTime / 60f);
            int seconds = Mathf.FloorToInt(elapsedTime % 60f);
            resultTimeText.text = $"タイム: {minutes:00}:{seconds:00}";
        }

        if (resultMoveText != null)
        {
            resultMoveText.text = $"手数: {moveCount} 回";
        }

        resultPanel.SetActive(true);
    }

    private void UpdateMoveCountUI()
    {
        if (moveCountText != null)
        {
            moveCountText.text = $" {remainingMoves}";
        }
    }

    private void UpdateTimerUI()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(currentTimer / 60f);
            int seconds = Mathf.FloorToInt(currentTimer % 60f);

            string label = (activeTimeLimit > 0f) ? "残り時間" : "時間";
            timerText.text = $" {minutes:00}:{seconds:00}";
        }
    }

}