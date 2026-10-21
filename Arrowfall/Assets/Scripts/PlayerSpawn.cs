// PlayerSpawn.cs
// Rayan Alduhaiman
// IT 485/585 - ArrowFall
// Moves the player to the level's spawn point when the level starts or restarts.
// The spawn point is any object tagged "Respawn", so each scene can have its own.

using UnityEngine;

public class PlayerSpawn : MonoBehaviour
{
    void Start()
    {
        GameObject spawnPoint = GameObject.FindWithTag("Respawn");
        if (spawnPoint == null)
        {
            Debug.LogWarning("No object tagged Respawn in this scene. The player starts where it was placed.");
            return;
        }

        // A CharacterController overrides position changes while it is on, so switch it off for the move.
        CharacterController controller = GetComponent<CharacterController>();
        if (controller != null)
        {
            controller.enabled = false;
        }

        // Face the same way as the spawn point, but stay upright.
        Quaternion facing = Quaternion.Euler(0f, spawnPoint.transform.eulerAngles.y, 0f);
        transform.SetPositionAndRotation(spawnPoint.transform.position, facing);

        if (controller != null)
        {
            controller.enabled = true;
        }
    }
}
