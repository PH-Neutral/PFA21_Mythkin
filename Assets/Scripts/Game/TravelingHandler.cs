using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class TravelingHandler : MonoBehaviour {
    [SerializeField] CinemachineVirtualCamera cam;
    [SerializeField] SplineWalker walkerFollow, walkerLookAt;
    [Range(0,120)][SerializeField] float duration = 10;
    [SerializeField] float lookAtLaunchDelay = 1;
    
    Action _onTravelingEnds;
    bool canBeSkiped = false;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && canBeSkiped)
        {
            StopTraveling();
        }
    }

    public void StartTraveling(Action onTravelingEnds) {
        _onTravelingEnds = onTravelingEnds;

        // setup the cam + spline walkers
        walkerFollow.duration = duration;
        walkerLookAt.duration = duration;

        // launch the walkers with callback parameter
        cam.Priority = 2;
        walkerFollow.BeginMoving();
        StartCoroutine(LaunchWalker(walkerLookAt, lookAtLaunchDelay));
    }
    IEnumerator LaunchWalker(SplineWalker walker, float delay) {
        float timer = 0;
        while(timer < delay) {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }
        canBeSkiped = true;
        walkerLookAt.BeginMoving(() => { OnTravelingEnds(); });
        yield break;
    }
    void OnTravelingEnds() {
        cam.Priority = 0;
        _onTravelingEnds?.Invoke();
    }
    void StopTraveling()
    {
        canBeSkiped = false;
        StopAllCoroutines();
        walkerLookAt.StopMoving();
        walkerFollow.StopMoving();
        UIManager.Instance.SwitchCameraMode(true);
        OnTravelingEnds();
    }
}