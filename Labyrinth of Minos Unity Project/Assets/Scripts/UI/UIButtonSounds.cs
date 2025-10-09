using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public class UIButtonSounds : MonoBehaviour, IPointerEnterHandler
{
    public AudioSource audioSource;
    public AudioClip hoverClip;
    public AudioClip clickClip;

    void Awake()
    {
        var btn = GetComponent<Button>();
        if (btn) btn.onClick.AddListener(() => { if (audioSource && clickClip) audioSource.PlayOneShot(clickClip); });
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (audioSource && hoverClip) audioSource.PlayOneShot(hoverClip);
    }
}
