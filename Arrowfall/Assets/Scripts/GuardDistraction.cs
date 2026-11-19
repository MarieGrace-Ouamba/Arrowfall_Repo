// GuardDistraction.cs
// Rayan Alduhaiman
// IT 485/585 - ArrowFall
// Controls the two guards outside the player's shed for the escape.
// While the lanterns are intact the guards watch the shed door (their own EnemyChase),
// so opening the door in front of them gets the player caught.
// When the player shoots a lantern, this script takes the guards over: it switches their
// EnemyChase off and walks them to the broken lantern with their NavMeshAgent, so they
// leave the door no matter what is in the way. It does not modify the shared EnemyChase
// script; it only disables that component on these two guards and drives the agent itself.

using UnityEngine;
using UnityEngine.AI;

public class GuardDistraction : MonoBehaviour
{
    [Header("Guards")]
    public EnemyChase[] guards;             // the two shed guards

    [Header("Lure")]
    public Transform distractTarget;        // optional fixed spot; leave empty to use the lantern that was shot
    public float arriveDistance = 2.5f;     // how close to the lantern the guards stop

    private bool distracted = false;
    private NavMeshAgent[] agents;
    private Animator[] animators;
    private string[] currentAnimation;

    public bool IsDistracted()
    {
        return distracted;
    }

    // Called by ShootableLantern when a lantern is shot.
    public void OnLanternBroken(Transform lantern)
    {
        if (distracted)
        {
            return;
        }
        distracted = true;

        Transform target = (distractTarget != null) ? distractTarget : lantern;

        // The lantern hangs in the air, so walk to the nearest ground point under it.
        Vector3 destination = target.position;
        if (NavMesh.SamplePosition(destination, out NavMeshHit hit, 8f, NavMesh.AllAreas))
        {
            destination = hit.position;
        }

        agents = new NavMeshAgent[guards.Length];
        animators = new Animator[guards.Length];
        currentAnimation = new string[guards.Length];

        for (int i = 0; i < guards.Length; i++)
        {
            EnemyChase guard = guards[i];
            if (guard == null)
            {
                continue;
            }

            // Stop the guard watching the door; from here on this script moves it.
            guard.enabled = false;

            animators[i] = guard.GetComponentInChildren<Animator>();
            agents[i] = guard.GetComponent<NavMeshAgent>();

            NavMeshAgent agent = agents[i];
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.updateRotation = true;
                agent.stoppingDistance = arriveDistance;
                agent.isStopped = false;
                agent.SetDestination(destination);
            }

            PlayAnimation(i, "Walk");
        }
    }

    void Update()
    {
        if (!distracted || agents == null)
        {
            return;
        }

        for (int i = 0; i < agents.Length; i++)
        {
            NavMeshAgent agent = agents[i];
            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            {
                continue;   // for example a guard that was killed
            }

            bool arrived = !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.2f;
            PlayAnimation(i, arrived ? "Idle" : "Walk");
        }
    }

    // Same idea as EnemyChase.PlayAnimation: switch state on change, and restart clips that do not loop.
    void PlayAnimation(int index, string state)
    {
        Animator animator = animators[index];
        if (animator == null)
        {
            return;
        }

        if (!animator.HasState(0, Animator.StringToHash(state)))
        {
            return;   // this guard model has no such state; keep whatever is playing
        }

        string statePath = "Base Layer." + state;

        if (currentAnimation[index] != state)
        {
            animator.CrossFadeInFixedTime(statePath, 0.15f, 0);
            currentAnimation[index] = state;
            return;
        }

        if (animator.IsInTransition(0))
        {
            return;
        }

        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
        if (info.IsName(statePath) && !info.loop && info.normalizedTime >= 1f)
        {
            animator.Play(statePath, 0, 0f);
        }
    }
}
