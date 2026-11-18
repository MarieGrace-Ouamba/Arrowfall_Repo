// BossStateMachine.cs
// The boss's behavior written with the state pattern from class. All the classes below
// belong in this one file:
//   IEnemyState         the interface every state follows
//   BossStateMachine   the component that goes on the boss; it holds the current state and runs it
//   IdleState / ChaseState / AttackState   one class per behavior
// Same structure as the class example, plus what the boss needs: a NavMeshAgent to walk
// around walls, an Animator, and the attack numbers.
// Replaces BossChase and BossAttack (remove those from the boss before adding this one).

using UnityEngine;
using UnityEngine.AI;

public interface IEnemyState
{
    void UpdateState(BossStateMachine enemy);
}

[RequireComponent(typeof(NavMeshAgent))]
public class BossStateMachine : MonoBehaviour
{
    // ----- Same as the class example -----
    public Transform player;                // leave empty and the boss finds the object tagged Player
    public float speed = 3.5f;
    public float chaseDistance = 40f;       // the class example uses 8
    public float attackDistance = 3f;       // the class example uses 2

    // ----- Added for the boss -----
    public float attackExitBuffer = 0.5f;   // stops him flickering between Chase and Attack at the edge
    public Animator animator;               // leave empty and the boss finds the Animator in his children

    [Range(0f, 1f)]
    public float damagePercent = 0.4f;      // 0.4 = 40% of the player's max health
    public float windUp = 0.5f;             // delay before the first hit after he starts attacking
    public float attackCooldown = 1.5f;     // time between hits

    [HideInInspector] public NavMeshAgent agent;
    [HideInInspector] public PlayerHealth playerHealth;

    private IEnemyState currentState;
    private string currentAnimation = "";

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = speed;
        agent.stoppingDistance = attackDistance * 0.8f;

        if (player == null)
        {
            GameObject playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
        }

        if (player != null)
            playerHealth = player.GetComponent<PlayerHealth>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator != null)
            animator.applyRootMotion = false;

        currentState = new IdleState();
    }

    void Update()
    {
        // The agent must be standing on the baked NavMesh before it can take commands.
        if (currentState == null || player == null || !agent.isOnNavMesh) return;
        currentState.UpdateState(this);
    }

    public void ChangeState(IEnemyState newState)
    {
        currentState = newState;
        Debug.Log("New state: " + currentState.GetType().Name);
    }

    // Straight-line distance to the player, ignoring height.
    public float DistanceToPlayer()
    {
        Vector3 difference = player.position - transform.position;
        difference.y = 0f;
        return difference.magnitude;
    }

    public void FacePlayer()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                Quaternion.LookRotation(direction),
                360f * Time.deltaTime);
        }
    }

    // Starts the named state in the Animator's Base Layer (Idle, Walk or Attack).
    // It only switches when the name changes, so calling it every frame is safe.
    public void PlayAnimation(string state)
    {
        if (animator == null || currentAnimation == state) return;

        currentAnimation = state;
        animator.CrossFadeInFixedTime("Base Layer." + state, 0.15f, 0);
    }
}

// Idle: stand still and wait for the player to get close.
public class IdleState : IEnemyState
{
    public void UpdateState(BossStateMachine enemy)
    {
        // Idle behavior: stand still.
        enemy.agent.isStopped = true;
        enemy.agent.updateRotation = true;
        enemy.PlayAnimation("Idle");

        float distance = enemy.DistanceToPlayer();

        if (distance <= enemy.attackDistance)
            enemy.ChangeState(new AttackState());
        else if (distance <= enemy.chaseDistance)
            enemy.ChangeState(new ChaseState());
        // Otherwise, remain idle.
    }
}

// Chase: walk toward the player along the NavMesh.
public class ChaseState : IEnemyState
{
    public void UpdateState(BossStateMachine enemy)
    {
        float distance = enemy.DistanceToPlayer();

        if (distance <= enemy.attackDistance)
            enemy.ChangeState(new AttackState());
        else if (distance > enemy.chaseDistance)
            enemy.ChangeState(new IdleState());
        else
        {
            // The NavMeshAgent walks around walls and obstacles. The class example moves the
            // transform with MoveTowards, which would let the boss walk straight through them.
            enemy.agent.isStopped = false;
            enemy.agent.updateRotation = true;
            enemy.PlayAnimation("Walk");
            enemy.agent.SetDestination(enemy.player.position);
        }
    }
}

// Attack: face the player and hit him on a timer.
public class AttackState : IEnemyState
{
    private float timer;
    private bool started = false;   // lets the wind-up timer start on the first frame of this state

    public void UpdateState(BossStateMachine enemy)
    {
        float distance = enemy.DistanceToPlayer();

        if (distance > enemy.chaseDistance)
            enemy.ChangeState(new IdleState());
        else if (distance > enemy.attackDistance + enemy.attackExitBuffer)
            enemy.ChangeState(new ChaseState());
        else
        {
            // Attack behavior.
            if (!started)
            {
                started = true;
                timer = enemy.windUp;
            }

            enemy.agent.isStopped = true;
            enemy.agent.updateRotation = false;
            enemy.FacePlayer();
            enemy.PlayAnimation("Attack");

            timer -= Time.deltaTime;

            if (timer <= 0f && enemy.playerHealth != null)
            {
                int damage = Mathf.RoundToInt(enemy.playerHealth.maxHealth * enemy.damagePercent);
                enemy.playerHealth.TakeDamage(damage);
                timer = enemy.attackCooldown;
            }
        }
    }
}