using UnityEngine;

public class Topping : MonoBehaviour
{
    private Food foodSc; 
    private bool isTrue = true;
    private GameObject good;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetUp(Food FSc, GameObject obj)
    {
        good = obj;
        foodSc = FSc;
        isTrue = false;
    }

    private void OnMouseDown()
    {
        if(isTrue == false)
        {
            GameObject obj = Instantiate(good,transform.position - new Vector3(0,0,1),Quaternion.identity);
            StartCoroutine(foodSc.Discover(obj));
            isTrue = true;
        }
    }
}
