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

        public EventButton[] buttons;

        public void InitGroup(int id, int tick, int pcm)
        {
            Id = id;
            Tick = tick;
            Pcm = pcm;
            buttons = GetComponentsInChildren<EventButton>();
            foreach (var button in buttons)
            {
                button.InitButton();
            }
        }

        public void Cut(EventGroup group)
        {
            foreach (var button in group.buttons)
            {
                if (button.type != BtnType.None)
                {
                    var note = NoteManager.CreateNote(button.note.type, Pcm + EditorManager.Instance.offset).Copy(button.note);
                    note.Relate(FindFirstEmptyButton(), button.type);
                    button.note.DeleteSelf();
                }
            }
        }

        public void Copy(EventGroup group)
        {
            foreach (var button in group.buttons)
            {
                if (button.type != BtnType.None)
                {
                    var note = NoteManager.CreateNote(button.note.type, Pcm + EditorManager.Instance.offset).Copy(button.note);
                    note.Relate(FindFirstEmptyButton(), button.type);
                }
            }
        }

        public void InverseCopy(EventGroup group)
        {
            foreach (var button in group.buttons)
            {
                if (button.type != BtnType.None)
                {
                    var note = NoteManager.CreateNote(button.note.type, Pcm + EditorManager.Instance.offset).InverseCopy(button.note);
                    note.Relate(FindFirstEmptyButton(), button.type);
                }
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