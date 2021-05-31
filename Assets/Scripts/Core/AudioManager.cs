using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour {
    [System.Serializable]
    public enum SoundTag {
        debugVillager, debugStillAlive
    }
    [System.Serializable]
    public class SoundAsset {
        public SoundTag tag;
        public AudioClip clip;
        public float minLength;
    }
    public struct SoundKey {
        public SoundTag tag;
        public GameObject target;

        public SoundKey(SoundTag tag, GameObject target) {
            this.tag = tag;
            this.target = target;
        }
    }

    public static AudioManager instance = null;

    [SerializeField] GameObject right, left;
    [SerializeField] float volumeMusic = 0.5f, volumeSound = 0.5f;
    [SerializeField] SoundAsset[] soundAssets;
    Dictionary<string, SoundAsset> assetDic;
    Dictionary<SoundKey, AudioSource> sourceDic;

    private void Awake() {
        if(instance == null) instance = this;
        else if(instance != this) Destroy(gameObject);

        // setup the sound dictionary
        assetDic = new Dictionary<string, SoundAsset>();
        for(int i = 0; i < soundAssets.Length; i++) {
            if(assetDic.ContainsKey(soundAssets[i].tag.ToString())) Debug.LogWarning($"The sound tag \"{soundAssets[i].tag}\" has already been used! \nPlease check the AudioManager to make sure no sound tags are used more than once.");
            //soundAssets[i].lastStartTime = Time.time - soundAssets[i].minLength;
            assetDic[soundAssets[i].tag.ToString()] = soundAssets[i];
        }
        // setup the source dictionary
        sourceDic = new Dictionary<SoundKey, AudioSource>();
    }

    private void Start() {
        SoundKey keyStillAlive = new SoundKey(SoundTag.debugStillAlive, null);
        PlayMusic(keyStillAlive.tag, true);
        PauseMusic(keyStillAlive);
    }

    private void Update() {
        if(Input.GetKeyDown(KeyCode.O)) {
            // play debug sound
            AudioManager.instance.PlaySound(AudioManager.SoundTag.debugVillager, left);
        }
        if(Input.GetKeyDown(KeyCode.P)) {
            // play debug sound
            AudioManager.instance.PlaySound(AudioManager.SoundTag.debugVillager, right);
        }
        if(Input.GetKeyDown(KeyCode.M)) {
            SoundKey key = new SoundKey(SoundTag.debugStillAlive, null);
            PauseMusic(key);
        }
    }

    public void PlaySound(SoundTag tag, float volumeScale = 1) => PlaySound(tag, null, volumeScale);
    public void PlaySound(SoundTag tag, GameObject target, float volumeScale = 1) {
        SoundAsset asset = GetSoundAsset(tag);
        if(asset == null) return;
        AudioSource source = GetSource(tag, target);
        if(source == null) source = (target != null ? target : new GameObject()).AddComponent<AudioSource>();
        //source.clip = asset.clip;
        source.volume = volumeSound;
        source.spatialBlend = target != null ? 1 : 0; // [0: 2D] [1: 3D]
        source.PlayOneShot(asset.clip, volumeScale);
        SetSource(tag, target, source);
    }

    public void PlayMusic(SoundTag tag, bool loop) => PlayMusic(tag, null, loop);
    public void PlayMusic(SoundTag tag, GameObject target, bool loop) {
        SoundAsset asset = GetSoundAsset(tag);
        if(asset == null) return;
        AudioSource source = GetSource(tag, target);
        if(source == null) source = (target != null ? target : new GameObject()).AddComponent<AudioSource>();
        source.clip = asset.clip;
        source.loop = loop;
        source.volume = volumeSound;
        source.spatialBlend = target != null ? 1 : 0; // [0: 2D] [1: 3D]
        source.Play();
        SetSource(tag, target, source);
    }

    public void PauseMusic(SoundTag tag, GameObject target) => PauseMusic(new SoundKey(tag, target));
    public void PauseMusic(SoundKey key) {
        AudioSource source = GetSource(key);
        if(source == null) return;
        if(source.isPlaying) source.Pause();
        else source.UnPause();
    }

    public void StopMusic(SoundTag tag, GameObject target) => StopMusic(new SoundKey(tag, target));
    public void StopMusic(SoundKey key) {
        AudioSource source = GetSource(key);
        if(source == null) return;
        source.Stop();
    }

    bool IsPlaying(SoundKey key) {
        AudioSource source = GetSource(key);
        if(source == null) return false;
        return source.isPlaying;
    }

    SoundAsset GetSoundAsset(SoundTag tag) {
        if(assetDic.ContainsKey(tag.ToString())) {
            return assetDic[tag.ToString()];
        }
        Debug.LogError($"The sound with tag \"{tag}\" was not found!");
        return null;
    }

    AudioSource GetSource(SoundTag tag, GameObject target) => GetSource(new SoundKey(tag, target));
    AudioSource GetSource(SoundKey key) {
        if(sourceDic.ContainsKey(key)) {
            return sourceDic[key];
        }
        return null;
    }

    void SetSource(SoundTag tag, GameObject target, AudioSource source) => SetSource(new SoundKey(tag, target), source);
    void SetSource(SoundKey key, AudioSource source) {
        sourceDic[key] = source;
    }
}