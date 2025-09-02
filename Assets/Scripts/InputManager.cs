using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class InputManager : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public UnityAction OnPointerEnterAction { get; set; }
    public UnityAction OnPointerExitAction { get; set; }
    public UnityAction OnPointerDownAction { get; set; }
    public UnityAction OnPointerUpAction { get; set; }
    public UnityAction OnClickAction { get; set; }

    private void Start()
    {
        // We can add a Button component and use its onClick, or handle it manually.
        // For flexibility with custom animations, we handle it via interfaces.
        var button = GetComponent<UnityEngine.UI.Button>();
        if (button != null)
        {
            button.onClick.AddListener(() => OnClickAction?.Invoke());
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        OnPointerEnterAction?.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnPointerExitAction?.Invoke();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnPointerDownAction?.Invoke();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        OnPointerUpAction?.Invoke();
    }
}
