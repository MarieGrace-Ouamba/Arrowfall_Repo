using UnityEngine;

public class KeyPickup : MonoBehaviour
{
    [Range(1, 3)]
    public int keyNumber = 1;

    public static bool hasKey1;
    public static bool hasKey2;
    public static bool hasKey3;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetKeys()
    {
        hasKey1 = false;
        hasKey2 = false;
        hasKey3 = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        switch (keyNumber)
        {
            case 1:
                hasKey1 = true;
                break;
            case 2:
                hasKey2 = true;
                break;
            case 3:
                hasKey3 = true;
                break;
            default:
                return;
        }

        Debug.Log("Key " + keyNumber + " collected!");
        gameObject.SetActive(false);
    }
}