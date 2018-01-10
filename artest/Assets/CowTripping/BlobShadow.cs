using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlobShadow
    : MonoBehaviour
{
    private Vector3 originalScale;

    public void Start()
    {
        originalScale = transform.localScale;
    }

    public void Update()
    {
        var mask = LayerMask.GetMask("TrackablePlane", "TrackablePlaneEmulated");
        var ray = new Ray(transform.parent.position, Vector3.down);
        var range = 0.25f;
        var info = new RaycastHit();
        var hit = Physics.Raycast(ray, out info, range, mask);
        var scale = 0.0f;

        //Debug.DrawLine(transform.parent.position, transform.parent.position + Vector3.down * range, hit ? Color.red : Color.blue, 0.1f, false);

        if (hit)
        {
            var ofs = info.point - transform.parent.position;
            var len = ofs.magnitude;
            if (float.IsNaN(len))
                len = 0.0f;

            scale = range - Mathf.Clamp01(len / range);

            transform.position = info.point;
        }

        transform.localScale = originalScale * scale;
    }
}
