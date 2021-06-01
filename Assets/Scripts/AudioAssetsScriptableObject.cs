using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MythkinCore.Audio {
    [System.Serializable]
    public enum AudioTag {
        debugVillager, debugStillAlive
    }
    [System.Serializable]
    public class AudioAsset {
        public AudioTag tag;
        public AudioClip clip;
        [Range(0f, 2f)]
        public float volume = 1f;

        public AudioAsset(AudioTag tag, AudioClip clip, float volume) {
            this.tag = tag;
            this.clip = clip;
            this.volume = volume;
        }
    }
    [System.Serializable]
    public struct AudioKey {
        public AudioTag tag;
        public GameObject target;

        public AudioKey(AudioTag tag, GameObject target) {
            this.tag = tag;
            this.target = target;
        }
    }
    // +++ +++ +++ +++ +++ SCRIPTABLE OBJECT +++ +++ +++ +++ +++ //
    [CreateAssetMenu(fileName = "New AudioAssetFile", menuName = "ScriptableObjects/AudioAssets", order = 1)]
    public class AudioAssetsScriptableObject : ScriptableObject {
        public AudioAsset[] musics;
        public AudioAsset[] sounds;
        public AudioAsset[] sfx;

        /// <summary>
        /// Save all data in a dictionary using a AudioKey in string form as key and storing sounds in AudioAsset objects.
        /// </summary>
        /// <returns>A dictionary containing all of the audio assets accessible by their AudioKey.</returns>
        public Dictionary<AudioTag, AudioAsset> ToDictionary() {
            Dictionary<AudioTag, AudioAsset> dic = new Dictionary<AudioTag, AudioAsset>();
            AddToDictionary(ref dic, musics);
            AddToDictionary(ref dic, sounds);
            AddToDictionary(ref dic, sfx);
            return dic;
        }
        void AddToDictionary(ref Dictionary<AudioTag, AudioAsset> dictionary, AudioAsset[] assetArray) {
            if(assetArray == null) return;
            for(int i = 0; i < assetArray.Length; i++) {
                if(assetArray[i].clip == null) continue;
                if(dictionary.ContainsKey(assetArray[i].tag)) Debug.LogWarning($"The sound tag \"{assetArray[i].tag}\" has already been used! \nPlease check the AudioManager to make sure no sound tags are used more than once.");
                dictionary[assetArray[i].tag] = assetArray[i];
            }
        }
    }
}