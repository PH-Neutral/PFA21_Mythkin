using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Utils {
    public const string layer_Interactibles = "Interactibles", layer_Terrain = "Terrain", layer_Environment = "Environment", 
        layer_Enemies = "Enemies", layer_Player = "Player";
    public static float floorHeight = 6;

    #region MISC

    public static bool LinePlaneIntersection(out Vector3 intersection, Vector3 pointOnPlane, Vector3 planeNormal, Vector3 pointOnLine, Vector3 lineDirection) {
        intersection = pointOnPlane;
        float a, b, x;
        a = Vector3.Dot(pointOnPlane - pointOnLine, planeNormal);
        b = Vector3.Dot(lineDirection, planeNormal);
        bool isParallel = b == 0, isPointOnPlane = a == 0;
        if(isPointOnPlane) {
            intersection = pointOnLine;
            return true;
        } else if(!isParallel) {
            x = a / b;
            intersection = x * lineDirection + pointOnLine;
            return true;
        }
        return false;
    }
    public static Vector2 CartToPolar(Vector2 coord) {
        if(coord == Vector2.zero) return Vector2.zero;
        Vector2 vMax;
        if(Mathf.Abs(coord.x) > Mathf.Abs(coord.y)) {
            vMax.x = Mathf.Sign(coord.x);
            vMax.y = coord.y / Mathf.Abs(coord.x);
        } else {
            vMax.y = Mathf.Sign(coord.y);
            vMax.x = coord.x / Mathf.Abs(coord.y);
        }
        return coord / vMax.magnitude;
    }
    public static Vector3 Multiply(this Vector3 vector, Vector3 other) {
        return new Vector3(vector.x * other.x, vector.y * other.y, vector.z * other.z);
    }
    public static Vector3 Flatten(this Vector3 vector) {
        return new Vector3(vector.x, 0, vector.z);
    }
    public static float ChangePrecision(this float f, int nbDecimals) {
        return ((int)(f * Mathf.Pow(10, nbDecimals))) / Mathf.Pow(10, nbDecimals);
    }
    public static int ToLayerMask(this string layerName) {
        return 1<<LayerMask.NameToLayer(layerName);
    }
    public static float Sum(this float[] array)
    {
        float result = 0f;
        for (int i = 0; i < array.Length; i++)
        {
            result += array[i];
        }
        return result;
    }
    public static float Sign(this float f) {
        return f == 0 ? 0 : (f > 0 ? 1 : -1);
    }
    public static bool IsBetween(this float f, float min, bool minInclu, float max, bool maxInclu) {
        return (minInclu ? f >= min : f > min) && (maxInclu ? f <= max : f < max);
    }
    #endregion

    #region SPECIFICS
    public static Vector3[] directions = new Vector3[] {
        Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back
    };

    public static bool GetSurfacePoint(Vector3 worldPos, out Vector3 surfacePoint, float maxDistance = 10) {
        surfacePoint = worldPos;
        if(Physics.Raycast(worldPos + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, maxDistance + 0.5f, Utils.layer_Terrain.ToLayerMask())) {
            surfacePoint = hit.point;
            Vector3 raycast = hit.point - worldPos;
            if(raycast.magnitude > Utils.floorHeight) {
                return false;
            }
            return true;
        }
        return false;
    }
    public static bool LerpPosition(this Transform obj, Vector3 targetPos, float lerpSpeed) {
        float t = lerpSpeed * Time.deltaTime / Vector3.Distance(obj.position, targetPos);
        obj.position = Vector3.Lerp(obj.position, targetPos, t);
        return t >= 1;
    }
    public static bool SlerpRotation(this Transform obj, Vector3 newDirection, Vector3 upAxis, float rotateSpeed, Space space = Space.World) {
        if(Vector3.Angle(obj.forward, newDirection) == 0) return true;
        Quaternion newRotation = Quaternion.LookRotation(newDirection, upAxis);
        return obj.SlerpRotation(newRotation, rotateSpeed, space);
    }
    public static bool SlerpRotation(this Transform obj, Quaternion newRotation, float rotateSpeed, Space space = Space.World) {
        float angle = Quaternion.Angle(space == Space.World ? obj.rotation : obj.localRotation, newRotation);
        if(angle == 0) return true;
        float t = rotateSpeed * Time.deltaTime / angle;
        if(space == Space.World) obj.rotation = Quaternion.Slerp(obj.rotation, newRotation, t);
        else obj.localRotation = Quaternion.Slerp(obj.localRotation, newRotation, t);
        return t >= 1;
    }
    public static Vector3 GetVector(this TunnelEntrance.Direction dir) {
        return directions[(int)dir];
    }
    #endregion

    #region SOUND
    public static float distanceUnit = 0.5f; // in m
    public static float levelStep = 3; // in dB
    public static float levelCutoff = 0.0001f; // in dB
    public static float lowLevel = 0; // in dB (10: breathing, 20: rustling leaves, 30: whisper, etc)

    /// <summary>
    /// Calculate the sound level based on the distance from the source of the sound and its base level.
    /// </summary>
    /// <param name="baseLevel">The level of the sound at the source of the emission.</param>
    /// <param name="distance">The distance from the source of the sound.</param>
    /// <returns>The percieved level of the sound.</returns>
    public static float CalculateSoundLevel(float distanceMax, float distance) {
        /*
        float level = baseLevel - levelStep * Mathf.Log(Mathf.Pow(distance / distanceUnit, 2), 2);
        return level < levelCutoff ? 0 : level;
        */
        return (1 - distance / distanceMax) * 100;
    }
    public static void EmitSound(float soundRadius, Vector3 soundPosition, bool isPlayer = false)
    {
        Vector3 relativePos;
        float soundLevel;
        Ray ray;
        RaycastHit[] hits = Physics.SphereCastAll(soundPosition, soundRadius, Vector3.up, soundRadius * 2 + /*layerOffset = */ 5f, layer_Enemies.ToLayerMask());
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider.TryGetComponent(out Enemy enemy))
            {
                ray = new Ray(soundPosition, enemy.sightCenter.position - soundPosition);
                float dist = Vector3.Distance(enemy.sightCenter.position, soundPosition);
                //Debug.Log("yDiff: " + yDiff);
                if(Physics.Raycast(ray, dist, layer_Terrain.ToLayerMask())) continue;
                relativePos = soundPosition - enemy.transform.position;
                soundLevel = CalculateSoundLevel(soundRadius, relativePos.magnitude);
                if (Mathf.Abs(enemy.transform.position.y - soundPosition.y) < 3f /*layer thickness (put a real var later)*/)
                enemy.HearSound(relativePos, soundLevel, isPlayer);
            }
        }
    }
    /// <summary>
    /// Calculate the distance from the source of the sound based on its base and percieved level.
    /// </summary>
    /// <param name="baseLevel">The level of the sound at the source of the emission.</param>
    /// <param name="percievedLevel">The level of the sound at some distance of the source.</param>
    /// <returns>The distance from the source of the emission.</returns>
    public static float DistanceFromSoundSourceLevel(float baseLevel, float percievedLevel) {
        return distanceUnit * Mathf.Sqrt(Mathf.Pow(2, (baseLevel - percievedLevel) / levelStep));
    }
    public static float SoundSourceLevelFromDistance(float distance) => SoundSourceLevelFromDistance(distance, lowLevel);
    /// <summary>
    /// Calculate the sound level at the emission source based on the percieved level it should have at a specified distance.
    /// </summary>
    /// <param name="distance">The distance of the source of the emission.</param>
    /// <param name="levelAtRange">The level of the sound percievable at the specified distance of the source.</param>
    /// <returns>The sound level at the source of the emission.</returns>
    public static float SoundSourceLevelFromDistance(float distance, float levelAtRange) {
        return levelAtRange + levelStep * Mathf.Log(Mathf.Pow(distance / distanceUnit, 2), 2);
    }
    #endregion
}