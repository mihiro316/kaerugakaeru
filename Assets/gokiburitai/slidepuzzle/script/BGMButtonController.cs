using UnityEngine;
using UnityEngine.UI;

public class BGMButtonController : MonoBehaviour
{
    [Header("表示切り替え用アイコン (任意)")]
    public Sprite soundOnSprite;  // 音が出る時の画像
    public Sprite soundOffSprite; // ミュート時の画像

    private Image buttonImage;

    private void Start()
    {
        buttonImage = GetComponent<Image>();
        UpdateButtonIcon();
    }

    /// <summary>
    /// ボタンの OnClick() イベントから呼び出すメソッド
    /// </summary>
    public void OnMuteButtonClicked()
    {
        if (BGMKeeper.Instance != null)
        {
            BGMKeeper.Instance.ToggleMute();
            UpdateButtonIcon();
        }
    }

    /// <summary>
    /// 現在のミュート状態に合わせてボタンの見た目を更新する
    /// </summary>
    private void UpdateButtonIcon()
    {
        if (BGMKeeper.Instance == null || buttonImage == null) return;

        if (BGMKeeper.Instance.IsMuted)
        {
            if (soundOffSprite != null) buttonImage.sprite = soundOffSprite;
        }
        else
        {
            if (soundOnSprite != null) buttonImage.sprite = soundOnSprite;
        }
    }
}