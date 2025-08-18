
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Item : MonoBehaviour

//여기서 deltaHp, deltaScore, preset 등은 필요하면 확장 가능합니다
{
    [SerializeField] private float lifetime = 4.5f; // 기본 수명, 인스펙터에서 수정 가능

    private void Start()
    {
        Destroy(gameObject, lifetime); // 일정 시간 후 자동 파괴
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            GameManager.Instance.CollectItem(); // 인벤토리 증가
            Destroy(gameObject);
            /*  GameManager.Instance.AddScore(1);
            Destroy(gameObject);*/
        }
    }
    // 먹었을 때 채워지는 HP양
    [SerializeField] int deltaHp = 5;

    // 먹었을 때 증가하는 점수
    [SerializeField] int deltaScore = 1;

    // 먹었을 때 변경되는 외형[SerializeField] CharacterPreset preset;

}

/*
 using UnityEngine;

public class Item : MonoBehaviour
{
    [SerializeField] LayerMask tongueLayer;
    [SerializeField] LayerMask frogBodyLayer;
    [SerializeField] LayerMask groundLayer;
    [SerializeField] LayerMask waterLayer;

    // 먹었을 때 채워지는 HP양
    [SerializeField] int deltaHp = 5;
    
    // 먹었을 때 증가하는 점수
    [SerializeField] int deltaScore = 1;
    
    // 먹었을 때 변경되는 외형
    [SerializeField] CharacterPreset preset;



void OnTriggerEnter2D(Collider2D col)
{
    if ((tongueLayer.value & (1 << col.gameObject.layer)) != 0)
    {
        if (Frog.Instance.CanCatch)
        {
            Frog.Instance.AttachItemToTongue(this);
        }
    }
    else if ((frogBodyLayer.value & (1 << col.gameObject.layer)) != 0)
    {
        if (Frog.Instance.IsAttachedToTongue(this) || Frog.Instance.CanCatch)
        {
            Frog.Instance.Score += deltaScore;
            Frog.Instance.Hp += deltaHp;
            if (deltaHp < 0)
            {
                Frog.Instance.PlayDamageClip();
            }
            else
            {
                Frog.Instance.PlayScoreClip();
            }

            Destroy(gameObject);

            // 외형 변경!!!
            if (preset != null)
            {
                Frog.Instance.Preset = preset;
            }
        }
    }
    else if ((groundLayer.value & (1 << col.gameObject.layer)) != 0)
    {
        // 아무것도 하지 않는다.
    }
    else if ((waterLayer.value & (1 << col.gameObject.layer)) != 0)
    {
        // 아무것도 하지 않는다.
    }
    else
    {
        Debug.LogError("Unknown collision layer");
    }
}
}*/