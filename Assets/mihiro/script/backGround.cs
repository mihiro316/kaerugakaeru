using UnityEngine;

public class backGround : MonoBehaviour
{
    private AudioSource audioSource;
    [SerializeField]private AudioClip badSe;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnMouseDown()
    {
        audioSource.PlayOneShot(badSe);
    }
}
