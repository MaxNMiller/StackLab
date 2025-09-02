using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SliderValueDisplay : MonoBehaviour
{
    public enum SliderType { Samples, XSTEP }
    public SliderType sliderKind;  
    public Slider slider;
    public TextMeshProUGUI valueLabel;
    public SimulationManager simulationManager;

    void Start()
    {
        if (slider == null) slider = GetComponent<Slider>();

        if (sliderKind == SliderType.Samples)
            slider.value = simulationManager.getsamples();
        else
            slider.value = simulationManager.getxstep();

        UpdateLabel(slider.value);
        slider.onValueChanged.AddListener(OnSliderChanged);
    }

    void OnSliderChanged(float value)
    {
        UpdateLabel(value);

        if (sliderKind == SliderType.Samples)
            simulationManager.setsamples(Mathf.RoundToInt(value));
        else
            simulationManager.setxstep(value);
    }

    void UpdateLabel(float value)
    {
        if (sliderKind == SliderType.Samples)
            valueLabel.text = Mathf.RoundToInt(value).ToString();
        else
            valueLabel.text = value.ToString("0.00");
    }

    void OnDestroy()
    {
        slider.onValueChanged.RemoveListener(OnSliderChanged);
    }
}
