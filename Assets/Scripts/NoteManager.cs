using System;
using Larvend;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Larvend.Gameplay
{
    public class NoteManager : MonoBehaviour
    {
        private static NoteManager _instance;

        [SerializeField] private GameObject[] prefabs;

        public List<Note> TapNotes { get; private set; }
        public List<Note> HoldNotes { get; private set; }
        public List<Note> FlickNotes { get; private set; }
        public Line BaseSpeed;
        public List<Line> SpeedAdjust = new();

        private void Awake()
        {
            _instance = this;
            TapNotes = new();
            HoldNotes = new();
            FlickNotes = new();
        }

        public static NoteManager Instance
        {
            get
            {
                _instance ??= new NoteManager();
                return _instance;
            }
        }

        public static void ClearAllNotes()
        {
            Instance.TapNotes.Clear();
            Instance.HoldNotes.Clear();
            Instance.FlickNotes.Clear();
            Instance.SpeedAdjust.Clear();
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

            UIController.RefreshSpeedPanel(Instance.SpeedAdjust);
        }

        public static void CreateNote(Type type)
        {
            if (!Global.IsFileSelected)
            {
                return;
            }

            if (type == Type.Tap)
            {
                var newNote = Instantiate(_instance.prefabs[0], _instance.transform.GetChild(0));
                newNote.GetComponent<Note>().InitNote(type, EditorManager.GetAudioPCMTime(), new Vector2(0.5f, 0.5f));

                var newPos = Camera.main.ViewportToWorldPoint(new Vector2(0.5f, 0.5f));
                newNote.transform.position = new Vector3(newPos.x, newPos.y, 1f);
                newNote.GetComponent<Note>().RefreshState();

                _instance.TapNotes.Add(newNote.GetComponent<Note>());
            }
            else if (type == Type.Hold)
            {
                var newNote = Instantiate(_instance.prefabs[1], _instance.transform.GetChild(1));
                newNote.GetComponent<Note>().InitNote(type, EditorManager.GetAudioPCMTime(), new Vector2(0.5f, 0.5f), EditorManager.GetAudioPCMTime());

                var newPos = Camera.main.ViewportToWorldPoint(new Vector2(0.5f, 0.5f));
                newNote.transform.position = new Vector3(newPos.x, newPos.y, 1f);
                newNote.GetComponent<Note>().RefreshState();

                _instance.HoldNotes.Add(newNote.GetComponent<Note>());
            }
            else if (type == Type.Flick)
            {
                var newNote = Instantiate(_instance.prefabs[2], _instance.transform.GetChild(2));
                newNote.GetComponent<Note>().InitNote(type, EditorManager.GetAudioPCMTime(), new Vector2(0.5f, 0.5f));

                var newPos = Camera.main.ViewportToWorldPoint(new Vector2(0.5f, 0.5f));
                newNote.transform.position = new Vector3(newPos.x, newPos.y, 1f);
                newNote.GetComponent<Note>().RefreshState();

                _instance.FlickNotes.Add(newNote.GetComponent<Note>());
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
                var newNote = Instantiate(_instance.prefabs[0], _instance.transform.GetChild(0));
                newNote.GetComponent<Note>().InitNote(line.type, line.time, line.position);

                var newPos = Camera.main.ViewportToWorldPoint(line.position);
                newNote.transform.position = new Vector3(newPos.x, newPos.y, 1f);
                newNote.GetComponent<Note>().RefreshState();

                _instance.TapNotes.Add(newNote.GetComponent<Note>());
            }
            else if (line.type == Type.Hold)
            {
                var newNote = Instantiate(_instance.prefabs[1], _instance.transform.GetChild(1));
                newNote.GetComponent<Note>().InitNote(line.type, line.time, line.position, line.endTime);

                var newPos = Camera.main.ViewportToWorldPoint(line.position);
                newNote.transform.position = new Vector3(newPos.x, newPos.y, 1f);
                newNote.GetComponent<Note>().RefreshState();

                _instance.HoldNotes.Add(newNote.GetComponent<Note>());
            }
            else if (line.type == Type.Flick)
            {
                var newNote = Instantiate(_instance.prefabs[2], _instance.transform.GetChild(2));
                newNote.GetComponent<Note>().InitNote(line.type, line.time, line.position);

                var newPos = Camera.main.ViewportToWorldPoint(line.position);
                newNote.transform.position = new Vector3(newPos.x, newPos.y, 1f);
                newNote.GetComponent<Note>().RefreshState();

                _instance.FlickNotes.Add(newNote.GetComponent<Note>());
            }
            else if (line.type == Type.SpeedAdjust)
            {
                if (Instance.BaseSpeed == null && line.time == 0 && line.endTime == 0)
                {
                    Instance.BaseSpeed = new Line(line.targetBpm);
                    EditorManager.Instance.InitializeBPM(line.targetBpm);
                    return;
                }
                _instance.SpeedAdjust.Add(line);
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
                _instance.SpeedAdjust.Clear();
                foreach (var line in list)
                {
                    var note = ChartManager.ReadNote(line);
                    _instance.SpeedAdjust.Add(note);
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