using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimationBehaviour : StateMachineBehaviour {
    const string animNameDead = "Dead";
    public float exitTransitionDuration = 0;

    float elapsedTime, remainingTime, layerWeight;
    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
        animator.SetLayerWeight(layerIndex, 1); 
        if(stateInfo.IsName(animNameDead)) OnPlayerDeathEnds();
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
        elapsedTime = stateInfo.normalizedTime * stateInfo.length;
        remainingTime = stateInfo.length - elapsedTime;
        //ebug.Log($"RemaingTime = {stateInfo.length} - {elapsedTime} = {remainingTime}");
        if(remainingTime < 0) {
            animator.SetLayerWeight(layerIndex, 0);
        } else if(remainingTime <= exitTransitionDuration) {
            layerWeight = exitTransitionDuration != 0 ? Mathf.Clamp01(remainingTime / exitTransitionDuration) : 0;
            animator.SetLayerWeight(layerIndex, layerWeight);
        }
    }

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
        animator.SetLayerWeight(layerIndex, 0);
    }

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

    public void OnPlayerDeathEnds() {
        GameManager.Instance.GameOver();
    }
}
