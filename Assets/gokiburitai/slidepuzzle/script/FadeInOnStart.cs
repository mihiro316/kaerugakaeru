using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class FadeInOnStart : MonoBehaviour
{
    [Header("フェードインが始まるまでの待ち時間（秒）")]
    [SerializeField] private float startDelay = 0.5f;

    [Header("フェードインにかかる時間（秒）")]
    [SerializeField] private float fadeDuration = 1.0f;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        // 最初は完全透明（非表示）にしておく
        canvasGroup.alpha = 0f;
    }

    private void Start()
    {
        // シーン開始時に処理をスタート
        StartCoroutine(FadeInRoutine());
    }

    private IEnumerator FadeInRoutine()
    {
        // 指定した時間だけ待機
        if (startDelay > 0f)
        {
            yield return new WaitForSeconds(startDelay);
        }

        float timer = 0f;

        // 徐々に不透明にする
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            // アルファ値を 0 から 1 へ滑らかに変化
            canvasGroup.alpha = Mathf.Clamp01(timer / fadeDuration);
            yield return null; // 1フレーム待つ
        }

        canvasGroup.alpha = 1f; // 最後に確実に 1 にする
    }
}