using Larvend;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
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
        public List<Note> Tap = new();
        public List<Note> SpeedAdjust = new();

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
        /// <param name="note">The note wanted to load.</param>
        public static void LoadNote(Note note)
        {
            if (note.type == Note.Type.Tap)
            {
                var newNote = Instantiate(_instance.prefabs[0], _instance.transform.GetChild(0));
                var newPos = Camera.main.ViewportToWorldPoint(note.position);
                newNote.transform.Translate(newPos.x, newPos.y, 1f);
                newNote.gameObject.SetActive(false);
                _instance.Tap.Add(note);
                _instance.TapNotes.Add(newNote);
            }
            else if (note.type == Note.Type.Hold)
            {
                var newNote = Instantiate(_instance.prefabs[1], _instance.transform.GetChild(1));
                var newPos = Camera.main.ViewportToWorldPoint(note.position);
                newNote.transform.Translate(newPos.x, newPos.y, 1f);
                newNote.gameObject.SetActive(false);
                _instance.HoldNotes.Add(newNote);
            }
            else if (note.type == Note.Type.Flick)
            {
                var newNote = Instantiate(_instance.prefabs[2], _instance.transform.GetChild(2));
                var newPos = Camera.main.ViewportToWorldPoint(note.position);
                newNote.transform.Translate(newPos.x, newPos.y, 1f);
                newNote.gameObject.SetActive(false);
                _instance.FlickNotes.Add(newNote);
            }
            else if (note.type == Note.Type.SpeedAdjust)
            {
                _instance.SpeedAdjust.Add(note);
            }
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