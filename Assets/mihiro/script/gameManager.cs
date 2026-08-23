using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{

    [SerializeField]private List<Sprite> pictureOfFood;
    [SerializeField]private List<GameObject> foods;
    private GameObject nowFood;
    [SerializeField]private GameObject menue;
    private SpriteRenderer menueSr;
    [SerializeField]private Vector3 foodsPos;
    private GameManager gameManagerSc;
    [SerializeField]private GameObject good;
    [SerializeField]private AudioClip goodSe;
    [SerializeField]private AudioClip badSe;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        menueSr = menue.GetComponent<SpriteRenderer>();
        gameManagerSc = GetComponent<GameManager>();
        Change();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void ChangeMenue(int n)
    {
        menueSr.sprite = pictureOfFood[n];
    }

    private void ChangeFood(int n)
    {
        Destroy(nowFood);
        nowFood = Instantiate(foods[n],foodsPos,Quaternion.identity);
        Food foodSc = nowFood.GetComponent<Food>();
        foodSc.SetUp(gameManagerSc, good, goodSe,badSe);
    }

    public void Change()
    {
        int n = Random.Range(0,foods.Count);
        ChangeFood(n);
        ChangeMenue(n);
    }
}
