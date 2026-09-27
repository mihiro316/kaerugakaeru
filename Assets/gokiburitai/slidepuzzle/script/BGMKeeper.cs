using UnityEngine;

public class BGMKeeper : MonoBehaviour
{
    public static BGMKeeper Instance { get; private set; }

    private AudioSource audioSource;
    private bool isMuted = false;

    public bool IsMuted => isMuted; // 現在のミュート状態の確認用

    private void Awake()
    {
        // 他のシーンから戻ってきた際の重複防止
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // 重複したオブジェクトを破棄[cite: 7]
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // シーン遷移しても破棄しない

        audioSource = GetComponent<AudioSource>();
    }

    /// <summary>
    /// BGMのミュート/再生を切り替えるメソッド
    /// </summary>
    public void ToggleMute()
    {
        isMuted = !isMuted;

        if (audioSource != null)
        {
            audioSource.mute = isMuted; // ミュート状態を反映
        }
    }
}