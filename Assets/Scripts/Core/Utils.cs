using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Utils {
    public static float floorHeight = 2;

    #region MISC
    public static float ChangePrecision(this float f, int nbDecimals) {
        return ((int)(f * Mathf.Pow(10, nbDecimals))) / Mathf.Pow(10, nbDecimals);
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
    public static float CalculateSoundLevel(float baseLevel, float distance) {
        float level = baseLevel - levelStep * Mathf.Log(Mathf.Pow(distance / distanceUnit, 2), 2);
        return level < levelCutoff ? 0 : level;
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