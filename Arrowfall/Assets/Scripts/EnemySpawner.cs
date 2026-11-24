using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawning")]
    public GameObject enemyPrefab;
    public Transform[] spawnPoints;
    public float firstSpawnDelay = 3f;
    public float spawnInterval = 8f;
    public int maxAlive = 2;

    [Header("Spawned Guards")]
    public bool huntPlayer = true;
    public bool removeBodies = true;
    public float bodyRemoveDelay = 5f;

    [Header("Testing")]
    public bool debugLogs = true;

    private List<EnemyHealth> alive = new List<EnemyHealth>();
    private bool spawning = false;
    private Coroutine spawnRoutine;
    private Transform player;

    void Start()
    {
        PlayerMovement playerMovement = FindFirstObjectByType<PlayerMovement>();
        if (playerMovement != null)
        {
            player = playerMovement.transform;
        }
    }

    public void StartSpawning()
    {
        if (spawning)
        {
            return;
        }

        spawning = true;
        spawnRoutine = StartCoroutine(SpawnLoop());
  
    }

    public void StopSpawning()
    {
        spawning = false;

        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
        }

  
    }

    IEnumerator SpawnLoop()
    {
        yield return new WaitForSeconds(firstSpawnDelay);

        while (spawning)
        {
            alive.RemoveAll(guard => guard == null || guard.IsDead());

            if (alive.Count < maxAlive)
            {
                SpawnOne();
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnOne()
    {
        if (enemyPrefab == null || spawnPoints == null || spawnPoints.Length == 0)
        {
            
            return;
        }

        Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];

        if (!NavMesh.SamplePosition(point.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
        {
            return;
        }

        Quaternion facing = point.rotation;
        if (player != null)
        {
            Vector3 toPlayer = player.position - hit.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > 0.01f)
            {
                facing = Quaternion.LookRotation(toPlayer);
            }
        }

        GameObject guard = Instantiate(enemyPrefab, hit.position, facing);

        if (huntPlayer)
        {
            EnemyChase chase = guard.GetComponent<EnemyChase>();
            if (chase != null)
            {
                chase.viewDistance = 500f;
                chase.viewAngle = 360f;
            }
        }

        EnemyHealth health = guard.GetComponent<EnemyHealth>();
        if (health != null)
        {
            if (removeBodies)
            {
                health.removeBody = true;
                health.removeDelay = bodyRemoveDelay;
            }

            alive.Add(health);
        }

        Log("Spawned a guard at " + point.name + ". Spawned guards alive: " + alive.Count);
    }

    void Log(string message)
    {
        if (debugLogs)
        {
            Debug.Log("[EnemySpawner] " + message);
        }
    }
}