// BossChase.cs
// Boss copy of EnemyChase (original by Rayan Alduhaiman, IT 485/585 - ArrowFall).
// The boss looks for the player, walks toward them, and switches between the
// Idle, Walk and Attack states in his Animator.

using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class BossChase : MonoBehaviour
{
    public Transform player;           // leave empty and the boss finds the object tagged Player
    public Animator animator;

    public float viewDistance = 40f;   // the guard default is 15

    [Range(0f, 360f)]
    public float viewAngle = 360f;     // 360 means he can see you from any direction

    public float eyeHeight = 2.5f;     // higher than a normal guard because he is bigger
    public float attackDistance = 3f;
    public float chaseSpeed = 3.5f;

    [Header("Debug")]
    public bool debugLogs = true;      // untick to stop the Console messages

    private NavMeshAgent agent;
    private string currentAnimation;
    private string seeReason = "";     // why CanSeePlayer said yes or no (shown in the debug line)

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = chaseSpeed;
        agent.stoppingDistance = attackDistance * 0.8f;

        if (player == null)
        {
            GameObject playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
        }

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator != null)
            animator.applyRootMotion = false;

        PlayAnimation("Idle");
    }

    void Update()
    {
        bool sees = CanSeePlayer();

        if (debugLogs)
        {
            Debug.Log("Boss [" + name + "] NavMesh: " + agent.isOnNavMesh +
                      " | Player set: " + (player != null) +
                      " | Sees: " + sees + " (" + seeReason + ")");
        }

        if (!agent.isOnNavMesh)
        {
            PlayAnimation("Idle");
            return;
        }

        if (!sees)
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

        // Switch animation when the boss changes what he is doing.
        if (currentAnimation != state)
        {
            animator.CrossFadeInFixedTime(statePath, 0.15f, 0);
            currentAnimation = state;
            return;
        }

        if (animator.IsInTransition(0)) return;

        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);

        // Replay finished clips that do not have Loop Time turned on.
        if (info.IsName(statePath) && !info.loop && info.normalizedTime >= 1f)
        {
            animator.Play(statePath, 0, 0f);
        }
    }

    bool CanSeePlayer()
    {
        if (player == null)
        {
            seeReason = "no player assigned";
            return false;
        }

        Vector3 direction = player.position - transform.position;

        if (direction.magnitude > viewDistance)
        {
            seeReason = "too far: " + direction.magnitude.ToString("F1") + " > " + viewDistance;
            return false;
        }

        direction.y = 0f;

        if (Vector3.Angle(transform.forward, direction) > viewAngle / 2f)
        {
            seeReason = "outside view angle";
            return false;
        }

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

            seeReason = "blocked by " + hit.collider.name + " (in " + hit.transform.root.name + ")";
            return false;
        }

        seeReason = "clear";
        return true;
    }
}