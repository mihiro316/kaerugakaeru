using UnityEngine;
using UnityEngine.SceneManagement; // シーン切り替え用

public class LevelSelectManager : MonoBehaviour
{
    /// <summary>
    /// レベルボタンが押された時に呼び出す（引数にレベル番号を指定）
    /// </summary>
    public void SelectLevel(int levelNumber)
    {
        // SelectedLevel に選択したレベル番号を保持[cite: 6]
        PuzzleGameManager.SelectedLevel = levelNumber;

        // パズル画面（slidepuzzle）へシーン遷移
        SceneManager.LoadScene("slidepuzzle");
    }

    /// <summary>
    /// 現在のレベルをもう一度最初からやり直す（リトライ/リスタート用）★追加
    /// </summary>
    public void Retry()
    {
        // PuzzleGameManager.SelectedLevel に直前のレベル番号が残っているため、
        // そのまま slidepuzzle シーンを再読み込みすることでリトライになります[cite: 1]
        SceneManager.LoadScene("slidepuzzle");
    }

    /// <summary>
    /// タイトル画面へ戻るボタン用
    /// </summary>
    public void BackToTitle()
    {
        SceneManager.LoadScene("title");
    }

    /// <summary>
    /// レベル選択画面へ戻るボタン用
    /// </summary>
    public void BackToLevelSelect()
    {
        SceneManager.LoadScene("levelselect"); 
    }
}