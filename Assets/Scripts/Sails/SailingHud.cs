using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(PanelRenderer))]
public class SailingHud : MonoBehaviour
{
    private Label _sailOpeningLabel;
    private Label _sailRotationLabel;

    private Image _sailImage;
    private Image _rotationPlankTop;
    private Image _rotationPlankBottom;

    private float _currentOpening = 100f;
    private float _currentRotation;

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
        _sailOpeningLabel = rootElement.Q<Label>("sail-opening-label");
        _sailRotationLabel = rootElement.Q<Label>("sail-rotation-label");

        _sailImage = rootElement.Q<Image>("sail-image");
        _rotationPlankTop = rootElement.Q<Image>("rotation-plank-top");
        _rotationPlankBottom = rootElement.Q<Image>("rotation-plank-bottom");

        UpdateOpeningVisual();
        UpdateRotationVisual();
    }

    public void SetSailOpening(float opening)
    {
        _currentOpening = opening;
        UpdateOpeningVisual();
    }

    public void SetSailRotation(float rotation)
    {
        _currentRotation = rotation;
        UpdateRotationVisual();
    }

    private void UpdateOpeningVisual()
    {
        if (_sailOpeningLabel == null || _sailImage == null)
            return;

        _sailOpeningLabel.text = $"Sail Opening: {_currentOpening:0}%";

        float scaleY = Mathf.Lerp(0.1f, 1f, _currentOpening / 100f);
        _sailImage.style.scale = new Scale(new Vector2(1f, scaleY));
    }

    private void UpdateRotationVisual()
    {
        if (_sailRotationLabel == null || _rotationPlankTop == null || _rotationPlankBottom == null)
            return;

        _sailRotationLabel.text = $"Sail Rotation: {_currentRotation:0}°";

        float lengthMultiplier = 1f / Mathf.Cos(Mathf.Abs(_currentRotation) * Mathf.Deg2Rad);

        _rotationPlankTop.style.rotate = new Rotate(new Angle(-_currentRotation, AngleUnit.Degree));
        _rotationPlankBottom.style.rotate = new Rotate(new Angle(_currentRotation, AngleUnit.Degree));

        _rotationPlankTop.style.scale = new Scale(new Vector2(0.8f, 1.1f * lengthMultiplier));
        _rotationPlankBottom.style.scale = new Scale(new Vector2(0.8f, 1.1f * lengthMultiplier));
    }
}