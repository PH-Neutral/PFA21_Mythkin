using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombTrajectory : MonoBehaviour
{
    [SerializeField] float maxLenght = 5f, precision = 0.2f, lastPointDisctance = 0.1f;
    [SerializeField] LineRenderer trajectoryLine;
    float g = Physics.gravity.magnitude;

    public void ShowBombTrajectory(Vector3 throwVector)
    {
        List<Vector3> trajectoryPoints = new List<Vector3>();
        Vector3 projectilePosition;

        for (float i = 0f; i < maxLenght; i += precision)
        {
            projectilePosition = transform.position + Camera.main.transform.TransformDirection(ProjectilePosition(throwVector, i));
            if (Physics.Raycast(projectilePosition, Vector3.down, lastPointDisctance)) break;

            trajectoryPoints.Add(projectilePosition);
        }
        trajectoryLine.positionCount = trajectoryPoints.Count;
        trajectoryLine.SetPositions(trajectoryPoints.ToArray());
    }
    Vector3 ProjectilePosition(Vector3 throwVector, float t)
    {
        Debug.DrawRay(transform.position, throwVector, Color.blue);
        return new Vector3(0,
                            throwVector.magnitude * t * Mathf.Sin(Vector3.Angle(Vector3.forward, throwVector) * Mathf.Deg2Rad) - 0.5f * g * Mathf.Pow(t, 2),
                            throwVector.magnitude * t * Mathf.Cos(Vector3.Angle(Vector3.forward, throwVector) * Mathf.Deg2Rad));
    }
}
