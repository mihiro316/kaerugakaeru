using UnityEngine;
using UnityEngine.SceneManagement; // シーン切り替え用[cite: 1]

public class LevelSelectManager : MonoBehaviour
{
    /// <summary>
    /// レベルボタンが押された時に呼び出す（引数にレベル番号を指定）
    /// </summary>
    public void SelectLevel(int levelNumber)
    {
        // GameManager から PuzzleGameManager に変更
        PuzzleGameManager.SelectedLevel = levelNumber;

        // パズル画面（slidepuzzle）へシーン遷移[cite: 1]
        SceneManager.LoadScene("slidepuzzle");
    }

    /// <summary>
    /// タイトル画面へ戻るボタン用
    /// </summary>
    public void BackToTitle()
    {
        SceneManager.LoadScene("title");
    }
}