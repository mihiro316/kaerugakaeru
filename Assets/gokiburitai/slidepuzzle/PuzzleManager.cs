using UnityEngine;

[System.Serializable]
public class LevelData
{
    public int levelNumber = 1;
    public int gridSize = 3;       // 3なら3x3、4なら4x4
    public float timeLimit = 60f;  // 制限時間（秒）
    public int moveLimit = 30;     // スライド回数制限
}

public class PuzzleManager : MonoBehaviour
{
    [Header("参照設定")]
    [Tooltip("同じオブジェクトにあるSlidePuzzleBoard")]
    public SlidePuzzleBoard puzzleBoard;

    [Header("レベル設定")]
    public LevelData[] levels;

    [Header("現在の状態データ")]
    private int currentLevelIndex = 0;
    private float currentTime;
    private int remainingMoves;
    private bool isGameActive = false;

    // SlidePuzzleBoard 側からゲーム進行状態を確認するためのプロパティ
    public bool IsGameActive => isGameActive;

    private void Start()
    {
        if (puzzleBoard == null)
        {
            puzzleBoard = GetComponent<SlidePuzzleBoard>();
        }

        if (levels != null && levels.Length > 0)
        {
            StartLevel(0);
        }
        else if (puzzleBoard != null)
        {
            puzzleBoard.InitializeBoard(3);
        }
    }

    public void StartLevel(int levelIndex)
    {
        if (levels == null || levelIndex >= levels.Length) return;

        currentLevelIndex = levelIndex;
        LevelData config = levels[currentLevelIndex];

        currentTime = config.timeLimit;
        remainingMoves = config.moveLimit;
        isGameActive = true;

        GenerateBoard(config.gridSize);
    }

    private void Update()
    {
        if (!isGameActive) return;

        // 時間切れ判定
        if (currentTime > 0)
        {
            currentTime -= Time.deltaTime;
        }
        else
        {
            currentTime = 0;
            GameOver("時間切れ！");
        }
    }

    // ピース移動時にSlidePuzzleBoardから呼び出される
    public void OnPieceMoved()
    {
        if (!isGameActive) return;

        remainingMoves--;

        // クリア判定を先に行い、クリアしていなければ手数切れチェックを行う
        if (CheckIsCleared())
        {
            GameClear();
        }
        else if (remainingMoves <= 0)
        {
            GameOver("手数オーバー！");
        }
    }

    private void GenerateBoard(int size)
    {
        if (puzzleBoard != null)
        {
            puzzleBoard.InitializeBoard(size);
        }
    }

    private bool CheckIsCleared()
    {
        if (puzzleBoard != null)
        {
            return puzzleBoard.CheckIsCleared();
        }
        return false;
    }

    private void GameOver(string reason)
    {
        isGameActive = false;
        Debug.Log($"<color=red>ゲームオーバー: {reason}</color>");
    }

    private void GameClear()
    {
        isGameActive = false;
        Debug.Log("<color=green>パズルクリア！おめでとうございます！</color>");
    }
}