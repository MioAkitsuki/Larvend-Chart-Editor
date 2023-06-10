using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Larvend.Gameplay
{
    public class EditorManager : MonoBehaviour
    {
        public static EditorManager Instance { get; private set; }
        private static AudioSource song;
        private static Info info;
        private static DifficultyInfo difficultyInfo;

        private float BPM;
        public int BeatPCM;
        public int offset;
        public int difficulty;
        
        private int lastPcmPointer;
        
        private static int step;
        private static int timePcmPointer;
        

        public static bool isAudioPlaying;

        private void Awake()
        {
            Instance = this;
            song = gameObject.GetComponent<AudioSource>();
            offset = 0;
            difficulty = 0;
            
            timePcmPointer = 0;
            BPM = 120f;
            BeatPCM = 22050;
            isAudioPlaying = false;

            Application.wantsToQuit += WantsToQuit;
            InitPlayerPrefs();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space) && Global.IsAudioLoaded && !Global.IsDialoging && !Global.IsEditing)
            {
                song.timeSamples = timePcmPointer;
                Instance.lastPcmPointer = timePcmPointer;
            }
            if (Input.GetKeyUp(KeyCode.Space) && Global.IsAudioLoaded && !Global.IsDialoging && !Global.IsEditing)
            {
                timePcmPointer = Instance.lastPcmPointer;
                song.timeSamples = timePcmPointer;
                UIController.RefreshUI();
            }
        }

        static bool WantsToQuit()
        {
            if (!Global.IsSaved)
            {
                MsgBoxManager.ShowMessage(MsgType.Warning, "Warning", Localization.GetString("UnsavedChart"),
                    SaveProject, delegate()
                    {
                        Application.wantsToQuit -= WantsToQuit;
                        Application.Quit();
                    });
                return false;
            }
            return true;
        }

        public static void Play()
        {
            if (Global.IsPrepared)
            {
                isAudioPlaying = true;
                song.Play();
                Global.IsPlaying = true;
            }
        }

        public static void Stop()
        {
            isAudioPlaying = false;
            song.Pause();
            Global.IsPlaying = false;
        }

        public static void InitPlayerPrefs()
        {
            PlayerPrefs.DeleteAll();
            if (!PlayerPrefs.HasKey("Language"))
            {
                PlayerPrefs.SetString("Language", "zh_cn");
            }
        }

        public static void SaveProject()
        {
            try
            {
                if (!Global.IsFileSelected)
                {
                    return;
                }
                InfoManager.WriteInfo();
                ChartManager.WriteChart(Instance.difficulty);

                MsgBoxManager.ShowMessage(MsgType.Info, "Saved Successfully", Localization.GetString("SaveChartSuccess"));
                Global.IsSaved = true;
            }
            catch (Exception e)
            {
                MsgBoxManager.ShowMessage(MsgType.Error, "Error", e.Message);
            }
        }

        public static Info GetInfo()
        {
            return info;
        }

        public static DifficultyInfo GetDifficultyInfo()
        {
            return difficultyInfo;
        }

        public static void UpdateInfo(Info newInfo)
        {
            info = newInfo;
        }

        public static void UpdateInfo(string title, string composer, string arranger, string rating)
        {
            info.title = title;
            info.composer = composer;
            foreach (var diffInfo in info.difficulties)
            {
                if (diffInfo.diffIndex == Instance.difficulty)
                {
                    diffInfo.arranger = arranger;
                    diffInfo.rating = rating;
                }
            }

            difficultyInfo.arranger = arranger;
            difficultyInfo.rating = rating;
        }

        public static void UpdateDifficultyInfo(DifficultyInfo newInfo)
        {
            difficultyInfo = newInfo;
            Instance.difficulty = newInfo.diffIndex;
        }

        public static void AddDifficulty()
        {
            var newInfo = new DifficultyInfo();
            info.difficulties.Add(newInfo);
            UpdateDifficultyInfo(newInfo);

            InfoManager.WriteInfo();
        }

        public static void AddDifficulty(int diff, string arranger, string rating)
        {
            var newInfo = new DifficultyInfo(diff, arranger, rating);
            info.difficulties.Add(newInfo);
            UpdateDifficultyInfo(newInfo);

            InfoManager.WriteInfo();
        }

        public static void SwitchDifficulty(int diff)
        {
            NoteManager.ClearAllNotes();
            NoteManager.RefreshAllNotes();

            foreach (var diffInfo in info.difficulties)
            {
                if (diffInfo.diffIndex == diff)
                {
                    ChartManager.ReadChart(diff);

                    ResetAudio();
                    Instance.difficulty = diff;

                    UpdateDifficultyInfo(diffInfo);

                    UIController.RefreshUI();
                    return;
                }
            }

            DirectoryManager.CreateChart(diff);
            ResetAudio();
            Instance.difficulty = diff;
            UIController.RefreshUI();
            AddDifficulty(diff, "Sample Arranger", "TBD");
        }

        public static float GetBPM()
        {
            return Instance.BPM;
        }

        public static void SetStep(double s)
        {
            step = (int) (Instance.BeatPCM * s);
        }

        public static void StepForward()
        {
            if (NoteManager.Instance.SpeedAdjust.Count > 0)
            {
                if (GetAudioPCMTime() < NoteManager.Instance.SpeedAdjust[0].time)
                {
                    if ((GetBPM() - NoteManager.Instance.BaseSpeed.targetBpm) != 0)
                    {
                        UpdateBpm(NoteManager.Instance.BaseSpeed.targetBpm);
                    }
                }
                else
                {
                    foreach (var line in NoteManager.Instance.SpeedAdjust)
                    {
                        if (GetAudioPCMTime() >= line.time + line.endTime)
                        {
                            UpdateBpm(line.targetBpm);
                        }
                        else if (GetAudioPCMTime() < line.time + line.endTime && GetAudioPCMTime() > line.time)
                        {
                            float proportion = (float)(GetAudioPCMTime() - line.time) / line.endTime;
                            UpdateBpm((line.targetBpm - GetBPM()) * proportion + GetBPM());
                        }
                    }
                }
            }

            timePcmPointer = timePcmPointer + step > song.clip.samples ? song.clip.samples : timePcmPointer + step;
            song.timeSamples = timePcmPointer;
            UIController.RefreshUI();
        }

        public static void StepBackward()
        {
            if (NoteManager.Instance.SpeedAdjust.Count > 0)
            {
                if (GetAudioPCMTime() <= NoteManager.Instance.SpeedAdjust[0].time)
                {
                    if ((GetBPM() - NoteManager.Instance.BaseSpeed.targetBpm) != 0)
                    {
                        UpdateBpm(NoteManager.Instance.BaseSpeed.targetBpm);
                    }
                }
                else
                {
                    foreach (var line in NoteManager.Instance.SpeedAdjust)
                    {
                        if (GetAudioPCMTime() > line.time + line.endTime)
                        {
                            UpdateBpm(line.targetBpm);
                        }
                        else if (GetAudioPCMTime() < line.time + line.endTime && GetAudioPCMTime() > line.time)
                        {
                            float proportion = (float)(GetAudioPCMTime() - line.time) / line.endTime;
                            UpdateBpm((line.targetBpm - GetBPM()) * proportion + GetBPM());
                        }
                    }
                }
            }

            timePcmPointer = timePcmPointer - step < 0 ? 0 : timePcmPointer - step;
            song.timeSamples = timePcmPointer;
            UIController.RefreshUI();
        }

        public static void AdjustPointer(int pointer)
        {
            timePcmPointer  = pointer * Instance.BeatPCM;
            song.timeSamples = timePcmPointer;
            UIController.RefreshUI();
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
                song.timeSamples = 0;
                timePcmPointer = 0;
                UpdateBpm(NoteManager.Instance.BaseSpeed.targetBpm);
                UIController.RefreshUI();
            }
        }

        public void InitializeBPM(float bpm)
        {
            BPM = bpm;
            BeatPCM = (int) (44100 * (60f / bpm));

            ResetAudio();
            UIController.InitUI();
        }

        public static float GetAudioLength()
        {
            return song.clip.length;
        }

        public static int GetAudioPCMLength()
        {
            return song.clip.samples;
        }
        public static float GetAudioTime()
        {
            return song.time;
        }
        public static int GetAudioPCMTime()
        {
            return song.timeSamples;
        }

        public static int GetTimePointer()
        {
            return timePcmPointer;
        }

        public static void UpdateBpm(float targetBpm)
        {
            Instance.BPM = targetBpm;
            Instance.BeatPCM = (int)(44100 * (60f / targetBpm));

            UIController.UpdateBpmUI();
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
                BeatPCM = (int)(44100 * (60f / targetBPM));

                UIController.UpdateBpmUI();
            }
            else
            {
                StartCoroutine(LinearlyUpdateBPM(beginTime, initialBPM, targetBPM, deltaTime));
            }
        }
        private IEnumerator LinearlyUpdateBPM(int beginTime, float initialBPM, float targetBPM, int deltaTime)
        {
            while (song.timeSamples < beginTime + deltaTime)
            {
                int time = song.timeSamples - beginTime;

                BPM = Mathf.Lerp(initialBPM, targetBPM, (float)time / deltaTime);
                BeatPCM = (int)(44100 * (60f / BPM));

                UIController.UpdateBpmUI();
                yield return new WaitForFixedUpdate();
            }

            BPM = targetBPM;
            BeatPCM = (int)(44100 * (60f / targetBPM));

            UIController.UpdateBpmUI();
            yield break;
        }
    }

}