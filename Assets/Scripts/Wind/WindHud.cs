using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(PanelRenderer))]
public class WindHud : MonoBehaviour
{
    [SerializeField] private WindController windController;

    private Slider _windSpeedSlider;
    private Slider _windDirectionSlider;

    private void OnEnable()
    {
        GetComponent<PanelRenderer>().RegisterUIReloadCallback(OnUIReload);
    }

    private void OnDisable()
    {
        GetComponent<PanelRenderer>().UnregisterUIReloadCallback(OnUIReload);
    }

    private void OnUIReload(PanelRenderer renderer, VisualElement rootElement)
    {
        _windSpeedSlider = rootElement.Q<Slider>("wind-speed-slider");
        _windDirectionSlider = rootElement.Q<Slider>("wind-direction-slider");

        _windSpeedSlider.lowValue = 0f;
        _windSpeedSlider.highValue = 20f;

        _windDirectionSlider.lowValue = 0f;
        _windDirectionSlider.highValue = 360f;

        _windSpeedSlider.focusable = false;
        _windDirectionSlider.focusable = false;

        _windSpeedSlider.SetValueWithoutNotify(windController.Speed);
        _windDirectionSlider.SetValueWithoutNotify(windController.DirectionDegrees);

        _windSpeedSlider.RegisterValueChangedCallback(OnWindSpeedChanged);
        _windDirectionSlider.RegisterValueChangedCallback(OnWindDirectionChanged);

        UpdateLabels();
    }

    private void OnWindSpeedChanged(ChangeEvent<float> evt)
    {
        windController.SetSpeed(evt.newValue);
        UpdateLabels();
    }

    private void OnWindDirectionChanged(ChangeEvent<float> evt)
    {
        windController.SetDirection(evt.newValue);
        UpdateLabels();
    }

    private void UpdateLabels()
    {
        _windSpeedSlider.label = $"Wind Speed: {windController.Speed:0.0}";
        _windDirectionSlider.label = $"Wind Direction: {windController.DirectionDegrees:0}°";
    }
}