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

    [Header("見本表示設定")]
    [Tooltip("画面横に配置した完成見本表示用のUI Image")]
    public Image referenceImage;

    [Header("アニメーション設定")]
    [Tooltip("1マスの移動にかかる時間（秒）")]
    public float moveDuration = 0.15f;

    [Header("UI設定")]
    [Tooltip("クリア時に表示するテキスト（TextMeshProUGUI）")]
    public TextMeshProUGUI clearMessageText;

    private int gridSize = 3;             // 現在の分割数（3x3）
    private int emptyIndex;               // 現在の空きマスのインデックス
    private int[] boardState;             // 各マスに配置されている元のピースID
    private Sprite[] originalSprites;     // 切り分けた元のSprite配列
    private Image[] tileImages;           // 生成した各タイルのImageコンポーネント
    private RectTransform[] tileRects;    // 各タイルのRectTransform
    private bool isCleared = false;       // クリアフラグ
    private bool isAnimating = false;     // アニメーション中フラグ

    public void InitializeBoard(int size)
    {
        gridSize = size;
        isCleared = false;
        isAnimating = false;

        if (clearMessageText != null)
        {
            clearMessageText.gameObject.SetActive(false);
        }

        // 見本ImageへのSprite設定
        if (referenceImage != null && sourceTexture != null)
        {
            Sprite sampleSprite = Sprite.Create(
                sourceTexture,
                new Rect(0, 0, sourceTexture.width, sourceTexture.height),
                new Vector2(0.5f, 0.5f)
            );
            referenceImage.sprite = sampleSprite;
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

        // -------------------------------------------------------------
        // ★ 3. 安全装置付き ピースの切り分けと生成 ★
        // -------------------------------------------------------------
        int sourceWidth = sourceTexture.width;
        int sourceHeight = sourceTexture.height;

        // 整数値で1マスあたりのピクセルサイズを計算
        int baseCropWidth = sourceWidth / gridSize;
        int baseCropHeight = sourceHeight / gridSize;

        for (int i = 0; i < totalTiles; i++)
        {
            boardState[i] = i;

            int row = i / gridSize;
            int col = i % gridSize;

            // X座標・Y座標の開始位置（整数）
            int texX = col * baseCropWidth;
            int texY = (gridSize - 1 - row) * baseCropHeight;

            // 安全装置: 切り出し幅・高さを計算し、画像端を超えないように制限 (Mathf.Min)
            int actualWidth = Mathf.Min(baseCropWidth, sourceWidth - texX);
            int actualHeight = Mathf.Min(baseCropHeight, sourceHeight - texY);

            // 正確に領域内に収まるRectを生成
            Rect spriteRect = new Rect(texX, texY, actualWidth, actualHeight);
            
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

            int tileIndex = i;
            Button btn = tileObj.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() => OnTileClicked(tileIndex));
            }
        }

        // 最右下を空きマスにする
        emptyIndex = totalTiles - 1;
        tileImages[emptyIndex].color = new Color(1f, 1f, 1f, 0f);

        // シャッフル実行
        ShuffleBoard(gridSize * 30);
    }

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

        MoveEmptyTileToBottomRight();
    }

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
                nextIndex = emptyIndex + gridSize;
            }
            else if (emptyCol < gridSize - 1)
            {
                nextIndex = emptyIndex + 1;
            }

            SwapTilesInstant(nextIndex, emptyIndex);
            emptyIndex = nextIndex;
        }
    }

    private void OnTileClicked(int clickedIndex)
    {
        if (isCleared || isAnimating) return;

        PuzzleManager manager = GetComponent<PuzzleManager>();
        if (manager != null && !manager.IsGameActive) return;

        if (clickedIndex == emptyIndex) return;

        int clickedRow = clickedIndex / gridSize;
        int clickedCol = clickedIndex % gridSize;
        int emptyRow = emptyIndex / gridSize;
        int emptyCol = emptyIndex % gridSize;

        List<int> moveSequence = new List<int>();

        if (clickedRow == emptyRow)
        {
            int step = (clickedCol < emptyCol) ? -1 : 1;
            for (int col = emptyCol; col != clickedCol; col += step)
            {
                moveSequence.Add(emptyRow * gridSize + (col + step));
            }
        }
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

    private IEnumerator AnimateSlideSequence(List<int> moveSequence)
    {
        isAnimating = true;

        for (int i = 0; i < moveSequence.Count; i++)
        {
            int targetTileIndex = moveSequence[i];
            
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

    private void SwapTilesInstant(int index1, int index2)
    {
        int tempState = boardState[index1];
        boardState[index1] = boardState[index2];
        boardState[index2] = tempState;

        Sprite tempSprite = tileImages[index1].sprite;
        tileImages[index1].sprite = tileImages[index2].sprite;
        tileImages[index2].sprite = tempSprite;

        Color tempColor = tileImages[index1].color;
        tileImages[index1].color = tileImages[index2].color;
        tileImages[index2].color = tempColor;
    }

    private List<int> GetAdjacentIndices(int centerIndex)
    {
        List<int> neighbors = new List<int>();
        int row = centerIndex / gridSize;
        int col = centerIndex % gridSize;

        if (row > 0) neighbors.Add(centerIndex - gridSize);
        if (row < gridSize - 1) neighbors.Add(centerIndex + gridSize);
        if (col > 0) neighbors.Add(centerIndex - 1);
        if (col < gridSize - 1) neighbors.Add(centerIndex + 1);

        return neighbors;
    }

    public bool CheckIsCleared()
    {
        for (int i = 0; i < boardState.Length; i++)
        {
            if (boardState[i] != i) return false;
        }
        return true;
    }

    private void OnClear()
    {
        isCleared = true;
        tileImages[emptyIndex].color = new Color(1f, 1f, 1f, 1f);

        if (clearMessageText != null)
        {
            clearMessageText.text = "CLEAR!!";
            clearMessageText.gameObject.SetActive(true);
        }

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