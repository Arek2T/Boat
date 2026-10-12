using UnityEngine;

public class WindController : MonoBehaviour
{
    [SerializeField, Min(0f)] private float windSpeed = 8f;
    [SerializeField, Range(0f, 360f)] private float windDirection;

    public float Speed => windSpeed;
    public float DirectionDegrees => windDirection;

    public Vector3 Direction => Quaternion.Euler(0f, windDirection, 0f) * Vector3.forward;

    public Vector3 Velocity => Direction * windSpeed;

    public void SetSpeed(float speed)
    {
        windSpeed = Mathf.Max(0f, speed);
    }

    public void SetDirection(float direction)
    {
        windDirection = Mathf.Repeat(direction, 360f);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 start = transform.position;
        Vector3 end = start + Direction * windSpeed;

        Gizmos.DrawLine(start, end);
        Gizmos.DrawSphere(end, 0.15f);
    }
}