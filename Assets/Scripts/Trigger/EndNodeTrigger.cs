using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;

public class EndNodeTrigger : MonoBehaviour
{
    private PrimMazeGenerator mazeGenerator;
    private XROrigin xrOrigin;

    public void Initialize(PrimMazeGenerator generator, XROrigin origin)
    {
        mazeGenerator = generator;
        xrOrigin = origin;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the player (XR Origin) has stepped on the end node
        if (other.gameObject == xrOrigin.gameObject)
        {
            mazeGenerator.OnPlayerSteppedOnEndNode();
        }
    }
}
