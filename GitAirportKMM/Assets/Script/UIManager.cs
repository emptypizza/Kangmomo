using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Opening Settings")]
    public Image openingImage;
    public float openingDisplayTime = 3f;
    public float fadeOutDuration = 1.5f;

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

        if (openingImage != null)
        {
            StartCoroutine(OpeningSequenceCoroutine());
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

    private IEnumerator OpeningSequenceCoroutine()
    {
        openingImage.gameObject.SetActive(true);
        openingImage.color = new Color(1, 1, 1, 1); // Start fully opaque
        Time.timeScale = 0f; // Pause the game

        float timer = 0f;
        bool isClicked = false;

        // Wait for the duration to pass or for a user click
        while (timer < openingDisplayTime && !isClicked)
        {
            if (Input.GetMouseButtonDown(0))
            {
                isClicked = true;
            }
            timer += Time.unscaledDeltaTime; // Use unscaled time
            yield return null;
        }

        // Start the fade-out process
        float fadeTimer = 0f;
        while (fadeTimer < fadeOutDuration)
        {
            fadeTimer += Time.unscaledDeltaTime;
            float alpha = 1f - (fadeTimer / fadeOutDuration);
            openingImage.color = new Color(1, 1, 1, alpha);
            yield return null;
        }

        // Cleanup
        openingImage.gameObject.SetActive(false);
        Time.timeScale = 1f; // Resume the game
    }
}

