// Crosshair.cs
// Rayan Alduhaiman
// IT 485/585 - ArrowFall
// A dot in the middle of the screen showing where arrows will land.
// It stays faint while the bow is at rest and becomes solid as the draw reaches full power.

using UnityEngine;
using UnityEngine.UI;

public class Crosshair : MonoBehaviour
{
    [Header("References")]
    public BowShoot bow;                    // drag Main Camera here (it has BowShoot)
    public Image dot;                       // drag the crosshair Image here

    [Header("Look")]
    public bool alwaysVisible = true;       // untick to show the dot only while drawing
    [Range(0f, 1f)]
    public float restAlpha = 0.5f;          // how visible the dot is when not drawing
    [Range(0f, 1f)]
    public float drawStartAlpha = 0.7f;     // how visible it is at the start of a draw; full draw is solid

    void Update()
    {
        if (bow == null || dot == null)
        {
            return;
        }

        // 0 when not drawing, up to 1 at a full draw.
        float charge = bow.GetCharge();
        bool drawing = charge > 0f && bow.enabled;

        dot.enabled = drawing || alwaysVisible;

        Color color = dot.color;
        color.a = drawing ? Mathf.Lerp(drawStartAlpha, 1f, charge) : restAlpha;
        dot.color = color;
    }
}
