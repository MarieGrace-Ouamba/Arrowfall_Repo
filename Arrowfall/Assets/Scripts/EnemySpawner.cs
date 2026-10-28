// EnemySpawner.cs
// Rayan Alduhaiman
// IT 485/585 - ArrowFall
// Keeps sending guards at the player while the gate puzzle runs.
// NumberPuzzle calls StartSpawning() when the rounds begin and StopSpawning() when the gate opens.
// Never more than maxAlive spawned guards at once, so the puzzle stays possible.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawning")]
    public GameObject enemyPrefab;           // the Enemy prefab from Assets/Prefabs
    public Transform[] spawnPoints;          // empty objects where guards appear, on the NavMesh
    public float firstSpawnDelay = 3f;       // seconds before the first guard appears
    public float spawnInterval = 8f;         // seconds between guards
    public int maxAlive = 2;                 // never more than this many spawned guards at once

    [Header("Spawned Guards")]
    public bool huntPlayer = true;           // spawned guards see the player from anywhere, so they always come
    public bool removeBodies = true;         // dead spawned guards disappear after a while
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
        Log("Spawning started.");
    }

    public void StopSpawning()
    {
        spawning = false;

        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
        }

        Log("Spawning stopped.");
    }

    IEnumerator SpawnLoop()
    {
        yield return new WaitForSeconds(firstSpawnDelay);

        while (spawning)
        {
            // Forget guards that died or were removed.
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
            Log("No enemy prefab or spawn points assigned.");
            return;
        }

        Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];

        // Snap to the nearest spot on the NavMesh, or the guard could not walk.
        if (!NavMesh.SamplePosition(point.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
        {
            Log("Spawn point " + point.name + " is not near the NavMesh. Move it onto the blue area.");
            return;
        }

        // Face the player, so the guard sees them straight away.
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
