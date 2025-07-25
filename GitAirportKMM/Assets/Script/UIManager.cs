using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
 
    public Text nLevel; // Modified to pcRotText
    public Text PlayerHP;

    public Text fDifficulty_lv;

    public Image plus_button;
    public Image minus_button;
    public Image multi_button;
    public Image chaos_button;


    public Image pc1win;
    


    public Button StartButton; // Modified to shotButton
    public Button clearButton;
    public Button gameoverButton;


    public Image clearImage; // Modified to clearImage
    public Image StartImage;
    public void UpdateHPUI(int hp)
    {
        if (PlayerHP != null)
            PlayerHP.text = "HP: " + hp.ToString();
    }


    void Start()
    {

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.GetComponent<Player>();
        }
        // player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();

        if (pc1win != null)
        {
            pc1win.transform.position = new Vector3(999, 480);
            pc1win.gameObject.SetActive(false);
        }
    
        if (clearImage != null)
        {
            clearImage.transform.position = new Vector3(0, 0);
            clearImage.gameObject.SetActive(false);
        }

       
    }
    public void GameStart()
    {
        Time.timeScale = 1f;
        StartImage.gameObject.SetActive(false);

    }

    public Player player;
    void Update()
    {
        UpdateHPUI(player.nHP);
    }





    public void GameClear()
    {
        Time.timeScale = 0f;
        clearImage.gameObject.SetActive(true);

    }




}

