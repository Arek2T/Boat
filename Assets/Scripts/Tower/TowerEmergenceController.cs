using UnityEngine;

public class TowerEmergenceController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TowerImpactCollapseController collapseController;

    [Header("Detection")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float activationDistance = 20f;
    [SerializeField] private float detectionInterval = 0.1f;
    [SerializeField] private bool ignoreYForDetection = true;

    [Header("Emergence")]
    [SerializeField] private float emergeYOffset = 10f;
    [SerializeField] private float emergeDuration = 2f;
    [SerializeField] private AnimationCurve emergeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Shake")]
    [SerializeField] private bool enableShake = true;
    [SerializeField] private Vector3 shakeAmplitude = new Vector3(0.08f, 0.02f, 0.08f);
    [SerializeField] private float shakeFrequency = 10f;
    [SerializeField] private AnimationCurve shakeStrengthCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

    private float nextDetectionTime;
    private float emergeElapsed;

    private bool emerging;
    private bool emergenceFinished;

    private Vector3 emergeStartPosition;
    private Vector3 emergeTargetPosition;

    private float shakeSeedX;
    private float shakeSeedY;
    private float shakeSeedZ;

    private void Awake()
    {
        if (collapseController == null)
            collapseController = GetComponent<TowerImpactCollapseController>();

        shakeSeedX = Random.Range(0f, 1000f);
        shakeSeedY = Random.Range(0f, 1000f);
        shakeSeedZ = Random.Range(0f, 1000f);
    }

    private void Update()
    {
        if (collapseController != null && collapseController.Triggered)
        {
            emerging = false;
            return;
        }

        if (!emerging && !emergenceFinished)
            CheckForPlayer();

        if (emerging)
            UpdateEmergence();
    }

    private void CheckForPlayer()
    {
        if (Time.time < nextDetectionTime)
            return;

        nextDetectionTime = Time.time + detectionInterval;

        GameObject[] players = GameObject.FindGameObjectsWithTag(playerTag);
        float sqrActivationDistance = activationDistance * activationDistance;

        foreach (GameObject player in players)
        {
            Vector3 difference = player.transform.position - transform.position;

            if (ignoreYForDetection)
                difference.y = 0f;

            if (difference.sqrMagnitude > sqrActivationDistance)
                continue;

            StartEmergence();
            return;
        }
    }

    private void StartEmergence()
    {
        emerging = true;
        emergeElapsed = 0f;

        emergeStartPosition = transform.position;
        emergeTargetPosition = emergeStartPosition + Vector3.up * emergeYOffset;
    }

    private void UpdateEmergence()
    {
        if (collapseController != null && collapseController.Triggered)
        {
            emerging = false;
            return;
        }

        if (emergeDuration <= 0f)
        {
            transform.position = emergeTargetPosition;
            FinishEmergence();
            return;
        }

        emergeElapsed += Time.deltaTime;

        float t = Mathf.Clamp01(emergeElapsed / emergeDuration);
        float curvedT = emergeCurve.Evaluate(t);

        Vector3 basePosition = Vector3.LerpUnclamped(
            emergeStartPosition,
            emergeTargetPosition,
            curvedT
        );

        if (enableShake)
            basePosition += CalculateShake(t);

        transform.position = basePosition;

        if (t >= 1f)
        {
            transform.position = emergeTargetPosition;
            FinishEmergence();
        }
    }

    private Vector3 CalculateShake(float normalizedTime)
    {
        float strength = shakeStrengthCurve.Evaluate(normalizedTime);
        float time = Time.time * shakeFrequency;

        float x = Mathf.PerlinNoise(shakeSeedX, time) * 2f - 1f;
        float y = Mathf.PerlinNoise(shakeSeedY, time) * 2f - 1f;
        float z = Mathf.PerlinNoise(shakeSeedZ, time) * 2f - 1f;

        return new Vector3(
            x * shakeAmplitude.x,
            y * shakeAmplitude.y,
            z * shakeAmplitude.z
        ) * strength;
    }

    private void FinishEmergence()
    {
        emerging = false;
        emergenceFinished = true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, activationDistance);
    }
}