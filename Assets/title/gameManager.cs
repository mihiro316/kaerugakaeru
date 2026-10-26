using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class gameManager : MonoBehaviour
{
    private int gameN;
    [SerializeField] int allGame;
    [SerializeField] private InputActionReference selectAction;
    [SerializeField] private InputActionReference decideAction;
    [SerializeField] private string[] sceneNames;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        selectAction.action.performed += selectGame;
        decideAction.action.performed += decideGame;
        selectAction.action.Enable();
        decideAction.action.Enable();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void selectGame(InputAction.CallbackContext context)
    {
        float input = context.ReadValue<float>();
        if (input > 0)
        {
            gameN = (gameN+1)%allGame;
        }
        else if (input < 0)
        {
            gameN = (gameN+allGame-1)%allGame;
        }
        Debug.Log(gameN);
    }

    void decideGame(InputAction.CallbackContext context)
    {
        SceneManager.LoadScene(sceneNames[gameN]);
    }
}
