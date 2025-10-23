using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public class UIButtonSounds : MonoBehaviour, IPointerEnterHandler
{
    public AudioClip hoverClip;
    public AudioClip clickClip;

    Button btn;

    void Awake()
    {
        btn = GetComponent<Button>();
        if (btn != null)
            btn.onClick.AddListener(() =>
            {
                if (clickClip) AudioManager.Instance?.PlayUI(clickClip);
            });
    }

    public void OnPointerEnter(PointerEventData _)
    {
        if (hoverClip) AudioManager.Instance?.PlayUI(hoverClip);
    }
}
