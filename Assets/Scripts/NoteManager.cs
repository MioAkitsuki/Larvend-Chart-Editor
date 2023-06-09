using System;
using Larvend;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Larvend.Gameplay
{
    public class BeatRange
    {
        public int start;
        public int end;
        public int range;

        public BeatRange(int _start, int _end)
        {
            start = _start;
            end = _end;
            range = _end - _start + 1;
        }

        public bool IsIn(int value)
        {
            if (value >= start && value <= end)
            {
                return true;
            }
            return false;
        }

        public bool IsIn(float value)
        {
            if (value >= start && value <= end)
            {
                return true;
            }
            return false;
        }

        public bool IsBothIn(int v1, int v2)
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
        public Dictionary<BeatRange, int> PcmDict = new Dictionary<BeatRange, int>();
        public List<Note> UnplayedNotes { get; private set; }
        public List<Line> UnplayedSpeedAdjusts;

        private void Awake()
        {
            Instance = this;
            TapNotes = new();
            HoldNotes = new();
            FlickNotes = new();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space) && Global.IsAudioLoaded && !Global.IsDialoging && !Global.IsEditing && !Global.IsPlaying)
            {
                PlayPreparation();
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

                        int endTime = UnplayedSpeedAdjusts[i].time + UnplayedSpeedAdjusts[i].endTime;
                        EditorManager.Instance.LinearlyUpdateBPM(UnplayedSpeedAdjusts[i].time,
                            UnplayedSpeedAdjusts[i].targetBpm, endTime);
                        UnplayedSpeedAdjusts.RemoveAt(i);
                        i--;
                    }
                }
            }
            if (Input.GetKeyUp(KeyCode.Space) && Global.IsAudioLoaded && !Global.IsDialoging && !Global.IsEditing && Global.IsPlaying)
            {
                EditorManager.Stop();
                RefreshAllNotes();
                Global.IsPrepared = false;
            }
        }

        public void PlayPreparation()
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

            Global.IsPrepared = true;
            EditorManager.Play();
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

        public static void RefreshSpeed()
        {
            if (Global.IsAudioLoaded && Global.IsFileSelected)
            {
                Instance.PcmDict = new();
                if (Instance.SpeedAdjust.Count == 0)
                {
                    int threshold = (int)Math.Floor(EditorManager.GetAudioPCMLength() / (44100 * (60f / Instance.BaseSpeed.targetBpm)));
                    Instance.PcmDict.Add(new BeatRange(1, threshold), (int)(44100 * (60f / Instance.BaseSpeed.targetBpm)));
                }
                else
                {
                    int i;
                    int upper = 0, lower = 0;

                    for (i = 0; i < Instance.SpeedAdjust.Count; i++)
                    {
                        if (i == 0)
                        {
                            upper = (int)Math.Floor(Instance.SpeedAdjust[i].time / (44100 * (60f / Instance.BaseSpeed.targetBpm)));
                            Instance.PcmDict.Add(new BeatRange(1, upper), (int)(44100 * (60f / Instance.BaseSpeed.targetBpm)));
                            lower = upper + 1;
                            continue;
                        }

                        upper = (int)Math.Floor((Instance.SpeedAdjust[i].time - Instance.SpeedAdjust[i - 1].time) / (44100 * 60f / Instance.SpeedAdjust[i - 1].targetBpm)) + lower;
                        Instance.PcmDict.Add(new BeatRange(lower, upper), (int)(44100 * (60f / Instance.SpeedAdjust[i - 1].targetBpm)));
                        lower = upper + 1;
                    }

                    upper = (int)Math.Floor((EditorManager.GetAudioPCMLength() - Instance.SpeedAdjust[i - 1].time) / (44100 * 60f / Instance.SpeedAdjust[i - 1].targetBpm)) + lower;
                    Instance.PcmDict.Add(new BeatRange(lower, upper), (int)(44100 * (60f / Instance.SpeedAdjust[i - 1].targetBpm)));
                }

                UIController.RefreshSpeedPanel(Instance.SpeedAdjust);
            }
        }

        public static void CreateNote(Type type)
        {
            if (!Global.IsFileSelected)
            {
                return;
            }

            if (type == Type.Tap)
            {
                var newNote = Instantiate(Instance.prefabs[0], Instance.transform.GetChild(0));
                newNote.GetComponent<Note>().InitNote(type, EditorManager.GetAudioPCMTime(), new Vector2(0.5f, 0.5f));

                var newPos = Camera.main.ViewportToWorldPoint(new Vector2(0.5f, 0.5f));
                newNote.transform.position = new Vector3(newPos.x, newPos.y, 1f);
                newNote.GetComponent<Note>().RefreshState();

                Instance.TapNotes.Add(newNote.GetComponent<Note>());
            }
            else if (type == Type.Hold)
            {
                var newNote = Instantiate(Instance.prefabs[1], Instance.transform.GetChild(1));
                newNote.GetComponent<Note>().InitNote(type, EditorManager.GetAudioPCMTime(), new Vector2(0.5f, 0.5f), EditorManager.GetAudioPCMTime() + EditorManager.Instance.BeatPCM);

                var newPos = Camera.main.ViewportToWorldPoint(new Vector2(0.5f, 0.5f));
                newNote.transform.position = new Vector3(newPos.x, newPos.y, 1f);
                newNote.GetComponent<Note>().RefreshState();

                Instance.HoldNotes.Add(newNote.GetComponent<Note>());
            }
            else if (type == Type.Flick)
            {
                var newNote = Instantiate(Instance.prefabs[2], Instance.transform.GetChild(2));
                newNote.GetComponent<Note>().InitNote(type, EditorManager.GetAudioPCMTime(), new Vector2(0.5f, 0.5f));

                var newPos = Camera.main.ViewportToWorldPoint(new Vector2(0.5f, 0.5f));
                newNote.transform.position = new Vector3(newPos.x, newPos.y, 1f);
                newNote.GetComponent<Note>().RefreshState();

                Instance.FlickNotes.Add(newNote.GetComponent<Note>());
            }

            Global.IsSaved = false;
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
                var newNote = Instantiate(Instance.prefabs[0], Instance.transform.GetChild(0));
                newNote.GetComponent<Note>().InitNote(line.type, line.time, line.position);

                var newPos = Camera.main.ViewportToWorldPoint(line.position);
                newNote.transform.position = new Vector3(newPos.x, newPos.y, 1f);
                newNote.GetComponent<Note>().RefreshState();

                Instance.TapNotes.Add(newNote.GetComponent<Note>());
            }
            else if (line.type == Type.Hold)
            {
                var newNote = Instantiate(Instance.prefabs[1], Instance.transform.GetChild(1));
                newNote.GetComponent<Note>().InitNote(line.type, line.time, line.position, line.endTime);

                var newPos = Camera.main.ViewportToWorldPoint(line.position);
                newNote.transform.position = new Vector3(newPos.x, newPos.y, 1f);
                newNote.GetComponent<Note>().RefreshState();

                Instance.HoldNotes.Add(newNote.GetComponent<Note>());
            }
            else if (line.type == Type.Flick)
            {
                var newNote = Instantiate(Instance.prefabs[2], Instance.transform.GetChild(2));
                newNote.GetComponent<Note>().InitNote(line.type, line.time, line.position);

                var newPos = Camera.main.ViewportToWorldPoint(line.position);
                newNote.transform.position = new Vector3(newPos.x, newPos.y, 1f);
                newNote.GetComponent<Note>().RefreshState();

                Instance.FlickNotes.Add(newNote.GetComponent<Note>());
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

        public static List<Line> GetAllNotes()
        {
            List<Note> allNotes = Instance.TapNotes;
            allNotes.AddRange(Instance.HoldNotes);
            allNotes.AddRange(Instance.FlickNotes);
            
            List<Line> allLines = new List<Line>();
            foreach (var note in allNotes)
            {
                switch (note.type)
                {
                    case Type.Tap:
                        allLines.Add(new Line(note.type, note.time, note.position));
                        break;
                    case Type.Hold:
                        allLines.Add(new Line(note.type, note.time, note.position, note.endTime));
                        break;
                    case Type.Flick:
                        allLines.Add(new Line(note.type, note.time, note.position));
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
            }
            catch (Exception e)
            {
                MsgBoxManager.ShowMessage(MsgType.Error, "Error", e.Message);
            }
        }
    }

}