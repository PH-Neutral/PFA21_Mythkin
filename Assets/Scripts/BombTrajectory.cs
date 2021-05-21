using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombTrajectory : MonoBehaviour
{
    [SerializeField] float maxLenght = 5f, precision = 0.2f, _throwForce, _throwAngleOffset;
    [SerializeField] LineRenderer trajectoryLine;
    [SerializeField] GameObject _bombPrefab;
    [SerializeField] PlayerCamera _playerCamera;

    public bool IsDisplaying
    {
        get
        {
            return trajectoryLine.enabled;
        }
        set
        {
            trajectoryLine.enabled = value;
        }
    }
    Vector3 throwVector;

    public void SetBombTrajectory()
    {
        List<Vector3> trajectoryPoints = new List<Vector3>();
        Vector3 projectilePosition, rayOrigin, rayDir;
        RaycastHit hit;

        throwVector = FindThrowVector();

        for (float t = 0f, i = 0; t < maxLenght; t += precision, i++)
        {
            projectilePosition = ProjectilePosition(throwVector, t);

            if (i > 0)
            {
                rayOrigin = transform.TransformPoint(trajectoryPoints[(int)i - 1]);
                rayDir = transform.TransformDirection(projectilePosition - trajectoryPoints[(int)i - 1]);
                if (Physics.Raycast(rayOrigin, rayDir, out hit, rayDir.magnitude, 1<<LayerMask.NameToLayer("Terrain") | 1<<LayerMask.NameToLayer("Interactibles")))
                {
                    trajectoryPoints.Add(transform.InverseTransformPoint(hit.point));
                    break;
                }
            }

            trajectoryPoints.Add(projectilePosition);
        }
        trajectoryLine.positionCount = trajectoryPoints.Count;
        trajectoryLine.SetPositions(trajectoryPoints.ToArray());
    }
    Vector3 ProjectilePosition(Vector3 throwVector, float t)
    {
        Debug.DrawRay(transform.position, throwVector, Color.blue);
        return new Vector3(0,
                            throwVector.magnitude * t * Mathf.Sin(Vector3.Angle(Vector3.forward, throwVector) * Mathf.Deg2Rad) - 0.5f * Utils.gravity.magnitude * Mathf.Pow(t, 2),
                            throwVector.magnitude * t * Mathf.Cos(Vector3.Angle(Vector3.forward, throwVector) * Mathf.Deg2Rad));
    }
    public void ThrowBomb()
    {
        GameObject bombInstance = Instantiate(_bombPrefab, transform.position, transform.rotation);
        bombInstance.GetComponent<Rigidbody>().velocity = transform.TransformDirection(throwVector);
    }
    Vector3 FindThrowVector()
    {
        return  _playerCamera.xRotation * Quaternion.Euler(-_throwAngleOffset, 0, 0) * Vector3.forward * _throwForce;
    }
}
