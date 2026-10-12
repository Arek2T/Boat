using UnityEngine;
using UnityEngine.InputSystem;

public class SailController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference sailOpeningAction;
    [SerializeField] private InputActionReference sailRotationAction;

    [Header("Sail")]
    [SerializeField] private float openingSpeed = 30f;
    [SerializeField] private float rotationSpeed = 30f;
    [SerializeField, Range(-180f, 180f)] private float baseDirectionAngle;
    [SerializeField] private SailDeformer[] sailDeformers;

    [Header("Wind")]
    [SerializeField] private WindController windController;
    [SerializeField] private BoatMovement boatMovement;

    [Header("UI")]
    [SerializeField] private SailingHud sailingHud;

    [Header("Gizmo")]
    [SerializeField] private float gizmoMinLength = 0.5f;
    [SerializeField] private float gizmoMaxLength = 4f;
    [SerializeField] private float gizmoReferenceWindSpeed = 20f;
    [SerializeField] private float gizmoArrowSize = 0.5f;
    [SerializeField] private float windArrowHeightOffset = 0.35f;

    public float SailOpening { get; private set; } = 100f;
    public float SailRotation { get; private set; }

    public Vector3 WorldSailDirection
    {
        get
        {
            float angle = baseDirectionAngle + SailRotation;
            Vector3 localDirection = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;

            return transform.TransformDirection(localDirection).normalized;
        }
    }

    public Vector3 ApparentWind => windController.Velocity - boatMovement.Velocity;

    public float WindAlignment
    {
        get
        {
            Vector3 apparentWind = ApparentWind;

            if (apparentWind.magnitude < 0.01f)
                return 0f;

            return Vector3.Dot(WorldSailDirection, apparentWind.normalized);
        }
    }

    public float SailForce
    {
        get
        {
            if (SailOpening <= 0f)
                return 0f;

            float opening = SailOpening / 100f;
            return WindAlignment * ApparentWind.magnitude * opening;
        }
    }

    private void OnEnable()
    {
        sailOpeningAction.action.Enable();
        sailRotationAction.action.Enable();
    }

    private void OnDisable()
    {
        sailOpeningAction.action.Disable();
        sailRotationAction.action.Disable();
    }

    private void Start()
    {
        UpdateHud();
        UpdateSailWind();
    }

    private void Update()
    {
        float openingInput = sailOpeningAction.action.ReadValue<float>();
        float rotationInput = sailRotationAction.action.ReadValue<float>();

        SailOpening += openingInput * openingSpeed * Time.deltaTime;
        SailRotation += rotationInput * rotationSpeed * Time.deltaTime;

        SailOpening = Mathf.Clamp(SailOpening, 0f, 100f);
        SailRotation = Mathf.Clamp(SailRotation, -45f, 45f);

        UpdateHud();
        UpdateSailWind();
    }

    private void UpdateHud()
    {
        sailingHud.SetSailOpening(SailOpening);
        sailingHud.SetSailRotation(SailRotation);
    }

    private void UpdateSailWind()
    {
        Vector3 wind = SailOpening <= 0f ? Vector3.zero : ApparentWind;

        foreach (SailDeformer sailDeformer in sailDeformers)
            sailDeformer.SetApparentWind(wind);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 start = transform.position;
        Vector3 sailDirection = WorldSailDirection;
        float alignment = WindAlignment;
        float sailLength = GetSailGizmoLength(alignment);

        DrawArrow(start, sailDirection, sailLength, GetAlignmentColor(alignment));

        Vector3 apparentWind = ApparentWind;

        if (apparentWind.magnitude < 0.01f)
            return;

        float normalizedWind = Mathf.Clamp01(apparentWind.magnitude / gizmoReferenceWindSpeed);
        float windLength = Mathf.Lerp(gizmoMinLength, gizmoMaxLength, normalizedWind);
        Vector3 windStart = start + Vector3.up * windArrowHeightOffset;

        DrawArrow(windStart, apparentWind.normalized, windLength, Color.cyan);
    }

    private float GetSailGizmoLength(float alignment)
    {
        float opening = SailOpening / 100f;
        float windStrength = Mathf.Clamp01(ApparentWind.magnitude / gizmoReferenceWindSpeed);
        float positiveAlignment = Mathf.Max(0f, alignment);
        float effect = positiveAlignment * opening * windStrength;

        return Mathf.Lerp(gizmoMinLength, gizmoMaxLength, effect);
    }

    private Color GetAlignmentColor(float alignment)
    {
        if (alignment >= 0f)
            return Color.Lerp(Color.yellow, Color.green, alignment);

        return Color.Lerp(Color.yellow, Color.red, -alignment);
    }

    private void DrawArrow(Vector3 start, Vector3 direction, float length, Color color)
    {
        Vector3 end = start + direction.normalized * length;

        Gizmos.color = color;
        Gizmos.DrawLine(start, end);

        Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
        Vector3 back = -direction.normalized * gizmoArrowSize;

        Gizmos.DrawLine(end, end + back + right * gizmoArrowSize * 0.5f);
        Gizmos.DrawLine(end, end + back - right * gizmoArrowSize * 0.5f);
    }
}