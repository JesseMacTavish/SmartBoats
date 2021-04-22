using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PullObject : MonoBehaviour
{
    private GameObject targetPull;
    private float pullSpeed;

    void Update()
    {
        //Remove pull if the target is gone
        if (targetPull == null)
        {
            Destroy(this);
            return;
        }

        float step = pullSpeed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, targetPull.transform.position, step);
    }

    public void SetTargetAndSpeed(GameObject pTargetPull, float pPullSpeed)
    {
        targetPull = pTargetPull;
        pullSpeed = pPullSpeed;
    }
}
