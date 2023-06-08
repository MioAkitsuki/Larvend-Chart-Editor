using System;
using Larvend;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Events;

namespace Larvend.Gameplay
{
    public class NoteManager : MonoBehaviour
    {
        private static NoteManager _instance;

        [SerializeField] private GameObject[] prefabs;

        public List<Note> TapNotes { get; private set; }
        public List<Note> HoldNotes { get; private set; }
        public List<Note> FlickNotes { get; private set; }
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

        public static void RefreshAllNotes()
        {
            foreach (var note in Instance.TapNotes)
            {
                if ((EditorManager.GetAudioPCMTime() - note.time) / 44100f / (60f / EditorManager.GetBPM()) < -1)
                {
                    continue;
                }

                note.RefreshState();
            }

            foreach (var note in Instance.HoldNotes)
            {
                if ((EditorManager.GetAudioPCMTime() - note.time) / 44100f / (60f / EditorManager.GetBPM()) < -1)
                {
                    continue;
                }

                note.RefreshState();
            }

            foreach (var note in Instance.FlickNotes)
            {
                if ((EditorManager.GetAudioPCMTime() - note.time) / 44100f / (60f / EditorManager.GetBPM()) < -1)
                {
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

                _instance.TapNotes.Add(newNote.GetComponent<Note>());
            }
            else if (type == Type.Hold)
            {
                var newNote = Instantiate(_instance.prefabs[1], _instance.transform.GetChild(1));
                newNote.GetComponent<Note>().InitNote(type, EditorManager.GetAudioPCMTime(), new Vector2(0.5f, 0.5f), EditorManager.GetAudioPCMTime());

                var newPos = Camera.main.ViewportToWorldPoint(new Vector2(0.5f, 0.5f));
                newNote.transform.position = new Vector3(newPos.x, newPos.y, 1f);

                _instance.HoldNotes.Add(newNote.GetComponent<Note>());
            }
            else if (type == Type.Flick)
            {
                var newNote = Instantiate(_instance.prefabs[2], _instance.transform.GetChild(2));
                newNote.GetComponent<Note>().InitNote(type, EditorManager.GetAudioPCMTime(), new Vector2(0.5f, 0.5f));

                var newPos = Camera.main.ViewportToWorldPoint(new Vector2(0.5f, 0.5f));
                newNote.transform.position = new Vector3(newPos.x, newPos.y, 1f);

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

                _instance.TapNotes.Add(newNote.GetComponent<Note>());
            }
            else if (line.type == Type.Hold)
            {
                var newNote = Instantiate(_instance.prefabs[1], _instance.transform.GetChild(1));
                newNote.GetComponent<Note>().InitNote(line.type, line.time, line.position, line.endTime);

                var newPos = Camera.main.ViewportToWorldPoint(line.position);
                newNote.transform.position = new Vector3(newPos.x, newPos.y, 1f);

                _instance.HoldNotes.Add(newNote.GetComponent<Note>());
            }
            else if (line.type == Type.Flick)
            {
                var newNote = Instantiate(_instance.prefabs[2], _instance.transform.GetChild(2));
                newNote.GetComponent<Note>().InitNote(line.type, line.time, line.position);

                var newPos = Camera.main.ViewportToWorldPoint(line.position);
                newNote.transform.position = new Vector3(newPos.x, newPos.y, 1f);

                _instance.FlickNotes.Add(newNote.GetComponent<Note>());
            }
            else if (line.type == Type.SpeedAdjust)
            {
                _instance.SpeedAdjust.Add(line);
            }
        }

        public static List<Line> GetAllNotes()
        {
            MsgBoxManager.ShowMessage(MsgType.Error, "Unfinished Method", "Unfinished Method");
            return null;
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