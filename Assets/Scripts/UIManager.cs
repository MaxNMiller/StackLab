using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("UI References")]
    public Button spawnButton;
    public Button freezeButton;
    public TextMeshProUGUI counterText;
    public TextMeshProUGUI aiMessageText;

    [Header("UI Animations")]
    public float buttonHoverScale = 1.05f;
    public float buttonClickScale = 0.95f;
    public float buttonAnimDuration = 0.15f;
    public float textPunchStrength = 0.5f;
    public float textPunchDuration = 0.5f;
    public Color textHighlightColor = new Color(1.2f, 1.2f, 1.2f, 1f);
    public float messageFadeDuration = 1.5f;
    public AnimationCurve buttonScaleCurve;
    public AnimationCurve textPunchCurve;

    private Vector3 buttonOriginalScale;
    private Vector3 counterOriginalScale;
    private Color counterOriginalColor;
    private Color messageOriginalColor;
    private Coroutine buttonAnimation;
    private Coroutine counterAnimation;
    private Coroutine messageAnimation;

    public UnityEngine.Events.UnityAction OnFreezeClicked { get; set; }

    void Awake()
    {
        counterText.text = "0";
        aiMessageText.text = "";

        buttonOriginalScale = spawnButton.transform.localScale;
        counterOriginalScale = counterText.transform.localScale;
        counterOriginalColor = counterText.color;
        messageOriginalColor = aiMessageText.color;

        freezeButton.onClick.AddListener(() => OnFreezeClicked?.Invoke());
    }

    public void OnButtonPointerEnter()
    {
        if (buttonAnimation != null) StopCoroutine(buttonAnimation);
        buttonAnimation = StartCoroutine(ScaleButton(buttonOriginalScale * buttonHoverScale, buttonAnimDuration));
    }

    public void OnButtonPointerExit()
    {
        if (buttonAnimation != null) StopCoroutine(buttonAnimation);
        buttonAnimation = StartCoroutine(ScaleButton(buttonOriginalScale, buttonAnimDuration));
    }

    public void OnButtonPointerDown()
    {
        if (buttonAnimation != null) StopCoroutine(buttonAnimation);
        buttonAnimation = StartCoroutine(ScaleButton(buttonOriginalScale * buttonClickScale, buttonAnimDuration * 0.5f));
    }

    public void OnButtonPointerUp()
    {
        if (buttonAnimation != null) StopCoroutine(buttonAnimation);
        buttonAnimation = StartCoroutine(ScaleButton(buttonOriginalScale * buttonHoverScale, buttonAnimDuration));
    }

    IEnumerator ScaleButton(Vector3 targetScale, float duration)
    {
        Vector3 initialScale = spawnButton.transform.localScale;
        float time = 0f;

        while (time < duration)
        {
            float t = time / duration;
            if (buttonScaleCurve != null && buttonScaleCurve.keys.Length > 0)
            {
                t = buttonScaleCurve.Evaluate(t);
            }

            spawnButton.transform.localScale = Vector3.Lerp(initialScale, targetScale, t);
            time += Time.deltaTime;
            yield return null;
        }

        spawnButton.transform.localScale = targetScale;
    }

    public void UpdateCounter(int count)
    {
        counterText.text = count.ToString();
        if (counterAnimation != null) StopCoroutine(counterAnimation);
        counterAnimation = StartCoroutine(PunchText(counterText, counterOriginalScale, textPunchStrength, textPunchDuration));
    }

    public void UpdateAIMessage(int tallestStack)
    {
        string message = "";
        if (tallestStack > 3)
        {
            message = "Are you trying to reach me?";
        }
        if (tallestStack >= 20) // Assuming maxPresses is 20
        {
            message = "No!!!!!! Leave me alone!";
        }

        if (!string.IsNullOrEmpty(message) && aiMessageText.text != message)
        {
            if (messageAnimation != null) StopCoroutine(messageAnimation);
            messageAnimation = StartCoroutine(FadeMessage(message));
        }
    }

    private IEnumerator PunchText(TextMeshProUGUI text, Vector3 originalScale, float strength, float duration)
    {
        float time = 0f;
        Color originalColor = text.color;

        while (time < duration)
        {
            float t = time / duration;
            if (textPunchCurve != null && textPunchCurve.keys.Length > 0)
            {
                t = textPunchCurve.Evaluate(t);
            }

            float scaleFactor = 1f + (strength * (1f - t));
            text.transform.localScale = originalScale * scaleFactor;
            text.color = Color.Lerp(originalColor, textHighlightColor, Mathf.PingPong(t * 2f, 1f));

            time += Time.deltaTime;
            yield return null;
        }

        text.transform.localScale = originalScale;
        text.color = originalColor;
    }

    private IEnumerator FadeMessage(string message)
    {
        // Fade out current message
        yield return StartCoroutine(FadeOutMessage(aiMessageText, messageFadeDuration * 0.5f));

        // Set new message and fade in
        aiMessageText.text = message;
        yield return StartCoroutine(FadeInMessage(aiMessageText, messageFadeDuration));
    }

    private IEnumerator FadeInMessage(TextMeshProUGUI text, float duration)
    {
        text.gameObject.SetActive(true);
        Color targetColor = messageOriginalColor;
        Color transparentColor = new Color(targetColor.r, targetColor.g, targetColor.b, 0f);
        text.color = transparentColor;

        float time = 0f;
        while (time < duration)
        {
            text.color = Color.Lerp(transparentColor, targetColor, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        text.color = targetColor;
    }

    private IEnumerator FadeOutMessage(TextMeshProUGUI text, float duration)
    {
        Color initialColor = text.color;
        if (initialColor.a == 0) yield break; // Already faded out

        Color transparentColor = new Color(initialColor.r, initialColor.g, initialColor.b, 0f);

        float time = 0f;
        while (time < duration)
        {
            text.color = Color.Lerp(initialColor, transparentColor, time / duration);
            time += Time.deltaTime;
            yield return null;
        }

        text.color = transparentColor;
        text.gameObject.SetActive(false);
    }

    public void DisableButton()
    {
        spawnButton.interactable = false;
        StartCoroutine(ScaleButton(Vector3.zero, buttonAnimDuration * 2f));
    }
}
