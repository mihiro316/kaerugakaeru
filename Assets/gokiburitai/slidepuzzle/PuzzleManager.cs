using UnityEngine;
using TMPro; // TextMeshProを使用（標準Textの場合は UnityEngine.UI.Text に変更）

public class PuzzleManager : MonoBehaviour
{
    [Header("UI表示コンポーネント")]
    [Tooltip("手数を表示するText (例: 手数: 0)")]
    public TextMeshProUGUI moveCountText;

    [Tooltip("経過時間を表示するText (例: 時間: 00:00)")]
    public TextMeshProUGUI timerText;

    [Header("参照")]
    public SlidePuzzleBoard puzzleBoard;

    private int moveCount = 0;
    private float timer = 0f;
    private bool isGameActive = false;

    public bool IsGameActive => isGameActive;

    private void Start()
    {
        // 起動時にパズルを初期化してスタート
        StartGame();
    }

    private void Update()
    {
        if (!isGameActive) return;

        // 経過時間の更新
        timer += Time.deltaTime;
        UpdateTimerUI();
    }

    /// <summary>
    /// ゲームの開始 / リスタート
    /// </summary>
    public void StartGame()
    {
        moveCount = 0;
        timer = 0f;
        isGameActive = true;

        UpdateMoveCountUI();
        UpdateTimerUI();

        if (puzzleBoard != null)
        {
            puzzleBoard.InitializeBoard(3); // 3x3で初期化
        }
    }

    /// <summary>
    /// ピースが動くたびに呼び出される処理
    /// </summary>
    public void OnPieceMoved()
    {
        if (!isGameActive) return;

        moveCount++;
        UpdateMoveCountUI();
    }

    /// <summary>
    /// クリア時に呼び出される処理
    /// </summary>
    public void OnClear()
    {
        isGameActive = false;
    }

    private void UpdateMoveCountUI()
    {
        if (moveCountText != null)
        {
            moveCountText.text = $"手数: {moveCount}";
        }
    }

    private void UpdateTimerUI()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(timer / 60f);
            int seconds = Mathf.FloorToInt(timer % 60f);
            timerText.text = $"時間: {minutes:00}:{seconds:00}";
        }
    }
}