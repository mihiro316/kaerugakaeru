using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Food : MonoBehaviour
{
    private int trueRate = 30;
    [SerializeField] private List<GameObject> items;
    [SerializeField] private List<Sprite> spriteT;
    [SerializeField] private List<Sprite> spriteF;
    private List<GameObject> goods　 = new List<GameObject>();
    private GameManager gameManagerSc;
    private Food foodSc;
    private int wrongCount = 0;
    private GameObject goodPrefab;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private int judge()
    {
        if (Random.Range(1,100) > trueRate)
        {
            return 1;
        }
        return 0;
    }

    public void SetUp(GameManager GMSc, GameObject obj)
    {
        goodPrefab = obj;
        foodSc = GetComponent<Food>();
        int count = 0;
        int wrong;
        gameManagerSc = GMSc;
        foreach(GameObject item in items)
        {
            wrong = judge();
            SpriteRenderer itemSr = item.GetComponent<SpriteRenderer>();
            switch (wrong)
            {
            case 0:
                itemSr.sprite = spriteT[count];break;
            case 1:
                itemSr.sprite = spriteF[count];
                Topping itemsCs = item.GetComponent<Topping>();
                itemsCs.SetUp(foodSc,goodPrefab);break;
            }
            wrongCount += wrong;
            count++;
        }
        if (wrongCount == 0)
        {
            int n = Random.Range(0,items.Count);
            SpriteRenderer itemSr = items[n].GetComponent<SpriteRenderer>();
            itemSr.sprite = spriteF[n];
            Topping itemsCs = items[n].GetComponent<Topping>();
            itemsCs.SetUp(foodSc,goodPrefab);
            wrongCount += 1;
        }
    } 

    public IEnumerator Discover(GameObject good)
    {
        wrongCount--;
        goods.Add(good);
        if(wrongCount <= 0)
        {
            yield return new WaitForSeconds(0.1f);
            foreach (GameObject obj in goods)
            {
                Destroy(obj);
            }
            goods.Clear();
            gameManagerSc.Change();
        }
    }
}
