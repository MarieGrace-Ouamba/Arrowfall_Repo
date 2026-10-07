using UnityEngine;

public class GateKey : MonoBehaviour
{
    public KeyCode openKey = KeyCode.G;
    Animator anim;

    void Start()
    {
        anim = GetComponent<Animator>();
        anim.enabled = false;   // gate stays closed until you press the key
    }

    void Update()
    {
        if (Input.GetKeyDown(openKey))
        {
            anim.enabled = true;   // plays the GateOpen animation
        }
    }
}