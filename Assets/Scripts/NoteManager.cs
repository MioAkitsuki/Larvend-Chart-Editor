using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;

namespace Larvend.Gameplay
{
    public class BeatRange
    {
        public float start;
        public float end;
        public float range;
        public bool IsLinearlyChanging = false;

        public BeatRange(float _start, float _end)
        {
            start = _start;
            end = _end;
            range = _end - _start;
            IsLinearlyChanging = false;
        }

        public BeatRange(float _start, float _end, bool flag)
        {
            start = _start;
            end = _end;
            range = _end - _start;
            IsLinearlyChanging = flag;
        }

        public bool IsIn(float value)
        {
            if (value >= start && value <= end)
            {
                return true;
            }
            return false;
        }

        public bool IsBothIn(float v1, float v2)
        {
            if (v1 >= start && v1 <= end && v2 >= start && v2 <= end)
            {
                return true;
            }
            return false;
        }
    }

    public class NoteManager : MonoBehaviour
    {
        public static NoteManager Instance { get; set; }

        [SerializeField] private GameObject[] prefabs;

        public List<Note> TapNotes { get; private set; }
        public List<Note> HoldNotes { get; private set; }
        public List<Note> FlickNotes { get; private set; }
        public Line BaseSpeed;
        public List<Line> SpeedAdjust = new();
        public Dictionary<BeatRange, float> PcmDict = new Dictionary<BeatRange, float>();
        public List<Note> UnplayedNotes { get; private set; }
        public List<Line> UnplayedSpeedAdjusts;

