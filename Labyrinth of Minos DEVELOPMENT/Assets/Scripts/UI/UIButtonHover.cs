using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler, IPointerDownHandler, IPointerUpHandler
{
    [Range(0.9f, 1.2f)] public float hoverScale = 1.03f;
    [Range(0.8f, 1.2f)] public float pressScale = 0.98f;
    public float speed = 12f;
    Vector3 target = Vector3.one;

    void Update() => transform.localScale = Vector3.Lerp(transform.localScale, target, Time.unscaledDeltaTime * speed);
    public void OnPointerEnter(PointerEventData e) => target = Vector3.one * hoverScale;
    public void OnPointerExit(PointerEventData e) => target = Vector3.one;
    public void OnSelect(BaseEventData e) => target = Vector3.one * hoverScale;   // keyboard/controller focus
    public void OnDeselect(BaseEventData e) => target = Vector3.one;
    public void OnPointerDown(PointerEventData e) => target = Vector3.one * pressScale;
    public void OnPointerUp(PointerEventData e) => target = Vector3.one * hoverScale;
}
