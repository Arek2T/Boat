using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class BoatWakeController : MonoBehaviour
{
    [SerializeField] private BoatMovement boatMovement;
    [SerializeField] private float referenceSpeed = 10f;

    [SerializeField] private float minEmission = 5f;
    [SerializeField] private float maxEmission = 50f;

    private ParticleSystem.EmissionModule _emission;

    private void Awake()
    {
        _emission = GetComponent<ParticleSystem>().emission;
    }

    private void Update()
    {
        float speedRatio = Mathf.Clamp01( boatMovement.CurrentSpeed / referenceSpeed);

        _emission.rateOverTime = Mathf.Lerp(minEmission, maxEmission, speedRatio);
    }
}