        private void Awake()
        {
            Instance = this;
            UnplayedNotes = new();
            UnplayedSpeedAdjusts = new();
            TapNotes = new();
            HoldNotes = new();
            FlickNotes = new();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space) && Global.IsAudioLoaded && !Global.IsDialoging && !Global.IsEditing && !Global.IsPlaying)
            {
                StartCoroutine(PlayPreparation());
            }
            if (Global.IsPlaying)
            {
                if (UnplayedNotes.Count > 0)
                {
                    for (var i = 0; i < UnplayedNotes.Count; i++)
                    {
                        if (Math.Abs(UnplayedNotes[i].time - EditorManager.GetAudioPCMTime()) > EditorManager.Instance.BeatPCM)
                        {
                            break;
                        }
                    
                        UnplayedNotes[i].gameObject.SetActive(true);
                        UnplayedNotes[i].StartCoroutine("StartPlay");
                        UnplayedNotes.RemoveAt(i);
                        i--;
                    }
                }

                if (UnplayedSpeedAdjusts.Count > 0)
                {
                    for (var i = 0; i < UnplayedSpeedAdjusts.Count; i++)
                    {
                        if (UnplayedSpeedAdjusts[i].time > EditorManager.GetAudioPCMTime())
                        {
                            break;
                        }

                        EditorManager.Instance.LinearlyUpdateBPM(UnplayedSpeedAdjusts[i].time,
                            UnplayedSpeedAdjusts[i].targetBpm, UnplayedSpeedAdjusts[i].endTime);
                        UnplayedSpeedAdjusts.RemoveAt(i);
                        i--;
                    }
                }
            }
            if (Input.GetKeyUp(KeyCode.Space) && Global.IsAudioLoaded && !Global.IsDialoging && !Global.IsEditing)
            {
                EditorManager.Stop();
                RefreshAllNotes();

                // EventTrack.EnableAll();
                Global.IsPrepared = false;
            }
        }

        public IEnumerator PlayPreparation()
        {
            NotePreparation();
            yield return new WaitUntil(() => Global.IsPrepared);

            EditorManager.Play();
        }

        public void NotePreparation()
        {
            UnplayedNotes = new List<Note>();
            UnplayedSpeedAdjusts = new List<Line>();

            foreach (var note in TapNotes)
            {
                if (note.time > EditorManager.GetAudioPCMTime() - EditorManager.Instance.BeatPCM)
                {
                    UnplayedNotes.Add(note);
                }
            }
            foreach (var note in HoldNotes)
            {
                if (note.endTime > EditorManager.GetAudioPCMTime() - EditorManager.Instance.BeatPCM)
                {
                    UnplayedNotes.Add(note);
                }
            }
            foreach (var note in FlickNotes)
            {
                if (note.time > EditorManager.GetAudioPCMTime() - EditorManager.Instance.BeatPCM)
                {
                    UnplayedNotes.Add(note);
                }
            }

            if (SpeedAdjust.Count > 0)
            {
                foreach (var line in SpeedAdjust)
                {
                    if (line.time > EditorManager.GetAudioPCMTime())
                    {
                        UnplayedSpeedAdjusts.Add(line);
                        break;
                    }
                }
            }

            UnplayedNotes.Sort((p1, p2) =>
            {
                if (p1.time != p2.time)
                {
                    return p1.time.CompareTo(p2.time);
                }
                else return 0;
            });

            // EventTrack.DisableAll();

            Global.IsPrepared = true;
        }

        public static void ClearAllNotes()
        {
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < Instance.transform.GetChild(i).childCount; j++)
                {
                    Destroy(Instance.transform.GetChild(i).GetChild(j).gameObject);
                }
            }
            Instance.TapNotes.Clear();
            Instance.HoldNotes.Clear();
            Instance.FlickNotes.Clear();
            Instance.BaseSpeed = null;
        }

        public static void RefreshAllNotes()
        {
            foreach (var note in Instance.TapNotes)
            {
                if ((EditorManager.GetAudioPCMTime() - note.time) / 44100f / (60f / EditorManager.GetBPM()) < -1)
                {
                    note.gameObject.SetActive(false);
                    continue;
                }

                note.RefreshState();
            }

            foreach (var note in Instance.HoldNotes)
            {
                if ((EditorManager.GetAudioPCMTime() - note.time) / 44100f / (60f / EditorManager.GetBPM()) < -1)
                {
                    note.gameObject.SetActive(false);
                    continue;
                }

                note.RefreshState();
            }

            foreach (var note in Instance.FlickNotes)
            {
                if ((EditorManager.GetAudioPCMTime() - note.time) / 44100f / (60f / EditorManager.GetBPM()) < -1)
                {
                    note.gameObject.SetActive(false);
                    continue;
                }

                note.RefreshState();
            }
        }

        public IEnumerator RefreshSpeedEnumerator()
        {
            yield return new WaitUntil(() => Global.IsAudioLoaded && Global.IsFileSelected);
            RefreshSpeed();
        }

        // Very disgusting code. I really suggest you do not modify it.
        public static void RefreshSpeed()
        {
            Instance.PcmDict = new();
            if (Instance.SpeedAdjust.Count == 0)
            {
                // The happiest thing - No Speed Change!
                // (44100 * (60f / Instance.BaseSpeed.targetBpm)) - The Pcm Samples Per Beat
                float threshold = (EditorManager.GetAudioPCMLength() - EditorManager.Instance.offset) / (44100 * (60f / Instance.BaseSpeed.targetBpm)) + 1;
                Instance.PcmDict.Add(new BeatRange(1, threshold), 44100 * (60f / Instance.BaseSpeed.targetBpm));

                Debug.Log($"0. ({1}, {threshold}, {44100 * (60f / Instance.BaseSpeed.targetBpm)})");
                EditorManager.SetMaxTicks(Mathf.RoundToInt(960 * threshold));
            }
            else
            {
                int i;
                float upper = 0, lower = 0;

                for (i = 0; i < Instance.SpeedAdjust.Count; i++)
                {
                    if (i == 0)
                    {
                        // Writing Base Bpm Item
                        upper = Mathf.RoundToInt((Instance.SpeedAdjust[i].time - EditorManager.Instance.offset) / (44100 * (60f / Instance.BaseSpeed.targetBpm))) + 1;
                        Instance.PcmDict.Add(new BeatRange(1, upper + 1), (int) (44100 * (60f / Instance.BaseSpeed.targetBpm)));
                        lower = upper + 1;
                        Debug.Log($"0. ({1}, {upper}, {(int) (44100 * (60f / Instance.BaseSpeed.targetBpm))})");
                        continue;
                    }

                    if (Instance.SpeedAdjust[i].time == Instance.SpeedAdjust[i].endTime)  // Shear
                    {
                        upper = Mathf.RoundToInt((Instance.SpeedAdjust[i].time - Instance.SpeedAdjust[i - 1].endTime) / (44100 * 60f / Instance.SpeedAdjust[i - 1].targetBpm)) + lower;
                        Instance.PcmDict.Add(new BeatRange(lower, upper + 1), (int)(44100 * (60f / Instance.SpeedAdjust[i - 1].targetBpm)));
                        Debug.Log($"{i}. ({lower}, {upper}, {(int) (44100 * (60f / Instance.SpeedAdjust[i - 1].targetBpm))})");
                        lower = upper + 1;
                    }
                    else  // Linear Update
                    {
                        upper = lower + Instance.SpeedAdjust[i].sustainSection;
                        Instance.PcmDict.Add(new BeatRange(lower, upper + 1, true), Instance.SpeedAdjust[i].endTime - Instance.SpeedAdjust[i].time);
                        Debug.Log($"{i}. ({lower}, {upper}, {Instance.SpeedAdjust[i].endTime - Instance.SpeedAdjust[i].time})");
                        lower = upper + 1;
                    }
                    
                }

                if (Instance.SpeedAdjust[i - 1].time == Instance.SpeedAdjust[i - 1].endTime)
                {
                    upper = (EditorManager.GetAudioPCMLength() - Instance.SpeedAdjust[i - 1].endTime) / (44100 * 60f / Instance.SpeedAdjust[i - 1].targetBpm) + lower;
                    Instance.PcmDict.Add(new BeatRange(lower, upper + 1), (int)(44100 * (60f / Instance.SpeedAdjust[i - 1].targetBpm)));
                    Debug.Log($"{i}. ({lower}, {upper}, {(int) (44100 * (60f / Instance.SpeedAdjust[i - 1].targetBpm))})");
                }
                else
                {
                    Instance.PcmDict.Add(new BeatRange(lower, lower + Instance.SpeedAdjust[i - 1].sustainSection, true), Instance.SpeedAdjust[i - 1].endTime - Instance.SpeedAdjust[i - 1].time);
                    Debug.Log($"{i}. ({lower}, {lower+1}, {Instance.SpeedAdjust[i - 1].endTime - Instance.SpeedAdjust[i - 1].time})");

                    upper = (EditorManager.GetAudioPCMLength() - Instance.SpeedAdjust[i - 1].endTime) / (44100 * 60f / Instance.SpeedAdjust[i - 1].targetBpm) + lower + Instance.SpeedAdjust[i - 1].sustainSection;
                    Instance.PcmDict.Add(new BeatRange(lower + Instance.SpeedAdjust[i - 1].sustainSection, upper + 1), (int)(44100 * (60f / Instance.SpeedAdjust[i - 1].targetBpm)));
                    Debug.Log($"{i+1}. ({lower+1}, {upper}, {(int) (44100 * (60f / Instance.SpeedAdjust[i - 1].targetBpm))})");
                }

                EditorManager.SetMaxTicks(Mathf.RoundToInt(960 * upper));
            }

            UIController.RefreshSpeedPanel(Instance.SpeedAdjust);
            EventTrackController.RefreshPanel();
            EditorManager.ResetAudio();
        }

        public static Note CreateNote(Type type)
        {
            if (!Global.IsFileSelected)
            {
                return null;
            }

            Note newNote = null;

            if (type == Type.Tap)
            {
                newNote = Instantiate(Instance.prefabs[0], Instance.transform.GetChild(0)).GetComponent<Note>();
                newNote.InitNote(type, EditorManager.GetAudioPCMTime(), new Vector3(0.5f, 0.5f, EditorManager.GetAudioPCMTime() / 10000f));

                newNote.RefreshState();
                newNote.Relate();
                
                OperationTracker.Record(new Operation(OperationType.Create, null, new Line(newNote)));

                Instance.TapNotes.Add(newNote);
            }
            else if (type == Type.Hold)
            {
                newNote = Instantiate(Instance.prefabs[1], Instance.transform.GetChild(1)).GetComponent<Note>();
                newNote.InitNote(type, EditorManager.GetAudioPCMTime(), new Vector3(0.5f, 0.5f, EditorManager.GetAudioPCMTime() / 10000f), EditorManager.GetAudioPCMTime());

                newNote.RefreshState();
                newNote.Relate();

                Instance.HoldNotes.Add(newNote);
            }
            else if (type == Type.Flick)
            {
                newNote = Instantiate(Instance.prefabs[2], Instance.transform.GetChild(2)).GetComponent<Note>();
                newNote.InitNote(type, EditorManager.GetAudioPCMTime(), new Vector3(0.5f, 0.5f, EditorManager.GetAudioPCMTime() / 10000f));

                newNote.RefreshState();
                newNote.Relate();

                Instance.FlickNotes.Add(newNote);
            }

            TypeEventSystem.Global.Send(new GroupRefreshEvent());
            Global.IsSaved = false;
            return newNote;
        }

        public static Note CreateNote(Type type, int targetTime)
        {
            if (!Global.IsFileSelected)
            {
                return null;
            }

            Note newNote = null;

            if (type == Type.Tap)
            {
                newNote = Instantiate(Instance.prefabs[0], Instance.transform.GetChild(0)).GetComponent<Note>();
                newNote.InitNote(type, targetTime, new Vector3(0.5f, 0.5f, targetTime / 10000f));

                newNote.RefreshState();
                newNote.Relate();

                Instance.TapNotes.Add(newNote);
            }
            else if (type == Type.Hold)
            {
                newNote = Instantiate(Instance.prefabs[1], Instance.transform.GetChild(1)).GetComponent<Note>();
                newNote.InitNote(type, targetTime, new Vector3(0.5f, 0.5f, targetTime / 10000f), targetTime);

                newNote.RefreshState();

                Instance.HoldNotes.Add(newNote);
            }
            else if (type == Type.Flick)
            {
                newNote = Instantiate(Instance.prefabs[2], Instance.transform.GetChild(2)).GetComponent<Note>();
                newNote.InitNote(type, targetTime, new Vector3(0.5f, 0.5f, targetTime / 10000f));

                newNote.RefreshState();

                Instance.FlickNotes.Add(newNote);
            }

            TypeEventSystem.Global.Send(new GroupRefreshEvent());
            Global.IsSaved = false;
            return newNote;
        }

        public static Note CreateNoteWithoutRelate(Type type, int targetTime)
        {
            if (!Global.IsFileSelected)
            {
                return null;
            }

            Note newNote = null;

            if (type == Type.Tap)
            {
                newNote = Instantiate(Instance.prefabs[0], Instance.transform.GetChild(0)).GetComponent<Note>();
                newNote.InitNote(type, targetTime, new Vector3(0.5f, 0.5f, targetTime / 10000f));

                newNote.RefreshState();

                Instance.TapNotes.Add(newNote);
            }
            else if (type == Type.Hold)
            {
                newNote = Instantiate(Instance.prefabs[1], Instance.transform.GetChild(1)).GetComponent<Note>();
                newNote.InitNote(type, targetTime, new Vector3(0.5f, 0.5f, targetTime / 10000f), targetTime);

                newNote.RefreshState();

                Instance.HoldNotes.Add(newNote);
            }
            else if (type == Type.Flick)
            {
                newNote = Instantiate(Instance.prefabs[2], Instance.transform.GetChild(2)).GetComponent<Note>();
                newNote.InitNote(type, targetTime, new Vector3(0.5f, 0.5f, targetTime / 10000f));

                newNote.RefreshState();

                Instance.FlickNotes.Add(newNote);
            }
            Global.IsSaved = false;
            return newNote;
        }

        /// <summary>
        /// From chart file to load a certain note into the scene. At its position and disabled by default.
        /// Will be automatically distributed to NoteManager.
        /// </summary>
        /// <param name="line">The line provided and wanted to load.</param>
        public static void LoadNote(Line line)
        {
            if (line.type == Type.Tap)
            {
                var newNote = Instantiate(Instance.prefabs[0], Instance.transform.GetChild(0)).GetComponent<Note>();
                newNote.InitNote(line.type, line.time, line.position);

                newNote.RefreshState();

                Instance.TapNotes.Add(newNote);
            }
            else if (line.type == Type.Hold)
            {
                var newNote = Instantiate(Instance.prefabs[1], Instance.transform.GetChild(1)).GetComponent<Note>();
                newNote.InitNote(line.type, line.time, line.position, line.endTime);

                newNote.RefreshState();

                Instance.HoldNotes.Add(newNote);
            }
            else if (line.type == Type.Flick)
            {
                var newNote = Instantiate(Instance.prefabs[2], Instance.transform.GetChild(2)).GetComponent<Note>();
                newNote.InitNote(line.type, line.time, line.position);

                newNote.RefreshState();

                Instance.FlickNotes.Add(newNote);
            }
            else if (line.type == Type.SpeedAdjust)
            {
                if (Instance.BaseSpeed == null && line.time == 0 && line.endTime == 0)
                {
                    Instance.BaseSpeed = new Line(line.targetBpm);
                    EditorManager.Instance.InitializeBPM(line.targetBpm);
                    return;
                }
                Instance.SpeedAdjust.Add(line);
            }
        }

        public static void ReRelateAllNotes()
        {
            foreach (var note in Instance.HoldNotes)
            {
                note.CancelRelation();
                note.Relate();
            }

            foreach (var note in Instance.TapNotes)
            {
                note.CancelRelation();
                note.Relate();
            }

            foreach (var note in Instance.FlickNotes)
            {
                note.CancelRelation();
                note.Relate();
            }
        }

        public Note Find(Note note)
        {
            switch (note.type)
            {
                case Type.Tap:
                    return TapNotes.Find(n => n == note);
                case Type.Hold:
                    return HoldNotes.Find(n => n == note);
                case Type.Flick:
                    return FlickNotes.Find(n => n == note);
            }
            return null;
        }

        public Note Find(Line line)
        {
            switch (line.type)
            {
                case Type.Tap:
                    return TapNotes.Find(n => n.time == line.time && n.position == line.position && n.scale == line.scale);
                case Type.Hold:
                    return HoldNotes.Find(n => n.time == line.time && n.position == line.position && n.endTime == line.endTime && n.scale == line.scale);
                case Type.Flick:
                    return FlickNotes.Find(n => n.time == line.time && n.position == line.position && n.scale == line.scale);
            }
            return null;
        }

        public static List<Line> GetAllNotes()
        {
            if (Instance.TapNotes.Count == 0 && Instance.HoldNotes.Count == 0 && Instance.FlickNotes.Count == 0 && Instance.SpeedAdjust.Count == 0)
            {
                return new List<Line>();
            }
            
            List<Note> allNotes = new List<Note>();
            allNotes.AddRange(Instance.TapNotes);
            allNotes.AddRange(Instance.HoldNotes);
            allNotes.AddRange(Instance.FlickNotes);
            
            List<Line> allLines = new List<Line>();
            foreach (var note in allNotes)
            {
                switch (note.type)
                {
                    case Type.Tap:
                        allLines.Add(new Line(note.type, note.time, note.position, note.scale));
                        break;
                    case Type.Hold:
                        allLines.Add(new Line(note.type, note.time, note.position, note.endTime, note.scale));
                        break;
                    case Type.Flick:
                        allLines.Add(new Line(note.type, note.time, note.position, note.scale));
                        break;
                }
            }
            allLines.AddRange(Instance.SpeedAdjust);

            allLines.Sort((p1, p2) =>
            {
                if (p1.time != p2.time)
                {
                    return p1.time.CompareTo(p2.time);
                }
                else if (p1.type != p2.type)
                {
                    return p1.type.CompareTo(p2.type);
                }
                else return 0;
            });

            return allLines;
        }

        public static void UpdateSpeedEvents(List<string> list)
        {
            try
            {
                Instance.SpeedAdjust.Clear();
                foreach (var line in list)
                {
                    if (line == "")
                    {
                        continue;
                    }
                    var note = ChartManager.ReadNote(line);
                    Instance.SpeedAdjust.Add(note);
                }

                UIController.Instance.isSpeedInputChanged = false;
                RefreshSpeed();
            }
            catch (Exception e)
            {
                MsgBoxManager.ShowMessage(MsgType.Error, "Error", e.Message);
            }
        }

        public static void UpdateBaseBpm(float value)
        {
            Instance.BaseSpeed = new Line(value);
            RefreshSpeed();
        }
    }
}