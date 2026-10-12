using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class DemoController : MonoBehaviour
{
    [Header("Sails")]
    [SerializeField] private Renderer[] sailRenderers;

    [Header("Wind HUD")]
    [SerializeField] private PanelRenderer windHudRenderer;
    [SerializeField] private float windHudHiddenX = -400f;
    [SerializeField] private float windHudAnimationDuration = 0.5f;
    [SerializeField] private bool windHudStartsVisible;

    [Header("Released Objects")]
    [SerializeField] private GameObject[] releasedObjects;
    [SerializeField] private float releaseDelay = 0.5f;

    private VisualElement _windHudRoot;

    private bool _sailsVisible = true;
    private bool _windHudVisible;
    private bool _objectsReleased;

    private float _windHudProgress;

    private void OnEnable()
    {
        windHudRenderer.RegisterUIReloadCallback(OnWindHudReload);
    }

    private void OnDisable()
    {
        windHudRenderer.UnregisterUIReloadCallback(OnWindHudReload);
    }

    private void Start()
    {
        _windHudVisible = windHudStartsVisible;
        _windHudProgress = _windHudVisible ? 1f : 0f;
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
            ToggleSails();

        if (Keyboard.current.digit2Key.wasPressedThisFrame)
            _windHudVisible = !_windHudVisible;

        if (Keyboard.current.digit3Key.wasPressedThisFrame && !_objectsReleased)
            StartCoroutine(ReleaseObjects());

        UpdateWindHudAnimation();
    }

    private void ToggleSails()
    {
        _sailsVisible = !_sailsVisible;

        foreach (Renderer sailRenderer in sailRenderers)
        {
            if (sailRenderer != null)
                sailRenderer.enabled = _sailsVisible;
        }
    }

    private IEnumerator ReleaseObjects()
    {
        _objectsReleased = true;

        foreach (GameObject releasedObject in releasedObjects)
        {
            if (releasedObject == null)
                continue;

            releasedObject.transform.SetParent(null, true);
            releasedObject.SetActive(true);

            yield return new WaitForSeconds(releaseDelay);
        }
    }

    private void UpdateWindHudAnimation()
    {
        if (_windHudRoot == null)
            return;

        float target = _windHudVisible ? 1f : 0f;

        _windHudProgress = Mathf.MoveTowards(_windHudProgress, target, Time.deltaTime / windHudAnimationDuration
        );

        float smoothProgress = Mathf.SmoothStep(0f, 1f, _windHudProgress);
        float positionX = Mathf.Lerp(windHudHiddenX, 0f, smoothProgress);

        _windHudRoot.style.translate = new Translate( new Length(positionX, LengthUnit.Pixel), 0f);
    }

    private void OnWindHudReload(PanelRenderer renderer, VisualElement rootElement)
    {
        _windHudRoot = rootElement.Q<VisualElement>("wind-hud-root");

        if (_windHudRoot == null)
            return;

        float positionX = windHudStartsVisible ? 0f : windHudHiddenX;

        _windHudRoot.style.translate = new Translate( new Length(positionX, LengthUnit.Pixel), 0f);
    }
}