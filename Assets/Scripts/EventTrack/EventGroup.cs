using UnityEngine;
using QFramework;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

namespace Larvend
{
    public class EventGroup : MonoBehaviour , IController
    {
        private EventTrackModel mModel;
        [SerializeField]
        public EventGroupData Data;
        private List<EventButton> mButtons;

        public Image mImage;
        public TMP_Text mText;
        public GameObject IsSelected;

        private void Awake()
        {
            mModel = this.GetModel<EventTrackModel>();

            mImage = GetComponent<Image>();
            mImage.color = Color.white;

            mText = transform.Find("Time").GetComponent<TMP_Text>();

            IsSelected = transform.Find("IsSelected").gameObject;
            IsSelected.SetActive(false);

            TypeEventSystem.Global.Register<GroupRefreshEvent>(e => RefreshState()).UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        public void Init()
        {
            mButtons = new List<EventButton>();

            var buttons = transform.GetComponentsInChildren<EventButton>();
            int id = 0;
            foreach (var button in buttons)
            {
                button.SetData(Data.buttons[id]);
                button.Init(this);

                mButtons.Add(button);

                id++;
            }

            RefreshState();
        }

        public void RefreshState()
        {
            if (mModel.SelectedGroups.Find(e => e.Id == Data.Id) != null)
            {
                Select();
            }
            else
            {
                Deselect();
            }

            mText.text = $"{Data.Tick / 960 + 1} : {(Data.Tick % 960).ToString().PadLeft(3, '0')}";
            mText.color = Data.Tick % 960 == 0 ? Color.red : Color.black;

            if (mModel.CurrentEventGroup == Data)
            {
                SetAsCurrent();
            }
            else
            {
                DeCurrent();
            }

            foreach (var button in mButtons)
            {
                button.RefreshState();
            }
        }

        public void SetData(EventGroupData data)
        {
            Data = data;
        }

        private void SetAsCurrent()
        {
            mImage.color = Color.red;
            mText.color = Color.white;
        }

        private void DeCurrent()
        {
            mImage.color = Color.white;
            mText.color = Data.Tick % 960 == 0 ? Color.red : Color.black;
        }

        private void Select()
        {
            IsSelected.SetActive(true);
        }

        private void Deselect()
        {
            IsSelected.SetActive(false);
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