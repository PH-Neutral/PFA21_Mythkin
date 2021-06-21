using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AshkynCore.Audio;

public class AudioManager : MonoBehaviour {
    
    public static AudioManager instance = null;

    [Range(0f,1f)] public float volumeMusic = 0.5f, volumeSound = 0.5f;

    [SerializeField] AudioAssetStorage audioAssets;
    Dictionary<AudioTag, AudioAsset> _assetDic;
    Dictionary<AudioKey, AudioSource> _sourceDic;
    Dictionary<AudioSource, AudioAsset> _sourceAssetDic;
    List<AudioSource> _musicSources, _soundSources;

    private void Awake() {
        if(instance == null) instance = this;
        else if(instance != this) Destroy(gameObject);
        DontDestroyOnLoad(this);

        
        _assetDic = audioAssets.ToDictionary(); // setup the tracks dictionary
        _sourceDic = new Dictionary<AudioKey, AudioSource>(); // setup the source dictionary
        _sourceAssetDic = new Dictionary<AudioSource, AudioAsset>(); // setup the link between source and asset

        _musicSources = new List<AudioSource>();
        _soundSources = new List<AudioSource>();

        // get the playerPrefs
        LoadSettings();
    }

    void LoadSettings() {
        volumeMusic = GameData.volumeMusic;
        volumeSound = GameData.volumeSound;
    }
    

    public void PlaySound(AudioTag tag, float volumeScale = 1) => PlaySound(tag, null, volumeScale);
    public void PlaySound(AudioTag tag, GameObject target, float volumeScale = 1) {
        AudioAsset asset = GetSoundAsset(tag);
        if(asset == null) return;
        AudioSource source = GetSource(tag, target);
        if(source == null) source = (target != null ? target : new GameObject()).AddComponent<AudioSource>();
        //source.clip = asset.clip;
        source.volume = volumeSound * asset.volume;
        source.spatialBlend = target != null ? 1 : 0; // [0: 2D] [1: 3D]
        source.PlayOneShot(asset.GetClip(), volumeScale);
        SetSource(tag, target, source, asset);
        _soundSources.AddUnique(source);
    }

    public void PlayMusic(AudioTag tag, bool loop = false) => PlayMusic(tag, null, loop);
    public void PlayMusic(AudioTag tag, GameObject target, bool loop = false) {
        AudioAsset asset = GetSoundAsset(tag);
        if(asset == null) return;
        AudioSource source = GetSource(tag, target);
        if(source == null) source = (target != null ? target : new GameObject()).AddComponent<AudioSource>();
        source.clip = asset.GetClip();
        source.loop = loop;
        source.volume = volumeMusic * asset.volume;
        source.spatialBlend = target != null ? 1 : 0; // [0: 2D] [1: 3D]
        source.Play();
        SetSource(tag, target, source, asset);
        _musicSources.AddUnique(source);
    }

    public void PauseAudio(AudioTag tag, GameObject target) => PauseAudio(new AudioKey(tag, target));
    public void PauseAudio(AudioKey key) {
        AudioSource source = GetSource(key);
        if(source == null) return;
        if(source.isPlaying) source.Pause();
        else source.UnPause();
    }

    public void StopAudio(AudioTag tag, GameObject target) => StopAudio(new AudioKey(tag, target));
    public void StopAudio(AudioKey key) {
        AudioSource source = GetSource(key);
        if(source == null) return;
        source.Stop();
    }

    public void ChangeVolumeMusic(float volume) {
        GameData.volumeMusic = volumeMusic = volume;
        AudioAsset asset;
        for(int i=0; i<_musicSources.Count; i++) {
            asset = GetAsset(_musicSources[i]);
            if(asset == null) continue;
            _musicSources[i].volume = volume * asset.volume;
        }
    }
    public void ChangeVolumeSound(float volume) {
        GameData.volumeSound = volumeSound = volume;
        AudioAsset asset;
        for(int i = 0; i < _soundSources.Count; i++) {
            asset = GetAsset(_soundSources[i]);
            if(asset == null) continue;
            _soundSources[i].volume = volume * asset.volume;
        }
    }

    bool IsPlaying(AudioKey key) {
        AudioSource source = GetSource(key);
        if(source == null) return false;
        return source.isPlaying;
    }

    AudioAsset GetSoundAsset(AudioTag tag) {
        if(_assetDic.ContainsKey(tag)) {
            return _assetDic[tag];
        }
        Debug.LogError($"The sound with tag \"{tag}\" was not found!");
        return null;
    }

    AudioSource GetSource(AudioTag tag, GameObject target) => GetSource(new AudioKey(tag, target));
    AudioSource GetSource(AudioKey key) {
        if(_sourceDic.ContainsKey(key)) {
            return _sourceDic[key];
        }
        return null;
    }

    void SetSource(AudioTag tag, GameObject target, AudioSource source, AudioAsset asset) => SetSource(new AudioKey(tag, target), source, asset);
    void SetSource(AudioKey key, AudioSource source, AudioAsset asset) {
        _sourceDic[key] = source;
        _sourceAssetDic[source] = asset;
    }

    AudioAsset GetAsset(AudioSource source) {
        if(_sourceAssetDic.TryGetValue(source, out AudioAsset asset)) return asset;
        return null;
    }
}