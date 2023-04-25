using Larvend;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class NoteManager : MonoBehaviour
{
    private static NoteManager _instance;

    [SerializeField]
    private GameObject[] prefabs;

    public List<GameObject> TapNotes { get; private set; }
    public List<GameObject> HoldNotes { get; private set; }
    public List<GameObject> FlickNotes { get; private set; }

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
        Debug.Log("Load Note");
        if (note.type == Note.Type.Tap)
        {
            var newNote = Instantiate(_instance.prefabs[0], _instance.transform.GetChild(0));
            var newPos = Camera.main.ViewportToWorldPoint(note.position);
            newNote.transform.Translate(newPos.x, newPos.y, 1f);
            newNote.gameObject.SetActive(false);
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
            
        }
    }
}
