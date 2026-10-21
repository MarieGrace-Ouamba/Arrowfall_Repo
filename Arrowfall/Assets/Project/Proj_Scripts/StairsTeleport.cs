//Marie-Grace Ouamba

using UnityEngine;

public class StairsTeleport : MonoBehaviour
{
    
    public Transform player;
    public Transform destination;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            player.position = destination.position;

            if (cc != null) cc.enabled = true;
        }
    }
}
