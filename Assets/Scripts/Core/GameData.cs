using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameData : MonoBehaviour
{
    public const string key_Audio = "Audio_", key_volumeSound = "volumeSound", key_volumeMusic = "volumeMusic";
    public const string key_Highscore = "Highscore_", key_collectiblesCount = "collectiblesCount", key_bestTime = "bestTime", key_invisible = "invisible";

    public static int maxCollectiblesCount = 0, invisible = 0;
    public static float volumeSound = 0.5f, volumeMusic = 0.5f, bestTime = -1;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
    public void SaveSettings()
    {
        SaveSetting(key_Audio + key_volumeMusic, volumeMusic); // setup music volume
        SaveSetting(key_Audio + key_volumeSound, volumeSound); // setup sound volume
        SaveSetting(key_Highscore + key_collectiblesCount, maxCollectiblesCount);
        SaveSetting(key_Highscore + key_bestTime, bestTime);
        SaveSetting(key_Highscore + key_invisible, invisible);
        PlayerPrefs.Save();
    }
    bool LoadSetting(string settingKey, ref float floatToLoad)
    {
        if (!PlayerPrefs.HasKey(settingKey)) return false;
        floatToLoad = PlayerPrefs.GetFloat(settingKey);
        return true;
    }
    bool LoadSetting(string settingKey, ref int intToLoad)
    {
        if (!PlayerPrefs.HasKey(settingKey)) return false;
        intToLoad = PlayerPrefs.GetInt(settingKey);
        return true;
    }
    void LoadSettings()
    {
        LoadSetting(key_Audio + key_volumeMusic, ref volumeMusic); // setup music volume
        LoadSetting(key_Audio + key_volumeSound, ref volumeSound); // setup sound volume
        LoadSetting(key_Highscore + key_collectiblesCount, ref maxCollectiblesCount);
        LoadSetting(key_Highscore + key_bestTime, ref bestTime);
        LoadSetting(key_Highscore + key_invisible, ref invisible);
    }

    void SaveSetting(string settingKey, float floatToSave)
    {
        PlayerPrefs.SetFloat(settingKey, floatToSave);
    }
    void SaveSetting(string settingKey, int intToSave)
    {
        PlayerPrefs.SetInt(settingKey, intToSave);
    }

    private void OnApplicationQuit()
    {
        SaveSettings();
    }
}
