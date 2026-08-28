using UnityEngine;

public class FrogSpawner : MonoBehaviour
{
    [Header("生成する卵（frog[2]）のプレハブ")]
    public GameObject frog2Prefab;

    [Header("生成する範囲（X軸・Y軸の最小/最大）")]
    public float minX = -3.0f;
    public float maxX = 3.0f;
    public float minY = -2.0f;
    public float maxY = 2.0f;

    // ボタンから呼び出す生成用メソッド
    public void SpawnFrog()
    {
        if (frog2Prefab == null)
        {
            Debug.LogWarning("frog2Prefab が設定されていません！ Inspectorでセットしてください。");
            return;
        }

        // 指定した範囲内のランダムな位置を計算
        float randomX = Random.Range(minX, maxX);
        float randomY = Random.Range(minY, maxY);
        Vector3 spawnPosition = new Vector3(randomX, randomY, 0f);

        // 卵（frog[2]）を生成
        Instantiate(frog2Prefab, spawnPosition, Quaternion.identity);
        Debug.Log($"新しい卵(2)を生成しました: 位置 {spawnPosition}");
    }
}