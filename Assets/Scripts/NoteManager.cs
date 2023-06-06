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

        public List<GameObject> TapNotes { get; private set; }
        public List<GameObject> HoldNotes { get; private set; }
        public List<GameObject> FlickNotes { get; private set; }
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

        /// <summary>
        /// Load a certain note into the scene. At its position and disabled by default.
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
                newNote.transform.Translate(newPos.x, newPos.y, 1f);
                newNote.gameObject.SetActive(false);

                _instance.TapNotes.Add(newNote);
            }
            else if (line.type == Type.Hold)
            {
                var newNote = Instantiate(_instance.prefabs[1], _instance.transform.GetChild(1));
                newNote.GetComponent<Note>().InitNote(line.type, line.time, line.position, line.endTime);

                var newPos = Camera.main.ViewportToWorldPoint(line.position);
                newNote.transform.Translate(newPos.x, newPos.y, 1f);
                newNote.gameObject.SetActive(false);

                _instance.HoldNotes.Add(newNote);
            }
            else if (line.type == Type.Flick)
            {
                var newNote = Instantiate(_instance.prefabs[2], _instance.transform.GetChild(2));
                newNote.GetComponent<Note>().InitNote(line.type, line.time, line.position);

                var newPos = Camera.main.ViewportToWorldPoint(line.position);
                newNote.transform.Translate(newPos.x, newPos.y, 1f);
                newNote.gameObject.SetActive(false);

                _instance.FlickNotes.Add(newNote);
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
            _instance.SpeedAdjust.Clear();
            foreach (var line in list)
            {
                var note = ChartManager.ReadNote(line);
                _instance.SpeedAdjust.Add(note);
            }
        }
    }

}