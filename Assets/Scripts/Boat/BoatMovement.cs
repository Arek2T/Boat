using UnityEngine;
using UnityEngine.InputSystem;

public class BoatMovement : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference rudderAction;

    [Header("Movement")]
    [SerializeField] private SailController sailController;
    [SerializeField] private float sailAcceleration = 0.6f;
    [SerializeField] private float sailBraking = 0.8f;
    [SerializeField] private float waterDrag = 0.35f;

    [Header("Rudder")]
    [SerializeField] private float turnSpeed = 25f;
    [SerializeField] private float steeringReferenceSpeed = 5f;
    [SerializeField] private float rudderResponseSpeed = 2f;
    [SerializeField] private Vector3 steeringPivotOffset = new Vector3(0f, 0f, -2f);
    [SerializeField] private Transform rudderVisual;
    [SerializeField] private float rudderMaxAngle = 30f;
    [SerializeField] private Vector3 rudderRotationOffset;

    [Header("Visual")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Vector3 rockingPivotOffset = new Vector3(0f, -1f, 0f);
    [SerializeField] private float visualReferenceSpeed = 10f;

    [Header("Rocking")]
    [SerializeField] private float slowRockAngle = 6f;
    [SerializeField] private float fastRockAngle = 2f;
    [SerializeField] private float slowRockSpeed = 0.8f;
    [SerializeField] private float fastRockSpeed = 2f;

    [Header("Bobbing")]
    [SerializeField] private float slowBobHeight = 0.25f;
    [SerializeField] private float fastBobHeight = 0.05f;
    [SerializeField] private float slowBobSpeed = 0.8f;
    [SerializeField] private float fastBobSpeed = 1.8f;

    private float _currentSpeed;
    private float _currentRudder;
    private float _rockPhase;
    private float _bobPhase;

    private Vector3 _visualBasePosition;
    private Quaternion _visualBaseRotation;
    private Quaternion _rudderBaseRotation;

    public float CurrentSpeed => _currentSpeed;
    public Vector3 Velocity => transform.forward * _currentSpeed;

    private void Awake()
    {
        _visualBasePosition = visualRoot.localPosition;
        _visualBaseRotation = visualRoot.localRotation;
        _rudderBaseRotation = rudderVisual.localRotation * Quaternion.Euler(rudderRotationOffset);
    }

    private void OnEnable()
    {
        rudderAction.action.Enable();
    }

    private void OnDisable()
    {
        rudderAction.action.Disable();
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;

        UpdateSpeed(deltaTime);
        UpdateRudder(deltaTime);
        UpdateMovement(deltaTime);
        UpdateRockingAndBobbing(deltaTime);
    }

    private void UpdateSpeed(float deltaTime)
    {
        float sailForce = sailController.SailForce;

        if (sailForce > 0f)
            _currentSpeed += sailForce * sailAcceleration * deltaTime;
        else
            _currentSpeed += sailForce * sailBraking * deltaTime;

        _currentSpeed -= waterDrag * _currentSpeed * deltaTime;
        _currentSpeed = Mathf.Max(0f, _currentSpeed);
    }

    private void UpdateRudder(float deltaTime)
    {
        float input = rudderAction.action.ReadValue<float>();

        _currentRudder = Mathf.MoveTowards(_currentRudder, input, rudderResponseSpeed * deltaTime);

        float steeringStrength = Mathf.Clamp01(_currentSpeed / steeringReferenceSpeed);
        float turnAmount = _currentRudder * turnSpeed * steeringStrength * deltaTime;

        Vector3 steeringPivot = transform.TransformPoint(steeringPivotOffset);
        transform.RotateAround(steeringPivot, Vector3.up, turnAmount);

        UpdateRudderVisual();
    }

    private void UpdateMovement(float deltaTime)
    {
        transform.position += transform.forward * _currentSpeed * deltaTime;
    }

    private void UpdateRockingAndBobbing(float deltaTime)
    {
        float speedRatio = Mathf.Clamp01(_currentSpeed / visualReferenceSpeed);

        float rockAngle = Mathf.Lerp(slowRockAngle, fastRockAngle, speedRatio);
        float rockSpeed = Mathf.Lerp(slowRockSpeed, fastRockSpeed, speedRatio);

        _rockPhase += rockSpeed * deltaTime;

        float roll = Mathf.Sin(_rockPhase) * rockAngle;
        float pitch = Mathf.Sin(_rockPhase * 0.7f + 1.5f) * rockAngle * 0.35f;

        float bobHeight = Mathf.Lerp(slowBobHeight, fastBobHeight, speedRatio);
        float bobSpeed = Mathf.Lerp(slowBobSpeed, fastBobSpeed, speedRatio);

        _bobPhase += bobSpeed * deltaTime;

        float bob = Mathf.Sin(_bobPhase) * bobHeight;
        Quaternion rockingRotation = Quaternion.Euler(pitch, 0f, roll);

        Vector3 pivotPosition = _visualBasePosition + rockingPivotOffset;
        Vector3 offsetFromPivot = _visualBasePosition - pivotPosition;

        visualRoot.localPosition = pivotPosition + rockingRotation * offsetFromPivot + Vector3.up * bob;
        visualRoot.localRotation = rockingRotation * _visualBaseRotation;
    }

    private void UpdateRudderVisual()
    {
        float rudderAngle = -_currentRudder * rudderMaxAngle;
        rudderVisual.localRotation = _rudderBaseRotation * Quaternion.Euler(0f, rudderAngle, 0f);
    }

    private void OnDrawGizmosSelected()
    {
        if (visualRoot != null && visualRoot.parent != null)
        {
            Vector3 basePosition = Application.isPlaying ? _visualBasePosition : visualRoot.localPosition;
            Vector3 pivotLocalPosition = basePosition + rockingPivotOffset;
            Vector3 pivotWorldPosition = visualRoot.parent.TransformPoint(pivotLocalPosition);
            Vector3 rootWorldPosition = visualRoot.parent.TransformPoint(basePosition);

            Gizmos.color = Color.white;
            Gizmos.DrawSphere(pivotWorldPosition, 0.15f);
            Gizmos.DrawLine(rootWorldPosition, pivotWorldPosition);
        }

        Vector3 steeringPivot = transform.TransformPoint(steeringPivotOffset);

        Gizmos.color = Color.white;
        Gizmos.DrawSphere(steeringPivot, 0.2f);
        Gizmos.DrawLine(transform.position, steeringPivot);
    }
}