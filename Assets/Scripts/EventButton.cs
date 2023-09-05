using Larvend.Gameplay;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;

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
    public class EventButton : MonoBehaviour , IPointerClickHandler
    {
        public BtnType type;
        public int Id;
        public Note note;

        public EventGroup group;
        private Button button;
        private Image image;

        private void Start()
        {
            
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                OnLeftClick();
            else if (eventData.button == PointerEventData.InputButton.Middle)
                Debug.Log("Middle click");
            else if (eventData.button == PointerEventData.InputButton.Right)
                OnRightClick();
        }

        private void OnLeftClick()
        {
            if (EventTrack.IsHoldEditing && EventTrack.startButton != this)
            {
                EventTrack.IsHoldEditing = false;
                Debug.Log($"{group.Id}, {EventTrack.startButton.group.Id}");
                if (this.Id == EventTrack.startButton.Id)
                {
                    EventTrack.endButton = this;
                    EventTrack.GenerateHold();
                }
                return;
            }

            switch (type)
            {
                case BtnType.None:
                    if (note == null)
                    {
                        note = NoteManager.CreateNote(Type.Tap, group.Pcm + EditorManager.Instance.offset);
                        note.Relate(this, BtnType.Tap);
                    }
                    else
                    {
                        // note.CancelRelation();
                        note = NoteManager.CreateNote(Type.Tap, group.Pcm + EditorManager.Instance.offset).Inherit(note);
                        note.Relate(this, BtnType.Tap);
                    }
                    break;
                case BtnType.Tap:
                    if (note == null)
                    {
                        note = NoteManager.CreateNote(Type.Hold, group.Pcm + EditorManager.Instance.offset);
                        note.Relate(this, BtnType.Hold);
                    }
                    else
                    {
                        // note.CancelRelation();
                        note = NoteManager.CreateNote(Type.Hold, group.Pcm + EditorManager.Instance.offset).Inherit(note);
                        note.Relate(this, BtnType.Hold);
                    }
                    break;
                case BtnType.TapInIt:
                    type = BtnType.None;
                    if (note != null)
                    {
                        note.DeleteSelf();
                        note = null;
                    }
                    break;
                case BtnType.Hold:
                    if (note == null)
                    {
                        note = NoteManager.CreateNote(Type.Flick, group.Pcm + EditorManager.Instance.offset);
                        note.Relate(this, BtnType.Flick);
                    }
                    else
                    {
                        // note.CancelRelation();
                        note = NoteManager.CreateNote(Type.Flick, group.Pcm + EditorManager.Instance.offset).Inherit(note);
                        note.Relate(this, BtnType.Flick);
                    }
                    break;
                case BtnType.HoldInIt:
                    type = BtnType.None;
                    if (note != null)
                    {
                        note.DeleteSelf();
                        note = null;
                    }
                    break;
                case BtnType.Flick:
                    type = BtnType.None;
                    if (note != null)
                    {
                        note.DeleteSelf();
                        note = null;
                    }
                    break;
                case BtnType.FlickInIt:
                    type = BtnType.None;
                    if (note != null)
                    {
                        note.DeleteSelf();
                        note = null;
                    }
                    break;
            }
        }

        private void OnRightClick()
        {
            if (type is not BtnType.Hold or BtnType.Holding)
            {
                return;
            }
            EventTrack.IsHoldEditing = true;
            EventTrack.startButton = this.note.eventButtons[0];
        }
        
        public void Refresh()
        {
            switch (type)
            {
                case BtnType.None:
                    image.color = Color.white;
                    break;
                case BtnType.Tap:
                    image.color = Color.yellow;
                    break;
                case BtnType.TapInIt:
                    image.color = new Color(1f, 1f, 0.5f);
                    break;
                case BtnType.Hold:
                    image.color = Color.green;
                    break;
                case BtnType.HoldInIt:
                    image.color = new Color(0.5f, 1f, 0.5f);
                    break;
                case BtnType.Holding:
                    image.color = new Color(0.5f, 1f, 0.5f, 0.5f);
                    break;
                case BtnType.Flick:
                    image.color = Color.blue;
                    break;
                case BtnType.FlickInIt:
                    image.color = new Color(0.5f, 1f, 1f);
                    break;
            }
        }

        public void InitButton()
        {
            type = BtnType.None;

            button = GetComponent<Button>();
            image = GetComponent<Image>();
            group = transform.parent.GetComponent<EventGroup>();
        }
    }
}