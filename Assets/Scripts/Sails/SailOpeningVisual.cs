using UnityEngine;

public class SailOpeningVisual : MonoBehaviour
{
    private enum SailType
    {
        Standard,
        Triangular
    }

    [SerializeField] private SailController sailController;

    [Header("Type")]
    [SerializeField] private SailType sailType;

    [Header("Sail Rotation")]
    [SerializeField] private Vector3 rotationPivotOffset = new Vector3(0f, 0f, -0.2f);

    [Header("Sail Parts")]
    [SerializeField] private Transform topBeam;
    [SerializeField] private Transform bottomBeam;
    [SerializeField] private Transform sail;

    [Header("Closed State")]
    [SerializeField] private float closedBeamYOffset = 0.1f;
    [SerializeField, Range(0.01f, 1f)] private float closedSailScale = 0.05f;

    [Header("Triangular Sail")]
    [SerializeField] private Vector3 closedRotationOffset;
    [SerializeField] private float closedGlobalYOffset;

    private float _previousSailRotation;

    private Vector3 _topBeamOpenPosition;
    private Vector3 _topBeamClosedPosition;

    private Vector3 _sailOpenPosition;
    private Vector3 _sailOpenScale;
    private Quaternion _sailOpenRotation;
    private Quaternion _sailClosedRotation;

    private Vector3 _sailBottomLocalPoint;
    private Vector3 _sailBottomAnchor;

    private void Awake()
    {
        if (sailType == SailType.Standard)
        {
            _topBeamOpenPosition = transform.InverseTransformPoint(topBeam.position);

            Vector3 bottomBeamPosition = transform.InverseTransformPoint(bottomBeam.position);

            _topBeamClosedPosition = _topBeamOpenPosition;
            _topBeamClosedPosition.y = bottomBeamPosition.y + closedBeamYOffset;
        }

        _sailOpenPosition = sail.localPosition;
        _sailOpenScale = sail.localScale;
        _sailOpenRotation = sail.localRotation;
        _sailClosedRotation = _sailOpenRotation * Quaternion.Euler(closedRotationOffset);

        MeshFilter meshFilter = sail.GetComponent<MeshFilter>();
        Bounds bounds = meshFilter.sharedMesh.bounds;

        _sailBottomLocalPoint = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        _sailBottomAnchor = sail.parent.InverseTransformPoint(sail.TransformPoint(_sailBottomLocalPoint));
    }

    private void LateUpdate()
    {
        float opening = sailController.SailOpening / 100f;

        UpdateParentRotation();

        if (sailType == SailType.Standard)
            UpdateTopBeam(opening);

        UpdateSail(opening);
    }

    private void UpdateParentRotation()
    {
        float rotationDelta = sailController.SailRotation - _previousSailRotation;

        transform.RotateAround(GetRotationPivot(), transform.up, rotationDelta);

        _previousSailRotation = sailController.SailRotation;
    }

    private void UpdateTopBeam(float opening)
    {
        Vector3 currentPosition = Vector3.Lerp(_topBeamClosedPosition, _topBeamOpenPosition, opening);
        topBeam.position = transform.TransformPoint(currentPosition);
    }

    private void UpdateSail(float opening)
    {
        float scaleMultiplier = Mathf.Lerp(closedSailScale, 1f, opening);

        Vector3 scale = _sailOpenScale;
        scale.y *= scaleMultiplier;

        sail.localPosition = _sailOpenPosition;
        sail.localScale = scale;

        if (sailType == SailType.Triangular)
            sail.localRotation = Quaternion.Slerp(_sailClosedRotation, _sailOpenRotation, opening);
        else
            sail.localRotation = _sailOpenRotation;

        KeepBottomAttached();

        if (sailType == SailType.Triangular)
            UpdateTriangularPosition(opening);
    }

    private void KeepBottomAttached()
    {
        Vector3 currentBottomPosition = sail.parent.InverseTransformPoint(sail.TransformPoint(_sailBottomLocalPoint));
        sail.localPosition += _sailBottomAnchor - currentBottomPosition;
    }

    private void UpdateTriangularPosition(float opening)
    {
        float yOffset = closedGlobalYOffset * (1f - opening);
        sail.position += Vector3.up * yOffset;
    }

    private Vector3 GetRotationPivot()
    {
        return sail.position + transform.TransformDirection(rotationPivotOffset);
    }

    private void OnDrawGizmosSelected()
    {
        if (sail == null)
            return;

        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(GetRotationPivot(), 0.1f);
    }
}