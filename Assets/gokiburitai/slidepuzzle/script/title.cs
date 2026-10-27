using UnityEngine;
using UnityEngine.SceneManagement; // シーン切り替え機能に必須

public class TitleManager : MonoBehaviour
{
    /// <summary>
    /// 「START」ボタンを押した時に呼び出すメソッド
    /// </summary>
    public void OnStartButtonClicked()
    {
        // SlidePuzzleScene（パズル画面）へ移動
        SceneManager.LoadScene("levelselect");
    }
}