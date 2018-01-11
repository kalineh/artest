using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Balloon
    : MonoBehaviour
{
    private Rigidbody body;
    private SpringJoint jointOnTarget;

    private void Start()
    {
        body = GetComponent<Rigidbody>();

        var ray = new Ray(transform.position + Vector3.up * 0.1f, Vector3.down);
        var mask = LayerMask.GetMask("TrackableAttachedObject");
        var range = 0.35f;
        var hits = Physics.RaycastAll(ray, range, mask);

        Debug.DrawLine(ray.origin, transform.position + Vector3.down * range, Color.blue, 5.0f, false);

        foreach (var info in hits)
        {
            if (info.collider.gameObject == gameObject)
                continue;

            var hitBody = info.collider.gameObject.GetComponent<Rigidbody>();
            if (hitBody != null)
            {
                jointOnTarget = info.collider.gameObject.AddComponent<SpringJoint>();
                jointOnTarget.connectedBody = body;
                jointOnTarget.spring = 25.0f;
                jointOnTarget.maxDistance = 0.1f;

                hitBody.useGravity = false;
            }

            break;
        }
    }

    void Update()
    {
        var weight = jointOnTarget != null ? jointOnTarget.connectedBody.mass : 0.0f;
        var force = Mathf.Max(5.0f - weight, 0.1f);

        body.AddForce(Vector3.up * force * Time.deltaTime, ForceMode.Acceleration);
	}
}
