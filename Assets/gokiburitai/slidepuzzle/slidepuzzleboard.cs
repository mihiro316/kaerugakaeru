using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SlidePuzzleBoard : MonoBehaviour
{
    [Header("パズル設定")]
    [Tooltip("分割して使用する元画像")]
    public Texture2D sourceTexture;

    [Tooltip("パズルを表示する親オブジェクト（GridLayoutGroupを適用）")]
    public RectTransform boardParent;

    [Tooltip("ピースとして使用するUI Buttonプレハブ（ImageとButtonコンポーネントが必須）")]
    public GameObject tilePrefab;

    [Header("アニメーション設定")]
    [Tooltip("1マスの移動にかかる時間（秒）")]
    public float moveDuration = 0.15f;

    [Header("UI設定")]
    [Tooltip("クリア時に表示するテキスト（TextMeshProUGUI）")]
    public TextMeshProUGUI clearMessageText;

    private int gridSize = 3;             // 現在の分割数（3x3）
    private int emptyIndex;               // 現在の空きマスのインデックス（0 ～ totalTiles-1）
    private int[] boardState;             // 各マスに配置されている元のピースID（クリア判定用）
    private Sprite[] originalSprites;     // 切り分けた元のSprite配列
    private Image[] tileImages;           // 生成した各タイルのImageコンポーネント
    private RectTransform[] tileRects;    // 各タイルのRectTransform
    private bool isCleared = false;       // クリアフラグ
    private bool isAnimating = false;     // アニメーション中フラグ（連打防止）

    public void InitializeBoard(int size)
    {
        gridSize = size;
        isCleared = false;
        isAnimating = false;

        if (clearMessageText != null)
        {
            clearMessageText.gameObject.SetActive(false);
        }

        ClearBoard();

        // 1. GridLayoutGroup の自動調整
        GridLayoutGroup gridGroup = boardParent.GetComponent<GridLayoutGroup>();
        if (gridGroup == null)
        {
            gridGroup = boardParent.gameObject.AddComponent<GridLayoutGroup>();
        }

        float boardWidth = boardParent.rect.width;
        float boardHeight = boardParent.rect.height;
        float tileWidth = boardWidth / gridSize;
        float tileHeight = boardHeight / gridSize;

        gridGroup.cellSize = new Vector2(tileWidth, tileHeight);
        gridGroup.spacing = Vector2.zero;
        gridGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridGroup.constraintCount = gridSize;

        // 2. 配列と変数の準備
        int totalTiles = gridSize * gridSize;
        boardState = new int[totalTiles];
        originalSprites = new Sprite[totalTiles];
        tileImages = new Image[totalTiles];
        tileRects = new RectTransform[totalTiles];

        int sourceWidth = sourceTexture.width;
        int sourceHeight = sourceTexture.height;
        float cropWidth = (float)sourceWidth / gridSize;
        float cropHeight = (float)sourceHeight / gridSize;

        // 3. ピースの切り分けと生成
        for (int i = 0; i < totalTiles; i++)
        {
            boardState[i] = i;

            int row = i / gridSize;
            int col = i % gridSize;

            int texX = Mathf.RoundToInt(col * cropWidth);
            int texY = Mathf.RoundToInt((gridSize - 1 - row) * cropHeight);

            Rect spriteRect = new Rect(texX, texY, cropWidth, cropHeight);
            Sprite tileSprite = Sprite.Create(
                sourceTexture,
                spriteRect,
                new Vector2(0.5f, 0.5f)
            );

            originalSprites[i] = tileSprite;

            GameObject tileObj = Instantiate(tilePrefab, boardParent);
            tileObj.name = $"Tile_{i}";
            
            tileRects[i] = tileObj.GetComponent<RectTransform>();

            Image img = tileObj.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = tileSprite;
                tileImages[i] = img;
            }

            // ボタンクリックイベント（インデックス固定）
            int tileIndex = i;
            Button btn = tileObj.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() => OnTileClicked(tileIndex));
            }
        }

        // 初期状態で「最も右下（最後のインデックス）」を空きマスにする
        emptyIndex = totalTiles - 1;
        tileImages[emptyIndex].color = new Color(1f, 1f, 1f, 0f); // 右下を透明化

        // 4. シャッフル実行（アニメーションなしで高速処理）
        ShuffleBoard(gridSize * 30);
    }

    /// <summary>
    /// シャッフル処理（初期化用：アニメーションなし）
    /// </summary>
    private void ShuffleBoard(int shuffleSteps)
    {
        int previousIndex = -1;

        for (int i = 0; i < shuffleSteps; i++)
        {
            List<int> validNeighbors = GetAdjacentIndices(emptyIndex);

            if (previousIndex != -1 && validNeighbors.Count > 1)
            {
                validNeighbors.Remove(previousIndex);
            }

            int randomIndex = validNeighbors[Random.Range(0, validNeighbors.Count)];
            SwapTilesInstant(randomIndex, emptyIndex);

            previousIndex = emptyIndex;
            emptyIndex = randomIndex;
        }

        // シャッフル後、空きマスを最右下（3行目3列目）まで寄せる
        MoveEmptyTileToBottomRight();
    }

    /// <summary>
    /// 空きマスを確実に右下（最後のインデックス）までスライド移動させる
    /// </summary>
    private void MoveEmptyTileToBottomRight()
    {
        int totalTiles = gridSize * gridSize;
        int targetIndex = totalTiles - 1;

        while (emptyIndex != targetIndex)
        {
            int emptyRow = emptyIndex / gridSize;
            int emptyCol = emptyIndex % gridSize;

            int nextIndex = emptyIndex;

            if (emptyRow < gridSize - 1)
            {
                nextIndex = emptyIndex + gridSize; // 下へ
            }
            else if (emptyCol < gridSize - 1)
            {
                nextIndex = emptyIndex + 1; // 右へ
            }

            SwapTilesInstant(nextIndex, emptyIndex);
            emptyIndex = nextIndex;
        }
    }

    /// <summary>
    /// タイルクリック処理
    /// </summary>
    private void OnTileClicked(int clickedIndex)
    {
        if (isCleared || isAnimating) return; // アニメーション中・クリア後は操作不可

        PuzzleManager manager = GetComponent<PuzzleManager>();
        if (manager != null && !manager.IsGameActive) return;

        if (clickedIndex == emptyIndex) return;

        int clickedRow = clickedIndex / gridSize;
        int clickedCol = clickedIndex % gridSize;
        int emptyRow = emptyIndex / gridSize;
        int emptyCol = emptyIndex % gridSize;

        List<int> moveSequence = new List<int>();

        // 同じ行（横方向）
        if (clickedRow == emptyRow)
        {
            int step = (clickedCol < emptyCol) ? -1 : 1;
            for (int col = emptyCol; col != clickedCol; col += step)
            {
                moveSequence.Add(emptyRow * gridSize + (col + step));
            }
        }
        // 同じ列（縦方向）
        else if (clickedCol == emptyCol)
        {
            int step = (clickedRow < emptyRow) ? -1 : 1;
            for (int row = emptyRow; row != clickedRow; row += step)
            {
                moveSequence.Add((row + step) * gridSize + emptyCol);
            }
        }

        if (moveSequence.Count > 0)
        {
            StartCoroutine(AnimateSlideSequence(moveSequence));
        }
    }

    /// <summary>
    /// 複数マスを連続で一括スライドさせるアニメーションコルーチン
    /// </summary>
    private IEnumerator AnimateSlideSequence(List<int> moveSequence)
    {
        isAnimating = true;

        for (int i = 0; i < moveSequence.Count; i++)
        {
            int targetTileIndex = moveSequence[i];
            
            // 現在の空きマスの座標を取得
            Vector2 targetPos = tileRects[emptyIndex].anchoredPosition;
            Vector2 startPos = tileRects[targetTileIndex].anchoredPosition;

            float elapsedTime = 0f;
            while (elapsedTime < moveDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsedTime / moveDuration);
                tileRects[targetTileIndex].anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
                yield return null;
            }

            // 移動完了後、位置を元に戻してSpriteとColorのデータを差し替え
            tileRects[targetTileIndex].anchoredPosition = startPos;
            SwapTilesInstant(targetTileIndex, emptyIndex);
            emptyIndex = targetTileIndex;
        }

        OnPieceMoved();

        if (CheckIsCleared())
        {
            OnClear();
        }

        isAnimating = false;
    }

    /// <summary>
    /// 2つのマスの「表示情報（Sprite・アルファ値）」と「内部データ」を即座に入れ替える
    /// </summary>
    private void SwapTilesInstant(int index1, int index2)
    {
        // 1. 内部IDの入れ替え
        int tempState = boardState[index1];
        boardState[index1] = boardState[index2];
        boardState[index2] = tempState;

        // 2. Sprite（画像）の入れ替え
        Sprite tempSprite = tileImages[index1].sprite;
        tileImages[index1].sprite = tileImages[index2].sprite;
        tileImages[index2].sprite = tempSprite;

        // 3. 透明度（Color）の入れ替え
        Color tempColor = tileImages[index1].color;
        tileImages[index1].color = tileImages[index2].color;
        tileImages[index2].color = tempColor;
    }

    private List<int> GetAdjacentIndices(int centerIndex)
    {
        List<int> neighbors = new List<int>();
        int row = centerIndex / gridSize;
        int col = centerIndex % gridSize;

        if (row > 0) neighbors.Add(centerIndex - gridSize);             // 上
        if (row < gridSize - 1) neighbors.Add(centerIndex + gridSize); // 下
        if (col > 0) neighbors.Add(centerIndex - 1);                   // 左
        if (col < gridSize - 1) neighbors.Add(centerIndex + 1);       // 右

        return neighbors;
    }

    /// <summary>
    /// クリア判定
    /// </summary>
    public bool CheckIsCleared()
    {
        for (int i = 0; i < boardState.Length; i++)
        {
            if (boardState[i] != i) return false;
        }
        return true;
    }
    /// <summary>
    /// クリア時処理
    /// </summary>
    private void OnClear()
    {
        isCleared = true;

        // 透明にしていた空きマスを不透明に戻す
        tileImages[emptyIndex].color = new Color(1f, 1f, 1f, 1f);

        // クリアメッセージの表示
        if (clearMessageText != null)
        {
            clearMessageText.text = "CLEAR!!";
            clearMessageText.gameObject.SetActive(true);
        }

        //★ PuzzleManagerにクリアを通知してタイマーをストップさせる
        PuzzleManager manager = GetComponent<PuzzleManager>();
        if (manager != null)
        {
            manager.OnClear();
        }

        Debug.Log("クリアしました！");
    }

    private void OnPieceMoved()
    {
        PuzzleManager manager = GetComponent<PuzzleManager>();
        if (manager != null)
        {
            manager.OnPieceMoved();
        }
    }

    private void ClearBoard()
    {
        foreach (Transform child in boardParent)
        {
            Destroy(child.gameObject);
        }
    }
}