using UnityEngine;
using QFramework;
using System;
using System.Collections.Generic;
using Larvend.Gameplay;
using UnityEngine.UI;

namespace Larvend
{
    public class EventTrackController : MonoBehaviour , IController
    {
        private EventTrackModel mModel;
        private static EventTrackController mSelf;
        private InfiniteScrollView mInfiniteScrollView;
        public GameObject[] Prefabs;
        private int mSelectOptions = 0;

        private List<EventGroup> mEventGroups;

        void Start()
        {
            mModel = this.GetModel<EventTrackModel>();
            mSelf = this;
            mInfiniteScrollView = transform.parent.parent.GetComponent<InfiniteScrollView>();
        }

        void Update()
        {
            if (Global.IsPlaying && mModel.SelectedGroups.Count > 0)
            {
                DeselectGroup();
            }

            if (Input.GetKeyUp(KeyCode.D) && !Global.IsPlaying && !Global.IsDialoging)
            {
                SelectGroup(mModel.CurrentEventGroup);
            }
            if (Input.GetKeyUp(KeyCode.Escape) && !Global.IsPlaying && !Global.IsDialoging)
            {
                DeselectGroup();
            }

            if (Input.GetKeyUp(KeyCode.C) && mModel.SelectedGroups.Count > 0 && !Global.IsPlaying && !Global.IsDialoging)
            {
                mSelectOptions = 1;
            }
            if (Input.GetKeyUp(KeyCode.X) && mModel.SelectedGroups.Count > 0 && !Global.IsPlaying && !Global.IsDialoging)
            {
                mSelectOptions = 2;
            }
            if (Input.GetKeyUp(KeyCode.V) && !Global.IsPlaying && !Global.IsDialoging && mModel.SelectedGroups.Count > 0)
            {
                if (mSelectOptions == 1)
                {
                    var tmp = mModel.CurrentEventGroup;
                    for (int i = 0; i < mModel.SelectedGroups.Count; i++)
                    {
                        tmp.Copy(mModel.SelectedGroups[i]);
                        tmp = mModel.EventGroups[tmp.Id + 1];
                    }
                }
                else if (mSelectOptions == 2)
                {
                    var tmp = mModel.CurrentEventGroup;
                    for (int i = 0; i < mModel.SelectedGroups.Count; i++)
                    {
                        tmp.Cut(mModel.SelectedGroups[i]);
                        tmp = mModel.EventGroups[tmp.Id + 1];
                    }
                }

                mSelectOptions = 0;
                DeselectGroup();
                TypeEventSystem.Global.Send(new GroupRefreshEvent());
            }

            if (Input.GetKeyUp(KeyCode.Delete) && !Global.IsPlaying && !Global.IsDialoging && mModel.SelectedGroups.Count > 0)
            {
                MsgBoxManager.ShowMessage(MsgType.Warning, "Warning", Localization.GetString("DeleteEvents"), () => {
                    foreach (var group in mModel.SelectedGroups)
                    {
                        group.ClearAll();
                    }
                });
            }

            if (Input.GetKeyUp(KeyCode.N) && !Global.IsPlaying && !Global.IsDialoging && mModel.SelectedGroups.Count > 0)
            {
                foreach (var group in mModel.SelectedGroups)
                {
                    group.HorizontalMirror();
                }
            }
            if (Input.GetKeyUp(KeyCode.M) && !Global.IsPlaying && !Global.IsDialoging && mModel.SelectedGroups.Count > 0)
            {
                foreach (var group in mModel.SelectedGroups)
                {
                    group.VerticalMirror();
                }
            }
        }

        public static void RefreshPanel()
        {
            int ticks = 0, maxTicks = EditorManager.GetMaxTicks();
            int step = Int32.Parse(UIController.Instance.stepInputField.text);

            mSelf.mInfiniteScrollView.DestroyAll();
            mSelf.mModel.Reset();

            while (ticks < maxTicks)
            {
                for (int i = 0; i < step; i++)
                {
                    mSelf.mModel.EventGroups.Add(new EventGroupData(mSelf.mModel.EventGroups.Count, ticks, FromTickToPcm(ticks)));
                    ticks += 960 / step;
                }
            }

            mSelf.mModel.CurrentEventGroup = mSelf.mModel.EventGroups[0];

            mSelf.InitScrollView();
            NoteManager.ReRelateAllNotes();
        }

