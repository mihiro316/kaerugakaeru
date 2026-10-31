using UnityEngine;
using UnityEngine.SceneManagement;

public class Home : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void GoHome()
    {
        SceneManager.LoadScene("home");
    }

}
