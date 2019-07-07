using UnityEngine;

public class BoneGizmo : MonoBehaviour
{
    public void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        drawGizzy();
    }

    public void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        drawGizzy();
    }

    void drawGizzy()
    {
        float size = 1.0f;
        float hintSize = 0.2f;
        Gizmos.DrawSphere(transform.position, size);
        Gizmos.DrawLine(transform.position, transform.parent.position);

        Vector3 delta = transform.parent.position - transform.position;
        Vector3 hintDelta = delta.normalized * (delta.magnitude - size - hintSize);
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position + hintDelta, hintSize);
    }
}
