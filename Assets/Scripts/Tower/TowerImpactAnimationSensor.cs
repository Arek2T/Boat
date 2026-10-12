using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TowerImpactAnimationSensor : MonoBehaviour
{
    [SerializeField] private TowerImpactCollapseController tower;

    private void Awake()
    {
        if (tower == null)
            tower = GetComponent<TowerImpactCollapseController>();

        Rigidbody rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (tower == null || tower.Triggered)
            return;

        if (tower.IsPartOfTower(other.transform))
            return;

        if (other.attachedRigidbody != null)
            return;

        tower.TriggerImpact(other.bounds.center);

        TentacleBehaviour tentacle = other.GetComponentInParent<TentacleBehaviour>();

        if (tentacle != null)
            tentacle.OnHeavyImpact();
    }
}