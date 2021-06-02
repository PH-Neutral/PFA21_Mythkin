using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MythkinCore.Audio;

public class AudioManager : MonoBehaviour {
    public static AudioManager instance = null;

    [Range(0f,1f)]
    [SerializeField] float volumeMusic = 0.5f, volumeSound = 0.5f;
    [SerializeField] AudioAssetsScriptableObject audioAssets;
    Dictionary<AudioTag, AudioAsset> _assetDic;
    Dictionary<AudioKey, AudioSource> _sourceDic;

    private void Awake() {
        if(instance == null) instance = this;
        else if(instance != this) Destroy(gameObject);

        // setup the tracks dictionary
        _assetDic = audioAssets.ToDictionary();
        // setup the source dictionary
        _sourceDic = new Dictionary<AudioKey, AudioSource>();
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
        SetSource(tag, target, source);
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
        SetSource(tag, target, source);
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

    void SetSource(AudioTag tag, GameObject target, AudioSource source) => SetSource(new AudioKey(tag, target), source);
    void SetSource(AudioKey key, AudioSource source) {
        _sourceDic[key] = source;
    }
}