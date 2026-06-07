using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    Wyzard followTarget;

    // Update is called once per frame
    void Update()
    {
        if ((followTarget == null) || (!followTarget.isLocalPlayer))
        {
            var wyzards = FindObjectsByType<Wyzard>(FindObjectsSortMode.None);
            if (wyzards.Length > 0)
            {
                foreach (var wyzard in wyzards)
                {
                    if (wyzard.isLocalPlayer)
                    {
                        followTarget = wyzard;
                        break;
                    }
                }
                if (followTarget == null) followTarget = wyzards[0];
            }
        }

        if (followTarget != null)
        {
            var target = followTarget.transform.position;
            target.z = transform.position.z;

            transform.position = transform.position + (target - transform.position) * 0.05f;
        }
    }
}
