using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AshkynCore.Audio;

public class TrajectoryHandler : MonoBehaviour {
    public bool IsDisplaying {
        get {
            return _lr.enabled;
        }
        set {
            if(_lr.enabled == value) return;
            _lr.enabled = value;
        }
    }
    [HideInInspector] public PlayerCamera playerCamera;

    [SerializeField] float _lengthDisplayed = 5f, _precision = 0.05f, _throwForce, _throwAngleOffset;
    [SerializeField] GameObject _bombPrefab;
    [SerializeField] bool showDebug = true;
    LineRenderer _lr;
    Vector3 _throwVector;
    float _bombRadius;

    private void Awake() {
        _lr = GetComponent<LineRenderer>();
        _bombRadius = _bombPrefab.GetComponent<SphereCollider>().radius;
    }
    public void SetBombTrajectory(bool displayTrajectory = true)
    {
        List<Vector3> trajectoryPoints = new List<Vector3>();
        Vector3 projectilePosition, rayOrigin, rayDir;
        RaycastHit hit;

        _throwVector = FindThrowVector();
        if(showDebug) Debug.DrawRay(transform.position, transform.TransformDirection(_throwVector), Color.blue);

        for (float t = 0f, i = 0; t < _lengthDisplayed; t += _precision, i++)
        {
            projectilePosition = ProjectilePosition(_throwVector, t);

            if (i > 0)
            {
                rayOrigin = transform.TransformPoint(trajectoryPoints[(int)i - 1]);
                rayDir = transform.TransformDirection(projectilePosition - trajectoryPoints[(int)i - 1]);
                if(Physics.SphereCast(rayOrigin, _bombRadius, rayDir, out hit, rayDir.magnitude, Utils.l_Terrain.ToLayerMask() | Utils.l_Interactibles.ToLayerMask()))
                {
                    trajectoryPoints.Add(transform.InverseTransformPoint(hit.point + hit.normal * _bombRadius));
                    break;
                }
            }

            trajectoryPoints.Add(projectilePosition);
        }
        if(!displayTrajectory || !IsDisplaying) return;
        _lr.positionCount = trajectoryPoints.Count;
        _lr.SetPositions(trajectoryPoints.ToArray());
    }
    Vector3 ProjectilePosition(Vector3 throwVector, float t)
    {
        float angle = Vector3.SignedAngle(Vector3.forward, throwVector, -Vector3.right) * Mathf.Deg2Rad;
        return new Vector3(
            0,
            throwVector.magnitude * t * Mathf.Sin(angle) - 0.5f * Physics.gravity.magnitude * Mathf.Pow(t, 2),
            throwVector.magnitude * t * Mathf.Cos(angle)
            );
    }
    public void ThrowBomb()
    {
        AudioManager.instance.PlaySound(AudioTag.fruitBombThrow, gameObject);
        GameObject bombInstance = Instantiate(_bombPrefab, transform.position, transform.rotation);
        bombInstance.GetComponent<Rigidbody>().velocity = transform.TransformDirection(_throwVector);
    }
    Vector3 FindThrowVector()
    {
        return  playerCamera.xRotation * Quaternion.Euler(-_throwAngleOffset, 0, 0) * Vector3.forward * _throwForce;
    }
}
