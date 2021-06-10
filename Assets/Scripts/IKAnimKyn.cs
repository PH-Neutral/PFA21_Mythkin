using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IKAnimKyn : MonoBehaviour
{
    public float distanceToGround;

    public Animator anim;

    private void Start()
    {
        anim = GetComponentInChildren<Animator>();
    }

    void SetFoot(AvatarIKGoal ik) {
        anim.SetIKPositionWeight(ik, 1f);
        anim.SetIKRotationWeight(ik, 1f);

        RaycastHit hit;
        Ray ray = new Ray(anim.GetIKPosition(ik) + Vector3.up, Vector3.down);
        Debug.DrawRay(ray.origin, ray.direction * (distanceToGround + 1f), Color.green);
        if(Physics.Raycast(ray, out hit, distanceToGround + 1f, Utils.l_Terrain.ToLayerMask())) {
            Vector3 footPosition = hit.point;
            footPosition.y += distanceToGround;
            anim.SetIKPosition(ik, footPosition);
            //Debug.Log("bub");
        }
    }

    private void OnAnimatorIK(int layerIndex)
    {
        SetFoot(AvatarIKGoal.LeftFoot);
        SetFoot(AvatarIKGoal.RightFoot);
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawSphere(anim.GetIKPosition(AvatarIKGoal.LeftFoot), 0.2f);
        Debug.Log(anim.GetIKPosition(AvatarIKGoal.LeftFoot));
    }
}
