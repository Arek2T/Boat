using System.Collections;
using UnityEngine;

public class TentacleBehaviour : MonoBehaviour
{
    public enum AttackMode
    {
        Smack,
        SwipeLeft,
        SwipeRight
    }

    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("Behaviour")]
    [SerializeField] private AttackMode attackMode = AttackMode.Smack;
    [SerializeField] private float activationDistance = 8f;
    [SerializeField] private float detectionInterval = 0.1f;
    [SerializeField] private bool returnToIdleAfterAttack = true;
    [SerializeField] private bool ignoreYForDetection = false;

    [Header("Idle Speed")]
    [SerializeField] private bool randomizeIdleSpeed = true;
    [SerializeField] private float idleSpeed = 1f;
    [SerializeField] private Vector2 idleSpeedRange = new Vector2(0.8f, 1.2f);

    [Header("Attack Speed")]
    [SerializeField] private float attackSpeed = 1f;

    [Header("Emergence")]
    [SerializeField] private bool enableEmergence = false;
    [SerializeField] private float emergeActivationDistance = 15f;
    [SerializeField] private float emergeYOffset = 10f;
    [SerializeField] private float emergeDuration = 1f;
    [SerializeField] private AnimationCurve emergeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Animator Parameters")]
    [SerializeField] private string idleBool = "idle";
    [SerializeField] private string smackTrigger = "smack";
    [SerializeField] private string swipeLeftTrigger = "swipeLeft";
    [SerializeField] private string swipeRightTrigger = "swipeRight";
    [SerializeField] private string idleSpeedParameter = "idleSpeed";
    [SerializeField] private string attackSpeedParameter = "attackSpeed";

    [Header("Animator States")]
    [SerializeField] private string smackState = "smack";
    [SerializeField] private string swipeLeftState = "swipeLeft";
    [SerializeField] private string swipeRightState = "swipeRight";

    [Header("Impact Slowdown")]
    [SerializeField] private bool slowAttackOnImpact = true;
    [SerializeField] private float impactAttackSpeedMultiplier = 0.2f;
    [SerializeField] private float impactSlowdownDuration = 0.25f;

    private Coroutine impactSlowdownCoroutine;

    private float nextDetectionTime;
    private bool armed = true;
    private bool attacking;
    private bool emergenceStarted;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private void Start()
    {
        SetupIdleSpeed();

        animator.SetFloat(attackSpeedParameter, Mathf.Max(0.01f, attackSpeed));
        animator.SetBool(idleBool, true);
    }

    private void Update()
    {
        if (Time.time < nextDetectionTime)
            return;

        nextDetectionTime = Time.time + detectionInterval;

        CheckPlayers(out bool playerInAttackRange, out bool playerInEmergenceRange);

        if (enableEmergence && playerInEmergenceRange && !emergenceStarted)
            StartCoroutine(Emerge());

        if (playerInAttackRange && armed && !attacking)
        {
            armed = false;
            StartAttack();
        }
        else if (!playerInAttackRange && !attacking)
        {
            armed = true;
        }
    }

    private void SetupIdleSpeed()
    {
        float speed = idleSpeed;

        if (randomizeIdleSpeed)
        {
            float min = Mathf.Min(idleSpeedRange.x, idleSpeedRange.y);
            float max = Mathf.Max(idleSpeedRange.x, idleSpeedRange.y);
            speed = Random.Range(min, max);
        }

        animator.SetFloat(idleSpeedParameter, Mathf.Max(0f, speed));
    }

    private void StartAttack()
    {
        attacking = true;

        animator.SetFloat(attackSpeedParameter, Mathf.Max(0.01f, attackSpeed));

        if (!returnToIdleAfterAttack)
            animator.SetBool(idleBool, false);

        switch (attackMode)
        {
            case AttackMode.Smack:
                animator.SetTrigger(smackTrigger);
                StartCoroutine(WaitForAttack(smackState));
                break;

            case AttackMode.SwipeLeft:
                animator.SetTrigger(swipeLeftTrigger);
                StartCoroutine(WaitForAttack(swipeLeftState));
                break;

            case AttackMode.SwipeRight:
                animator.SetTrigger(swipeRightTrigger);
                StartCoroutine(WaitForAttack(swipeRightState));
                break;
        }
    }

    private IEnumerator WaitForAttack(string stateName)
    {
        yield return WaitForState(stateName);

        while (animator.GetCurrentAnimatorStateInfo(0).IsName(stateName) &&
               animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.99f)
        {
            yield return null;
        }

        attacking = false;
    }

    private IEnumerator WaitForState(string stateName)
    {
        while (true)
        {
            AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
            AnimatorStateInfo nextState = animator.GetNextAnimatorStateInfo(0);

            if (currentState.IsName(stateName) || nextState.IsName(stateName))
                yield break;

            yield return null;
        }
    }

    private IEnumerator Emerge()
    {
        emergenceStarted = true;

        Vector3 startPosition = transform.position;
        Vector3 targetPosition = startPosition + Vector3.up * emergeYOffset;

        if (emergeDuration <= 0f)
        {
            transform.position = targetPosition;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < emergeDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / emergeDuration);
            float curvedT = emergeCurve.Evaluate(t);

            transform.position = Vector3.LerpUnclamped(startPosition, targetPosition, curvedT);

            yield return null;
        }

        transform.position = targetPosition;
    }

    private void CheckPlayers(out bool playerInAttackRange, out bool playerInEmergenceRange)
    {
        playerInAttackRange = false;
        playerInEmergenceRange = false;

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        float attackSqrDistance = activationDistance * activationDistance;
        float emergenceSqrDistance = emergeActivationDistance * emergeActivationDistance;

        foreach (GameObject player in players)
        {
            Vector3 difference = player.transform.position - transform.position;

            if (ignoreYForDetection)
                difference.y = 0f;

            float sqrDistance = difference.sqrMagnitude;

            if (sqrDistance <= attackSqrDistance)
                playerInAttackRange = true;

            if (enableEmergence && sqrDistance <= emergenceSqrDistance)
                playerInEmergenceRange = true;

            if (playerInAttackRange && (!enableEmergence || playerInEmergenceRange))
                return;
        }
    }

    public void OnHeavyImpact()
    {
        if (!slowAttackOnImpact || !attacking)
            return;

        if (impactSlowdownCoroutine != null)
            StopCoroutine(impactSlowdownCoroutine);

        impactSlowdownCoroutine = StartCoroutine(ImpactSlowdown());
    }

    private IEnumerator ImpactSlowdown()
    {
        float slowedSpeed = Mathf.Max(0.01f, attackSpeed * impactAttackSpeedMultiplier);

        animator.SetFloat(attackSpeedParameter, slowedSpeed);

        yield return new WaitForSeconds(impactSlowdownDuration);

        animator.SetFloat(attackSpeedParameter, Mathf.Max(0.01f, attackSpeed));

        impactSlowdownCoroutine = null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, activationDistance);

        if (enableEmergence)
            Gizmos.DrawWireSphere(transform.position, emergeActivationDistance);
    }
}