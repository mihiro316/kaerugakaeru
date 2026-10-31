using UnityEngine;
using System.Collections.Generic;
using TMPro;

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

    private int foodsCount = -1;     //出てきた料理数
    [SerializeField] int allFoods;  //出てくる料理数
    private float clearTime;     //クリア時間
    [SerializeField] List<float> scoreTime; //クリアタイムのスコアの閾値

    [SerializeField] private GameObject scoreGameObject;
    [SerializeField] private TextMeshProUGUI score;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        menueSr = menue.GetComponent<SpriteRenderer>();
        gameManagerSc = GetComponent<GameManager>();
        clearTime = Time.time;
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
        foodsCount ++;
        if (foodsCount < allFoods)  //料理がまだあるなら
        {
            int n = Random.Range(0,foods.Count);
            ChangeFood(n);
            ChangeMenue(n);
        }
        else
        {
            Destroy(nowFood);
            clearTime = Time.time - clearTime;
            Debug.Log(clearTime);
            scoreGameObject.SetActive(true);
            score.text = ((int)clearTime).ToString();
        }
    }
}
