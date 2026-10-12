using System.Collections;
using UnityEngine;

public class TowerImpactCollapseController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform visualPivot;
    [SerializeField] private Transform mainPivotPoint;

    [Header("Collapse")]
    [SerializeField] private float fallAngle = 90f;
    [SerializeField] private float fallDuration = 1.5f;
    [SerializeField] private float releaseDelay = 0.7f;
    [SerializeField] private AnimationCurve fallCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Fall Direction")]
    [SerializeField] private bool overrideFallDirection = false;
    [SerializeField] private Vector3 localFallDirection = Vector3.forward;

    [Header("Direction Gizmo")]
    [SerializeField] private bool drawDirectionGizmo = true;
    [SerializeField] private float directionGizmoLength = 4f;
    [SerializeField] private float directionGizmoArrowSize = 0.6f;

    [Header("Physics")]
    [SerializeField] private float inheritedVelocityMultiplier = 1f;

    private Rigidbody[] segments;
    private Transform runtimePivot;

    private Quaternion initialPivotRotation;
    private Vector3 rotationAxis;

    private bool triggered;

    public bool Triggered => triggered;

    private void Awake()
    {
        if (visualPivot == null || mainPivotPoint == null)
        {
            Debug.LogError($"{name}: VisualPivot or MainPivotPoint is not assigned.");
            enabled = false;
            return;
        }

        CreateRuntimePivot();

        segments = visualPivot.GetComponentsInChildren<Rigidbody>(true);

        foreach (Rigidbody rb in segments)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        initialPivotRotation = runtimePivot.rotation;
    }

    public void TriggerImpact(Vector3 impactSourcePosition)
    {
        if (triggered)
            return;

        Vector3 fallDirection;

        if (overrideFallDirection)
        {
            fallDirection = transform.TransformDirection(localFallDirection);
        }
        else
        {
            fallDirection = runtimePivot.position - impactSourcePosition;
        }

        fallDirection.y = 0f;

        if (fallDirection.sqrMagnitude < 0.001f)
            fallDirection = transform.forward;

        fallDirection.Normalize();

        rotationAxis = Vector3.Cross(Vector3.up, fallDirection).normalized;

        if (rotationAxis.sqrMagnitude < 0.001f)
            return;

        triggered = true;
        StartCoroutine(CollapseRoutine());
    }

    public bool IsPartOfTower(Transform target)
    {
        return target == transform || target.IsChildOf(transform);
    }

    private void CreateRuntimePivot()
    {
        GameObject pivotObject = new GameObject($"{name}_RuntimePivot");
        runtimePivot = pivotObject.transform;

        runtimePivot.SetParent(visualPivot.parent, true);
        runtimePivot.position = mainPivotPoint.position;
        runtimePivot.rotation = mainPivotPoint.rotation;

        visualPivot.SetParent(runtimePivot, true);
    }

    private IEnumerator CollapseRoutine()
    {
        float releaseTime = Mathf.Clamp(releaseDelay, 0f, fallDuration);
        float elapsed = 0f;

        while (elapsed < releaseTime)
        {
            elapsed += Time.deltaTime;
            ApplyRotation(elapsed);

            yield return null;
        }

        ApplyRotation(releaseTime);

        Physics.SyncTransforms();
        ReleasePhysics(releaseTime);
    }

    private void ApplyRotation(float time)
    {
        float t = Mathf.Clamp01(time / Mathf.Max(0.001f, fallDuration));
        float angle = fallAngle * fallCurve.Evaluate(t);

        runtimePivot.rotation =
            Quaternion.AngleAxis(angle, rotationAxis) *
            initialPivotRotation;
    }

    private void ReleasePhysics(float releaseTime)
    {
        Vector3 angularVelocity = CalculateAngularVelocity(releaseTime) * inheritedVelocityMultiplier;
        Vector3 pivotPosition = runtimePivot.position;

        foreach (Rigidbody rb in segments)
        {
            if (rb == null)
                continue;

            Vector3 radius = rb.worldCenterOfMass - pivotPosition;
            Vector3 linearVelocity = Vector3.Cross(angularVelocity, radius);

            rb.isKinematic = false;
            rb.useGravity = true;

#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = linearVelocity;
#else
            rb.velocity = linearVelocity;
#endif

            rb.angularVelocity = angularVelocity;
            rb.WakeUp();
        }
    }

    private Vector3 CalculateAngularVelocity(float time)
    {
        const float sampleDelta = 0.02f;

        float previousTime = Mathf.Max(0f, time - sampleDelta);
        float deltaTime = Mathf.Max(0.001f, time - previousTime);

        float previousAngle = GetAngle(previousTime);
        float currentAngle = GetAngle(time);

        float angularSpeed =
            (currentAngle - previousAngle) *
            Mathf.Deg2Rad /
            deltaTime;

        return rotationAxis * angularSpeed;
    }

    private float GetAngle(float time)
    {
        float t = Mathf.Clamp01(time / Mathf.Max(0.001f, fallDuration));
        return fallAngle * fallCurve.Evaluate(t);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDirectionGizmo || !overrideFallDirection)
            return;

        Transform pivot = mainPivotPoint != null ? mainPivotPoint : transform;

        Vector3 direction = transform.TransformDirection(localFallDirection);
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        direction.Normalize();

        Vector3 start = pivot.position;
        Vector3 end = start + direction * directionGizmoLength;

        Gizmos.DrawLine(start, end);

        Vector3 side = Vector3.Cross(Vector3.up, direction).normalized;

        Vector3 arrowLeft =
            end - direction * directionGizmoArrowSize +
            side * directionGizmoArrowSize * 0.5f;

        Vector3 arrowRight =
            end - direction * directionGizmoArrowSize -
            side * directionGizmoArrowSize * 0.5f;

        Gizmos.DrawLine(end, arrowLeft);
        Gizmos.DrawLine(end, arrowRight);
    }
}