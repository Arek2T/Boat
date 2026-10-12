using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TowerImpactCollisionTrigger : MonoBehaviour
{
    [SerializeField] private TowerImpactCollapseController tower;

    private void Awake()
    {
        if (tower == null)
            tower = GetComponentInParent<TowerImpactCollapseController>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (tower == null || tower.Triggered)
            return;

        Rigidbody otherRigidbody = collision.rigidbody;

        if (otherRigidbody == null)
            return;

        if (tower.IsPartOfTower(otherRigidbody.transform))
            return;

        tower.TriggerImpact(otherRigidbody.worldCenterOfMass);
    }
}