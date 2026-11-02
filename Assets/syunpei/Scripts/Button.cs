using UnityEngine;
using UnityEngine.SceneManagement;

public class GoHome : MonoBehaviour
{
    [SerializeField] private GameObject hyou;
    private bool isHyou = false;

    public void ChangeScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void Hyou()
    {
        isHyou = !isHyou;
        hyou.SetActive(isHyou);
    }
}