        private void SelectGroup(EventGroupData target)
        {
            if (mModel.SelectedGroups.Count > 1)
            {
                DeselectGroup();
                mModel.SelectedGroups.Add(new EventGroupData(target));
            }
            else if (mModel.SelectedGroups.Count == 1)
            {
                if (mModel.SelectedGroups[0].Id > target.Id)
                {
                    for (int i = target.Id; i <= mModel.SelectedGroups[0].Id - 1; i++)
                    {
                        mModel.SelectedGroups.Add(new EventGroupData(mModel.EventGroups[i]));
                    }
                }
                else if (mModel.SelectedGroups[0].Id < target.Id)
                {
                    for (int i = mModel.SelectedGroups[0].Id + 1; i <= target.Id; i++)
                    {
                        mModel.SelectedGroups.Add(new EventGroupData(mModel.EventGroups[i]));
                    }
                }
            }
            else
            {
                mModel.SelectedGroups.Add(new EventGroupData(target));
            }

            TypeEventSystem.Global.Send(new GroupRefreshEvent());
        }
        
        private void DeselectGroup()
        {
            mModel.SelectedGroups.Clear();

            TypeEventSystem.Global.Send(new GroupRefreshEvent());
        }
        

        public static void GenerateHold()
        {
            var note = mSelf.mModel.HoldStartButton.note;

            note.CancelRelation();
            note.Relate(mSelf.mModel.HoldStartButton, BtnType.Hold);

            note.UpdateEndTime(mSelf.mModel.HoldEndButton.group.Pcm + EditorManager.Instance.offset);

            for (int i = mSelf.mModel.HoldStartButton.group.Id + 1; i <= mSelf.mModel.HoldEndButton.group.Id; i++)
            {
                var btn = mSelf.mModel.EventGroups[i].buttons[mSelf.mModel.HoldStartButton.Id];
                note.Relate(btn, BtnType.Holding);
            }

            TypeEventSystem.Global.Send(new GroupRefreshEvent());
        }

        public static void LocateGroupByTick(int target)
        {
            if (mSelf.mModel.DisplayedEventGroups.Count == 0)
            {
                return;
            }

            int targetId = FromTickToId(target);
            if (targetId < mSelf.mModel.DisplayedEventGroups[0].Data.Id
                || targetId > mSelf.mModel.DisplayedEventGroups[mSelf.mModel.DisplayedEventGroups.Count - 1].Data.Id)
            {
                mSelf.mInfiniteScrollView.GenerateGroups(targetId);
            }
            mSelf.mModel.CurrentEventGroup = mSelf.mModel.EventGroups[targetId];

            TypeEventSystem.Global.Send(new GroupRefreshEvent());
        }
        
        public static void PaintHold(Note note, EventButtonData start, EventButtonData end)
        {
            for (int i = start.group.Id + 1; i <= end.group.Id; i++)
            {
                var btn = mSelf.mModel.EventGroups[i].buttons[start.Id];

                var originNote = btn.note;
                var originType = btn.type;
                if (btn.type != BtnType.None)
                {
                    originNote.CancelRelation();
                    note.Relate(btn, BtnType.Holding);
                    originNote.Relate(btn.group.FindFirstEmptyButton(), originType);
                }
                else
                {
                    note.Relate(btn, BtnType.Holding);
                }
            }

            TypeEventSystem.Global.Send(new GroupRefreshEvent());
        }

        private void InitScrollView()
        {
            mInfiniteScrollView.SetTotalCount(mModel.EventGroups.Count);
            mInfiniteScrollView.Init();
        }

        public static int FromPcmToTick(int pcm)
        {
            int targetTick = 0;
            pcm -= EditorManager.Instance.offset;

            foreach (var item in NoteManager.Instance.PcmDict)
            {
                if (item.Key.range * item.Value < pcm)
                {
                    pcm -= Mathf.RoundToInt(item.Key.range * item.Value);
                    targetTick += Mathf.RoundToInt(item.Key.range * 960);
                }
                else
                {
                    targetTick += Mathf.RoundToInt(pcm / item.Value * 960);
                    break;
                }
            }

            return targetTick;
        }

        public static EventTrackModel GetModel()
        {
            return mSelf.mModel;
        }

        public static int FromTickToId(int tick)
        {
            return Mathf.RoundToInt(tick / (960f / Int32.Parse(UIController.Instance.stepInputField.text)));
        }

        public static int FromTickToPcm(int tick)
        {
            int targetPcm = 0;
            foreach (var item in NoteManager.Instance.PcmDict)
            {
                if (item.Key.IsIn(tick / 960f + 1))
                {
                    targetPcm += Mathf.RoundToInt((tick / 960f - item.Key.start + 1) * item.Value);
                    break;
                }
                else
                {
                    targetPcm += Mathf.RoundToInt(item.Key.range * item.Value);
                }
            }

            return targetPcm;
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