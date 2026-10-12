using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class SailDeformer : MonoBehaviour
{
    [Header("Wind")]
    [SerializeField] private float maxWindSpeed = 20f;

    [Header("Shape")]
    [SerializeField] private float maxDeformation = 0.5f;
    [SerializeField] private float deformationSpeed = 5f;
    [SerializeField] private float gravitySag = 0.15f;

    [Header("Flutter")]
    [SerializeField] private float flutterAmount = 0.03f;
    [SerializeField] private float flutterSpeed = 3f;
    [SerializeField] private float flutterFrequency = 4f;

    [Header("Motion Variation")]
    [SerializeField] private bool randomizeMovement;
    [SerializeField, Range(0f, 100f)] private float randomPercent = 20f;

    private Mesh _sailMesh;
    private Vector3[] _originalVertices;
    private Vector3[] _deformedVertices;
    private Bounds _meshBounds;

    private Vector3 _apparentWind;

    private void Awake()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();

        _sailMesh = Instantiate(meshFilter.sharedMesh);
        meshFilter.mesh = _sailMesh;

        _originalVertices = _sailMesh.vertices;
        _deformedVertices = _sailMesh.vertices;
        _meshBounds = _sailMesh.bounds;
    }

    private void Update()
    {
        UpdateSail(Time.deltaTime);
    }

    public void SetApparentWind(Vector3 wind)
    {
        _apparentWind = wind;
    }

    private void UpdateSail(float deltaTime)
    {
        Vector3 localWind = transform.InverseTransformDirection(_apparentWind);

        float windSpeed = localWind.magnitude;
        float windStrength = Mathf.Clamp01(windSpeed / maxWindSpeed);

        float windDirection = 0f;

        if (windSpeed > 0.01f)
            windDirection = localWind.normalized.z;

        float windPressure = windDirection * windStrength;

        float speedMultiplier = randomizeMovement ? 1f - randomPercent / 100f : 1f;
        float currentDeformationSpeed = deformationSpeed * speedMultiplier;
        float currentFlutterSpeed = flutterSpeed * speedMultiplier;

        for (int i = 0; i < _originalVertices.Length; i++)
        {
            Vector3 original = _originalVertices[i];

            float height = Mathf.InverseLerp(_meshBounds.min.y, _meshBounds.max.y, original.y);
            float width = Mathf.InverseLerp(_meshBounds.min.x, _meshBounds.max.x, original.x);

            float verticalFlex = Mathf.Sin(height * Mathf.PI);
            float centerShape = 0.65f + Mathf.Sin(width * Mathf.PI) * 0.35f;
            float flexibility = verticalFlex * centerShape;

            float bulge = windPressure * maxDeformation * flexibility;
            float sag = gravitySag * flexibility;
            float flutter = Mathf.Sin(Time.time * currentFlutterSpeed + width * flutterFrequency + height * flutterFrequency) * flutterAmount * flexibility * windStrength;

            Vector3 target = original;
            target.z += bulge + flutter;
            target.y -= sag;

            _deformedVertices[i] = Vector3.Lerp(_deformedVertices[i], target, currentDeformationSpeed * deltaTime);
        }

        _sailMesh.vertices = _deformedVertices;
        _sailMesh.RecalculateNormals();
        _sailMesh.RecalculateBounds();
    }
}