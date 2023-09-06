using Larvend.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace Larvend
{
    public class EventGroup : MonoBehaviour
    {
        public int Id;
        public int Tick;
        public int Pcm;
        public Image image;
        public GameObject IsSelected;

        public EventButton[] buttons;

        public void InitGroup(int id, int tick, int pcm)
        {
            Id = id;
            Tick = tick;
            Pcm = pcm;

            image = GetComponent<Image>();
            image.color = Color.white;

            IsSelected = transform.Find("IsSelected").gameObject;
            IsSelected.SetActive(false);

            buttons = GetComponentsInChildren<EventButton>();
            foreach (var button in buttons)
            {
                button.InitButton();
            }
        }

        public EventGroup NextGroup()
        {
            if (Id < EventTrack.Instance.EventGroups.Count - 1)
            {
                return EventTrack.Instance.EventGroups[Id + 1] ?? this;
            }
            return null;
        }

        public EventGroup PrevGroup()
        {
            Debug.Log($"{Id}, {EventTrack.Instance.EventGroups[Id - 1]}");
            if (Id > 0)
            {
                return EventTrack.Instance.EventGroups[Id - 1] ?? this;
            }
            return null;
        }

        public void Cut(EventGroup group)
        {
            foreach (var button in group.buttons)
            {
                if (button.type != BtnType.None && button.type != BtnType.Holding)
                {
                    var note = NoteManager.CreateNote(button.note.type, Pcm + EditorManager.Instance.offset).Copy(button.note);
                    var targetButton = FindFirstEmptyButton();
                    if (button.type == BtnType.Hold)
                    {
                        note.UpdateEndTime(Pcm + EditorManager.Instance.offset + button.note.endTime - button.note.time);
                        EventTrack.PaintHold(note, targetButton, EventTrack.Instance.EventGroups[Id + button.note.eventButtons.Count].FindButtonById(targetButton.Id));
                    }
                    note.Relate(targetButton, button.type);
                    button.note.DeleteSelf();
                }
            }
        }

        public void Copy(EventGroup group)
        {
            foreach (var button in group.buttons)
            {
                if (button.type != BtnType.None && button.type != BtnType.Holding)
                {
                    var note = NoteManager.CreateNote(button.note.type, Pcm + EditorManager.Instance.offset).Copy(button.note);
                    var targetButton = FindFirstEmptyButton();
                    if (button.type == BtnType.Hold)
                    {
                        note.UpdateEndTime(Pcm + EditorManager.Instance.offset + button.note.endTime - button.note.time);
                        EventTrack.PaintHold(note, targetButton, EventTrack.Instance.EventGroups[Id + button.note.eventButtons.Count].FindButtonById(targetButton.Id));
                    }
                    note.Relate(targetButton, button.type);
                }
            }
        }

        public void HorizontalMirror()
        {
            foreach (var button in buttons)
            {
                if (button.type != BtnType.None && button.type != BtnType.Holding)
                {
                    button.note.HorizontalMirror();
                }
            }
        }

        public void VerticalMirror()
        {
            foreach (var button in buttons)
            {
                if (button.type != BtnType.None && button.type != BtnType.Holding)
                {
                    button.note.VerticalMirror();
                }
            }
        }

        public void ClearAll()
        {
            foreach (var button in buttons)
            {
                button.DeleteSelf();
            }
        }

        public EventButton FindFirstEmptyButton()
        {
            foreach (var button in buttons)
            {
                if (button.type == BtnType.None)
                {
                    return button;
                }
            }
            return null;
        }

        public EventButton FindButtonById(int id)
        {
            return buttons[id];
        }
    }
}