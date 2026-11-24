using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyChase : MonoBehaviour
{
    public Transform player;
    public Animator animator;

    public float viewDistance = 15f;

    [Range(0f, 360f)]
    public float viewAngle = 120f;

    public float eyeHeight = 1.6f;
    public float attackDistance = 1f;
    public float chaseSpeed = 3.5f;

    [Header("Patrol")]
    public Transform[] patrolPoints;
    public float patrolSpeed = 2f;
    public float patrolWaitTime = 2f;
    public float patrolArrivalDistance = 0.3f;

    private NavMeshAgent agent;
    private string currentAnimation;
    private bool isDead = false;

    private int patrolIndex = 0;
    private float patrolWaitTimer = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = chaseSpeed;
        agent.stoppingDistance = attackDistance * 0.8f;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator != null)
            animator.applyRootMotion = false;

        PlayAnimation("Idle");
    }

    void Update()
    {
        if (isDead)
            return;

        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh)
        {
            PlayAnimation("Idle");
            return;
        }

        if (!CanSeePlayer())
        {
            Patrol();
            return;
        }

        patrolWaitTimer = 0f;
        agent.speed = chaseSpeed;
        agent.stoppingDistance = attackDistance * 0.8f;

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.magnitude <= attackDistance)
        {
            agent.isStopped = true;
            agent.updateRotation = false;

            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion rotation = Quaternion.LookRotation(direction);

                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    rotation,
                    360f * Time.deltaTime
                );
            }

            PlayAnimation("Attack");
        }
        else
        {
            agent.updateRotation = true;
            agent.isStopped = false;
            agent.SetDestination(player.position);

            PlayAnimation(
                agent.velocity.sqrMagnitude > 0.01f ? "Walk" : "Idle"
            );
        }
    }

    void Patrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            agent.isStopped = true;
            PlayAnimation("Idle");
            return;
        }

        // Hopp over tomme felt i listen.
        int checkedPoints = 0;

        while (patrolPoints[patrolIndex] == null)
        {
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
            checkedPoints++;

            if (checkedPoints >= patrolPoints.Length)
            {
                agent.isStopped = true;
                PlayAnimation("Idle");
                return;
            }
        }

        agent.speed = patrolSpeed;
        agent.stoppingDistance = patrolArrivalDistance;
        agent.updateRotation = true;

        if (patrolWaitTimer > 0f)
        {
            patrolWaitTimer -= Time.deltaTime;
            agent.isStopped = true;
            PlayAnimation("Idle");
            return;
        }

        Vector3 direction =
            patrolPoints[patrolIndex].position - transform.position;

        float heightDifference = Mathf.Abs(direction.y);
        direction.y = 0f;

        // Punktet er nådd. Vent litt før neste punkt.
        if (direction.magnitude <= patrolArrivalDistance + 0.1f
            && heightDifference <= 1f)
        {
            agent.isStopped = true;
            agent.ResetPath();

            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
            patrolWaitTimer = patrolWaitTime;

            PlayAnimation("Idle");
            return;
        }

        agent.isStopped = false;
        agent.SetDestination(patrolPoints[patrolIndex].position);

        PlayAnimation(
            agent.velocity.sqrMagnitude > 0.01f ? "Walk" : "Idle"
        );
    }

    public void Die()
    {
        if (isDead)
            return;

        isDead = true;

        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;
            agent.updateRotation = false;
        }

        if (animator != null)
        {
            animator.CrossFadeInFixedTime("Base Layer.Death", 0.15f, 0);
            currentAnimation = "Death";
        }
    }

    void PlayAnimation(string state)
    {
        if (animator == null) return;

        string statePath = "Base Layer." + state;

        // Bytt animasjon når fienden endrer handling.
        if (currentAnimation != state)
        {
            animator.CrossFadeInFixedTime(statePath, 0.15f, 0);
            currentAnimation = state;
            return;
        }

        if (animator.IsInTransition(0)) return;

        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);

        // Gjenta ferdige klipp som ikke har Loop Time aktivert.
        if (info.IsName(statePath) && !info.loop && info.normalizedTime >= 1f)
        {
            animator.Play(statePath, 0, 0f);
        }
    }

    bool CanSeePlayer()
    {
        if (player == null) return false;

        Vector3 direction = player.position - transform.position;

        if (direction.magnitude > viewDistance)
            return false;

        direction.y = 0f;

        if (Vector3.Angle(transform.forward, direction) > viewAngle / 2f)
            return false;

        Vector3 eyes = transform.position + Vector3.up * eyeHeight;
        Collider playerCollider = player.GetComponent<Collider>();

        Vector3 target = playerCollider != null
            ? playerCollider.bounds.center
            : player.position;

        Vector3 sightLine = target - eyes;

        RaycastHit[] hits = Physics.RaycastAll(
            eyes,
            sightLine.normalized,
            sightLine.magnitude,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        foreach (RaycastHit hit in hits)
        {
            if (hit.transform.IsChildOf(transform))
                continue;

            if (hit.transform.IsChildOf(player))
                continue;

            return false;
        }

        return true;
    }
}