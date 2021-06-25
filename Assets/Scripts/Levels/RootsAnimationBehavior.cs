using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RootsAnimationBehavior : StateMachineBehaviour
{
    bool raiseFlag = false;
    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if(raiseFlag && animator.GetFloat("OpenRatio") < 0 && stateInfo.normalizedTime < 0) {
            animator.SetBool("FlagClosed", true);
            raiseFlag = false;
        }
        if(!raiseFlag && animator.GetFloat("OpenRatio") > 0) {
            raiseFlag = true;
        }
        /*if (stateInfo.normalizedTime <= 0 || stateInfo.normalizedTime >= 1 && animator.GetBool("SpeedSetToZero"))
        {
            animator.speed = 0;
            animator.SetBool("SpeedSetToZero", true);
        }
        Debug.Log(animator.speed);*/
    }

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    //override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

    // OnStateMove is called right after Animator.OnAnimatorMove()
    //override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that processes and affects root motion
    //}

    // OnStateIK is called right after Animator.OnAnimatorIK()
    //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that sets up animation IK (inverse kinematics)
    //}
}
