using System;
using System.Collections;
using System.Collections.Generic;
using Larvend.Gameplay;

namespace Larvend
{
    public enum BtnType
    {
        None,
        Tap,
        TapInIt,
        Hold,
        Holding,
        HoldInIt,
        Flick,
        FlickInIt
    }
    [Serializable]
    public class EventGroupData
    {
        public int Id;
        public int Tick;
        public int Pcm;

        public List<EventButtonData> buttons;

        public EventGroupData()
        {

        }

        public EventGroupData(EventGroupData data)
        {
            Id = data.Id;
            Tick = data.Tick;
            Pcm = data.Pcm;

            buttons = new List<EventButtonData>();
            for (int i = 0; i < 6; i++)
            {
                var btn = new EventButtonData(data.buttons[i]);
                buttons.Add(btn);
            }
        }

        public EventGroupData(int id, int tick, int pcm)
        {
            Id = id;
            Tick = tick;
            Pcm = pcm + EditorManager.Instance.offset;

            buttons = new List<EventButtonData>();

            for (int i = 0; i < 6; i++)
            {
                var btn = new EventButtonData(this, i);
                buttons.Add(btn);
            }
        }

        public EventButtonData FindFirstEmptyButton()
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

        public void Cut(EventGroupData group)
        {
            foreach (var button in group.buttons)
            {
                if (button.type != BtnType.None && button.type != BtnType.Holding)
                {
                    var note = NoteManager.CreateNoteWithoutRelate(button.note.type, Pcm + EditorManager.Instance.offset).Copy(button.note);
                    var targetButton = FindFirstEmptyButton();
                    note.Relate(targetButton, button.type);

                    if (button.type == BtnType.Hold)
                    {
                        note.UpdateEndTime(Pcm + EditorManager.Instance.offset + button.note.endTime - button.note.time);
                        EventTrackController.PaintHold(note, targetButton, EventTrackController.GetModel().EventGroups[Id + button.note.eventButtons.Count - 1].buttons[targetButton.Id]);
                    }

                    button.note.DeleteSelf();
                }
            }
        }

        public void Copy(EventGroupData group)
        {
            foreach (var button in group.buttons)
            {
                if (button.type != BtnType.None && button.type != BtnType.Holding)
                {
                    var note = NoteManager.CreateNoteWithoutRelate(button.note.type, Pcm + EditorManager.Instance.offset).Copy(button.note);
                    var targetButton = FindFirstEmptyButton();
                    note.Relate(targetButton, button.type);

                    if (button.type == BtnType.Hold)
                    {
                        note.UpdateEndTime(Pcm + EditorManager.Instance.offset + button.note.endTime - button.note.time);
                        EventTrackController.PaintHold(note, targetButton, EventTrackController.GetModel().EventGroups[Id + button.note.eventButtons.Count - 1].buttons[targetButton.Id]);
                    }
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
                button.note?.DeleteSelf();
            }
        }
    }

    [System.Serializable]
    public class EventButtonData
    {
        public BtnType type;
        public int Id;
        public Note note;

        public EventGroupData group;

        public EventButtonData(EventButtonData data)
        {
            this.type = data.type;
            this.Id = data.Id;
            this.note = data.note;
        
            this.group = data.group;
        }

        public EventButtonData(EventGroupData group, int id)
        {
            this.type = BtnType.None;
            this.Id = id;
            this.note = null;
            
            this.group = group;
        }
    }
}