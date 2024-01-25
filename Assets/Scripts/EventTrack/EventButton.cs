using UnityEngine;
using QFramework;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Larvend.Gameplay;

namespace Larvend
{
    public class EventButton : MonoBehaviour , IController , IPointerClickHandler
    {
        private EventTrackModel mModel;
        [SerializeField]
        private EventButtonData mData;
        [SerializeField]
        private EventGroup mGroup;

        private Button mButton;
        private Image mImage;

        void Awake()
        {
            mModel = this.GetModel<EventTrackModel>();

            mButton = GetComponent<Button>();
            mButton.onClick.AddListener(() => {Debug.Log($"{mData.group.Id}, {mData.group.Pcm}, {mData.Id}");});

            mImage = GetComponent<Image>();
        }

        public void Init(EventGroup group)
        {
            mGroup = group;
        }

        public void SetData(EventButtonData data)
        {
            mData = data;
        }

        public void RefreshState()
        {
            switch (mData.type)
            {
                case BtnType.None:
                    mImage.color = Color.white;
                    break;
                case BtnType.Tap:
                    mImage.color = Color.yellow;
                    break;
                case BtnType.TapInIt:
                    mImage.color = new Color(1f, 1f, 0.5f);
                    break;
                case BtnType.Hold:
                    mImage.color = Color.green;
                    break;
                case BtnType.HoldInIt:
                    mImage.color = new Color(0.5f, 1f, 0.5f);
                    break;
                case BtnType.Holding:
                    mImage.color = new Color(0.5f, 1f, 0.5f, 0.5f);
                    break;
                case BtnType.Flick:
                    mImage.color = Color.blue;
                    break;
                case BtnType.FlickInIt:
                    mImage.color = new Color(0.5f, 1f, 1f);
                    break;
            }
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
            if (Global.IsHoldEditing && mModel.HoldStartButton != mData && mData.type is BtnType.None or BtnType.Holding)
            {
                Global.IsHoldEditing = false;
                // Debug.Log($"{mData.group.Id}, {mModel.HoldStartButton.group.Id}");
                if (mData.Id == mModel.HoldStartButton.Id)
                {
                    mModel.HoldEndButton = mData;
                    EventTrackController.GenerateHold();
                }
                return;
            }

            switch (mData.type)
            {
                case BtnType.None:
                    if (mData.note == null)
                    {
                        mData.note = NoteManager.CreateNoteWithoutRelate(Type.Tap, mData.group.Pcm);
                        mData.note.Relate(mData, BtnType.Tap);
                    }
                    else
                    {
                        // note.CancelRelation();
                        mData.note = NoteManager.CreateNoteWithoutRelate(Type.Tap, mData.group.Pcm)
                            .Inherit(mData.note);
                        mData.note.Relate(mData, BtnType.Tap);
                    }
                    break;
                case BtnType.Tap:
                    if (mData.note == null)
                    {
                        mData.note = NoteManager.CreateNoteWithoutRelate(Type.Hold, mData.group.Pcm);
                        mData.note.Relate(mData, BtnType.Hold);
                    }
                    else
                    {
                        // note.CancelRelation();
                        mData.note = NoteManager.CreateNoteWithoutRelate(Type.Hold, mData.group.Pcm)
                            .Inherit(mData.note);
                        mData.note.Relate(mData, BtnType.Hold);
                    }
                    break;
                case BtnType.TapInIt:
                    mData.type = BtnType.None;
                    if (mData.note != null)
                    {
                        mData.note.DeleteSelf();
                        mData.note = null;
                    }
                    break;
                case BtnType.Hold:
                    if (mData.note == null)
                    {
                        mData.note = NoteManager.CreateNoteWithoutRelate(Type.Flick, mData.group.Pcm);
                        mData.note.Relate(mData, BtnType.Flick);
                    }
                    else
                    {
                        // note.CancelRelation();
                        mData.note = NoteManager.CreateNoteWithoutRelate(Type.Flick, mData.group.Pcm)
                            .Inherit(mData.note);
                        mData.note.Relate(mData, BtnType.Flick);
                    }
                    break;
                case BtnType.HoldInIt:
                    mData.type = BtnType.None;
                    if (mData.note != null)
                    {
                        mData.note.DeleteSelf();
                        mData.note = null;
                    }
                    break;
                case BtnType.Flick:
                    mData.type = BtnType.None;
                    if (mData.note != null)
                    {
                        mData.note.DeleteSelf();
                        mData.note = null;
                    }
                    break;
                case BtnType.FlickInIt:
                    mData.type = BtnType.None;
                    if (mData.note != null)
                    {
                        mData.note.DeleteSelf();
                        mData.note = null;
                    }
                    break;
            }

            RefreshState();
        }

        private void OnRightClick()
        {
            if (mData.type is not BtnType.Hold or BtnType.Holding)
            {
                return;
            }
            Global.IsHoldEditing = true;
            mModel.HoldStartButton = this.mData.note.eventButtons[0];
        }

        public IArchitecture GetArchitecture()
        {
            return EventTrack.Interface;
        }

        private void OnDestroy()
        {
            mModel = null;
        }
    }
}