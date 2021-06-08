using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IKAnimKyn : MonoBehaviour
{
    public float distanceToGround;

    public Animator anim;

    private void Start()
    {
        anim = GetComponent<Animator>();
    }

    private void OnAnimatorIK(int layerIndex)
    {
        Debug.Log("yay");
        anim.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1f);
        anim.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 1f);

        // LEft Foot
        RaycastHit hit;
        Ray ray = new Ray(anim.GetIKPosition(AvatarIKGoal.LeftFoot) + Vector3.up, Vector3.down);
        Debug.DrawRay(ray.origin, ray.direction * (distanceToGround + 1f), Color.green);
        if (Physics.Raycast(ray, out hit, distanceToGround + 1f, Utils.layer_Terrain.ToLayerMask()))
        {
            Vector3 footPosition = hit.point;
            footPosition.y += distanceToGround;
            anim.SetIKPosition(AvatarIKGoal.LeftFoot, footPosition);
            Debug.Log("bub");
        }
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawSphere(anim.GetIKPosition(AvatarIKGoal.LeftFoot), 0.2f);
        Debug.Log(anim.GetIKPosition(AvatarIKGoal.LeftFoot));
    }
}
