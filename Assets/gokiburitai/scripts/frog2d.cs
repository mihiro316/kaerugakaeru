using UnityEngine;
using TMPro;

public class FrogDrag2D : MonoBehaviour
{
    public int rank = 2; // カエルの数字 (2, 4, 8, 16...)
    public TextMeshPro textMesh;

    // inspectorから 2〜4096 のプレハブを登録するための配列
    [Header("各ランクのプレハブ設定")]
    public GameObject[] frogPrefabs;

    private Camera mainCamera;
    private bool isDragging = false;
    private Vector3 offset;

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
        // ドラッグ開始
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouseWorldPos = GetMouseWorldPos();
            Collider2D hitCollider = Physics2D.OverlapPoint(mouseWorldPos);

            if (hitCollider != null && hitCollider.gameObject == gameObject)
            {
                isDragging = true;
                offset = transform.position - mouseWorldPos;
                transform.localScale = new Vector3(1.2f, 1.2f, 1f);
            }
        }

        // ドラッグ中
        if (isDragging && Input.GetMouseButton(0))
        {
            Vector3 targetPosition = GetMouseWorldPos() + offset;
            transform.position = new Vector3(targetPosition.x, targetPosition.y, 0f);
        }

        // ドラッグ終了
        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            isDragging = false;
            transform.localScale = Vector3.one;
        }
    }

    // 🐸 合成（進化）処理 🐸
    private void OnTriggerStay2D(Collider2D other)
    {
        if (isDragging)
        {
            FrogDrag2D otherFrog = other.GetComponent<FrogDrag2D>();

            if (otherFrog != null && otherFrog != this && otherFrog.rank == this.rank)
            {
                int nextRank = this.rank * 2;
                Vector3 spawnPosition = transform.position; // 合成位置

                Debug.Log($"【合成成功!】 ランク: {nextRank}");

                // 次のランクのプレハブを探して生成する
                GameObject nextPrefab = GetPrefabForRank(nextRank);
                if (nextPrefab != null)
                {
                    Instantiate(nextPrefab, spawnPosition, Quaternion.identity);
                }
                else
                {
                    Debug.LogWarning($"ランク {nextRank} のプレハブが登録されていません！");
                }

                // 自分と相手の両方を消去する
                Destroy(other.gameObject);
                Destroy(gameObject);
            }
        }
    }

    // ランクに応じたプレハブを配列から検索して返す
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