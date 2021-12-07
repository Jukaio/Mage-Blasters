using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AvatarGUI : MonoBehaviour
{
    [SerializeField] private Image deathImage;
    [SerializeField] private Image avatarImage;

    public void SetSprite(Sprite sprite)
    {
        avatarImage.sprite = sprite;
    }

    public void MarkAsDead()
    {
        deathImage.gameObject.SetActive(true);

    }

    public void MarkAsAlive()
    {
        deathImage.gameObject.SetActive(false);
    }
}
