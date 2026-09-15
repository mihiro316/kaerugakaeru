using UnityEngine;
using TMPro;

public class FrogDrag2D : MonoBehaviour
{
    public int rank = 2; // カエルの数字
    public TextMeshPro textMesh;

    [Header("各ランクのプレハブ設定")]
    public GameObject[] frogPrefabs;

    private Camera mainCamera;
    private bool isDragging = false;
    private Vector3 offset;
    private Vector3 startPosition; // ドラッグ開始時の位置（元あったハスの葉の位置）

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            GameObject camObj = GameObject.Find("2D camera");
            if (camObj != null)
            {
                mainCamera = camObj.GetComponent<Camera>();
            }
        }

        if (textMesh == null)
        {
            textMesh = GetComponentInChildren<TextMeshPro>();
        }

        UpdateUI();
    }

    void Update()
    {
        // 1. ドラッグ開始（クリックした瞬間）
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouseWorldPos = GetMouseWorldPos();
            Collider2D hitCollider = Physics2D.OverlapPoint(mouseWorldPos);

            if (hitCollider != null && hitCollider.gameObject == gameObject)
            {
                isDragging = true;
                startPosition = transform.position; // 持ち上げたときの位置を記憶
                offset = transform.position - mouseWorldPos;
                transform.localScale = new Vector3(1.2f, 1.2f, 1f); // つまんだ演出
            }
        }

        // 2. ドラッグ中
        if (isDragging && Input.GetMouseButton(0))
        {
            Vector3 targetPosition = GetMouseWorldPos() + offset;
            transform.position = new Vector3(targetPosition.x, targetPosition.y, 0f);
        }

        // 3. 左クリックを離した瞬間
        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            isDragging = false;
            transform.localScale = Vector3.one;

            // A. まず同じ数字のカエルとの合成を試みる
            bool merged = TryMerge();

            // B. 合成しなかった場合、離した場所にある空いているハスの葉への移動を試みる
            if (!merged)
            {
                bool moved = TryMoveToLeafUnderMouse();

                // 合成も移動もできなかった（外に落とした、または別のカエルがいる葉に落とした）場合は元の位置へ戻す
                if (!moved)
                {
                    transform.position = startPosition;
                }
            }
        }
    }

    // 🐸 合成処理 🐸
    private bool TryMerge()
    {
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, 0.4f);

        foreach (var hit in hitColliders)
        {
            if (hit.gameObject == gameObject) continue; // 自分自身は無視

            FrogDrag2D otherFrog = hit.GetComponent<FrogDrag2D>();

            // 同じランク（数字）のカエルと重ねて離された場合
            if (otherFrog != null && otherFrog.rank == this.rank)
            {
                int nextRank = this.rank * 2;
                Vector3 spawnPosition = otherFrog.transform.position; // 相手の位置に新世代を生成

                GameObject nextPrefab = GetPrefabForRank(nextRank);
                if (nextPrefab != null)
                {
                    Instantiate(nextPrefab, spawnPosition, Quaternion.identity);
                }

                // 両方を削除
                Destroy(otherFrog.gameObject);
                Destroy(gameObject);

                return true; // 合成成功
            }
        }

        return false; // 合成失敗
    }

    // 🍃 ピンポイントで直下の「ハスの葉」を検知して移動する処理 🍃
    private bool TryMoveToLeafUnderMouse()
    {
        // 離した位置に重なっているコライダーをすべて取得
        Collider2D[] hits = Physics2D.OverlapPointAll(transform.position);

        Transform targetLeaf = null;

        // 1. 下にある「ハスの葉（Tag: Leaf または 名前判定）」を探す
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Leaf") || hit.gameObject.name.Contains("蓮葉"))
            {
                targetLeaf = hit.transform;
                break;
            }
        }

        // 直下にハスの葉がない場合は失敗
        if (targetLeaf == null) return false;

        // 2. そのハスの葉の上に「自分以外のカエル」が既にいないかチェック
        Vector3 leafPos = new Vector3(targetLeaf.position.x, targetLeaf.position.y, 0f);
        Collider2D[] frogsOnLeaf = Physics2D.OverlapCircleAll(leafPos, 0.3f);

        foreach (var col in frogsOnLeaf)
        {
            if (col.gameObject != gameObject && col.GetComponent<FrogDrag2D>() != null)
            {
                return false; // すでに他のカエルが乗っているので移動不可
            }
        }

        // 3. 空いているハスの葉の中心にぴったり配置
        transform.position = leafPos;
        return true;
    }

    private GameObject GetPrefabForRank(int targetRank)
    {
        if (frogPrefabs == null) return null;

        foreach (GameObject prefab in frogPrefabs)
        {
            if (prefab != null)
            {
                FrogDrag2D script = prefab.GetComponent<FrogDrag2D>();
                if (script != null && script.rank == targetRank)
                {
                    return prefab;
                }
            }
        }
        return null;
    }

    public void UpdateUI()
    {
        if (textMesh != null)
        {
            textMesh.text = rank.ToString();
        }
    }

    private Vector3 GetMouseWorldPos()
    {
        if (mainCamera == null) return Vector3.zero;
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Mathf.Abs(mainCamera.transform.position.z);
        return mainCamera.ScreenToWorldPoint(mousePos);
    }
}