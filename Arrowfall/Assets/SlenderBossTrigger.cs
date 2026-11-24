using UnityEngine;

[RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
public class SlenderBossTrigger : MonoBehaviour
{
    public SlenderBoss boss;

    private bool activated;

    void Awake()
    {
        GetComponent<BoxCollider>().isTrigger = true;

        Rigidbody body = GetComponent<Rigidbody>();
        body.useGravity = false;
        body.isKinematic = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (activated || boss == null || boss.player == null)
            return;

        PlayerHealth enteringPlayer =
            other.GetComponentInParent<PlayerHealth>();

        if (enteringPlayer != boss.player)
            return;

        activated = true;
        boss.ActivateBoss();

        GetComponent<BoxCollider>().enabled = false;
    }
}