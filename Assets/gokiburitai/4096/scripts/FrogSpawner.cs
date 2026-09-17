using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrogSpawner : MonoBehaviour
{
    [Header("生成する卵（frog[2]）のプレハブ")]
    public GameObject frog2Prefab;

    [Header("蓮の葉（マス目）のTransformを16個登録")]
    public Transform[] gridPositions; 

    [Header("判定用のサイズ（マスの大きさに合わせる）")]
    public Vector2 checkBoxSize = new Vector2(0.8f, 0.8f);

    // ボタンから呼び出す生成処理
    public void SpawnFrog()
    {
        if (frog2Prefab == null)
        {
            Debug.LogWarning("frog2Prefab がセットされていません！");
            return;
        }

        // 1. 重なっていない「空いている蓮の葉」を探す
        List<Transform> emptyGrids = new List<Transform>();

        foreach (Transform grid in gridPositions)
        {
            if (grid == null) continue;

            // 2Dでそのマスの上に何かオブジェクトがあるか判定
            Collider2D hit = Physics2D.OverlapBox(grid.position, checkBoxSize, 0f);

            // カエルや卵（FrogDrag2Dスクリプトを持っているもの）がないかチェック
            if (hit == null || hit.GetComponent<FrogDrag2D>() == null)
            {
                emptyGrids.Add(grid);
            }
        }

        // 2. 空いている場所がない場合
        if (emptyGrids.Count == 0)
        {
            Debug.LogWarning("すべての蓮の葉が埋まっています！");
            return;
        }

        // 3. 空いている蓮の葉からランダムに1つ選ぶ
        int randomIndex = Random.Range(0, emptyGrids.Count);
        Transform targetGrid = emptyGrids[randomIndex];

        // 4. その蓮の葉の位置に生成
        Vector3 spawnPos = new Vector3(targetGrid.position.x, targetGrid.position.y, 0f);
        Instantiate(frog2Prefab, spawnPos, Quaternion.identity);

        Debug.Log($"蓮の葉 [{targetGrid.name}] に新しい卵を生成しました！");
    }
}