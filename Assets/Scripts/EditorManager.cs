using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace Larvend.Gameplay
{
    public class EditorManager : MonoBehaviour
    {
        public static EditorManager Instance { get; private set; }
        private static AudioSource song;

        private float BPM;

        private float lastTimePointer;

        private static float cortchet;
        private static float step;
        public static float tick;
        private static float timePointer;

        public static bool isAudioPlaying;

        private void Awake()
        {
            Instance = this;
            song = gameObject.GetComponent<AudioSource>();
            timePointer = 0f;
            BPM = 120f;
            isAudioPlaying = false;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space) && Global.IsAudioLoaded)
            {
                song.time = timePointer;
                lastTimePointer = timePointer;
                isAudioPlaying = true;
                song.Play();
            }
            if (Input.GetKey(KeyCode.Space) && Global.IsAudioLoaded)
            {
                // Debug.Log(song.timeSamples);
            }
            if (Input.GetKeyUp(KeyCode.Space) && Global.IsAudioLoaded)
            {
                timePointer = lastTimePointer;
                song.time = lastTimePointer;
                UIController.RefreshUI();
                isAudioPlaying = false;
                song.Stop();
            }

            if (Input.GetKeyUp(KeyCode.RightArrow) && Global.IsAudioLoaded && !isAudioPlaying)
            {
                timePointer = timePointer + step > song.clip.length ? song.clip.length : timePointer + step;
            }

            if (Input.GetKeyUp(KeyCode.LeftArrow) && Global.IsAudioLoaded && !isAudioPlaying)
            {
                timePointer = timePointer - step < 0 ? 0 : timePointer - step;
            }
        }

        /// <summary>
        /// Load AudioClip to Editor
        /// </summary>
        /// <param name="clip">External AudioClip</param>
        public static void InitAudio(AudioClip clip)
        {
            song.clip = clip;
        }

        /// <summary>
        /// Set the song into initial status
        /// </summary>
        public static void ResetAudio()
        {
            if (Global.IsAudioLoaded)
            {
                song.time = 0;
                timePointer = 0;
                UIController.RefreshUI();
            }
        }

        public void InitializeBPM(float bpm)
        {
            BPM = bpm;
            cortchet = 60f / bpm;
            tick = cortchet / 960f;
        }

        public static float GetAudioLength()
        {
            return song.clip.length;
        }
        public static float GetAudioTime()
        {
            return song.time;
        }
        public static int GetAudioPCMTime()
        {
            return song.timeSamples;
        }
        public static void SetPlayTime(float time)
        {
            timePointer = time;
        }
        // TODO: Connect with UI System
        public static void UpdateStep(int denominator, bool isTriplet, bool isDotted)
        {
            step = cortchet * 4 / denominator;
            step = isTriplet ? step * 2 / 3 : step;
            step = isDotted ? step * 3 / 2 : step;
        }

        /// <summary>
        /// Linearly Update BPM in given period
        /// </summary>
        /// <param name="beginTime"></param>
        /// <param name="targetBPM"></param>
        /// <param name="endTime"></param>
        public void LinearlyUpdateBPM(int beginTime, float targetBPM, int endTime)
        {
            int deltaTime = endTime - song.timeSamples;
            float initialBPM = BPM;
            if (deltaTime <= 0 || beginTime == endTime)
            {
                BPM = targetBPM;
                cortchet = 60f / BPM;
                tick = cortchet / 960f;
            }
            else
            {
                StartCoroutine(LinearlyUpdateBPM(beginTime, initialBPM, targetBPM, deltaTime));
            }
        }
        private IEnumerator LinearlyUpdateBPM(int beginTime, float initialBPM, float targetBPM, int deltaTime)
        {
            if (song.timeSamples > beginTime + deltaTime)
            {
                BPM = targetBPM;
                yield break;
            }

            int time = song.timeSamples - beginTime;

            BPM = Mathf.Lerp(initialBPM, targetBPM, (float)time / deltaTime);
            cortchet = 60f / BPM;
            tick = cortchet / 960f;

            yield return new WaitForFixedUpdate();
            StartCoroutine(LinearlyUpdateBPM(beginTime, initialBPM, targetBPM, deltaTime));
        }
    }

}