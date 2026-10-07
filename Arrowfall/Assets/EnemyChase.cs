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

    private NavMeshAgent agent;
    private string currentAnimation;

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
        if (!agent.isOnNavMesh)
        {
            PlayAnimation("Idle");
            return;
        }

        if (!CanSeePlayer())
        {
            agent.isStopped = true;
            PlayAnimation("Idle");
            return;
        }

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