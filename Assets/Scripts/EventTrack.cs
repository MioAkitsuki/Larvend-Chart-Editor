using UnityEngine;
using System.Collections;
using TMPro;
using Larvend.Gameplay;
using System;
using System.Collections.Generic;

namespace Larvend
{
    public class EventTrack : MonoBehaviour
    {
        public static EventTrack Instance { get; set; }

        public int MaxTick;
        public EventGroup CurrentGroup;
        public List<EventGroup> SelectGroups;
        public int SelectOptions;  // 0 for None, 1 for Copy and 2 for Cut.
        public GameObject[] Prefabs;
        public List<EventGroup> EventGroups;
        private Transform content;
        public static bool IsHoldEditing;
        public static EventButton startButton;
        public static EventButton endButton;

        private void Start()
        {
            Instance = this;
            EventGroups = new List<EventGroup>();
            SelectGroups = new List<EventGroup>();
            content = transform.Find("Scroll View/Viewport/Content");

            IsHoldEditing = false;

            SelectOptions = 0;
        }

        private void Update()
        {
            if (Global.IsPlaying && SelectGroups.Count > 0)
            {
                DeselectGroup();
            }

            if (Input.GetKeyUp(KeyCode.D) && !Global.IsPlaying)
            {
                SelectGroup(CurrentGroup);
            }
            if (Input.GetKeyUp(KeyCode.Escape) && !Global.IsPlaying)
            {
                DeselectGroup();
            }

            if (Input.GetKeyUp(KeyCode.C) && SelectGroups.Count > 0 && !Global.IsPlaying)
            {
                SelectOptions = 1;
            }
            if (Input.GetKeyUp(KeyCode.X) && SelectGroups.Count > 0 && !Global.IsPlaying)
            {
                SelectOptions = 2;
            }
            if (Input.GetKeyUp(KeyCode.V) && !Global.IsPlaying && SelectGroups.Count > 0)
            {
                if (SelectOptions == 1)
                {
                    var tmp = CurrentGroup;
                    for (int i = 0; i < SelectGroups.Count; i++)
                    {
                        tmp.Copy(SelectGroups[i]);
                        tmp = tmp.NextGroup();
                    }
                }
                else if (SelectOptions == 2)
                {
                    var tmp = CurrentGroup;
                    for (int i = 0; i < SelectGroups.Count; i++)
                    {
                        tmp.Cut(SelectGroups[i]);
                        tmp = tmp.NextGroup();
                    }
                    SelectOptions = 0;
                    DeselectGroup();
                }
            }

            if (Input.GetKeyUp(KeyCode.Delete) && !Global.IsPlaying && SelectGroups.Count > 0)
            {
                MsgBoxManager.ShowMessage(MsgType.Warning, "Warning", Localization.GetString("DeleteEvents"), () => {
                    foreach (var group in SelectGroups)
                    {
                        group.ClearAll();
                    }
                });
            }

            if (Input.GetKeyUp(KeyCode.N) && !Global.IsPlaying && SelectGroups.Count > 0)
            {
                foreach (var group in SelectGroups)
                {
                    group.HorizontalMirror();
                }
            }
            if (Input.GetKeyUp(KeyCode.M) && !Global.IsPlaying && SelectGroups.Count > 0)
            {
                foreach (var group in SelectGroups)
                {
                    group.VerticalMirror();
                }
            }
        }

        public void NextGroup()
        {
            if (CurrentGroup != null && CurrentGroup.Id < EventGroups.Count - 1)
            {
                SetCurrentGroup( EventGroups[CurrentGroup.Id + 1] ?? CurrentGroup );
            }
        }

        public void PrevGroup()
        {
            if (CurrentGroup != null && CurrentGroup.Id > 0)
            {
                SetCurrentGroup( EventGroups[CurrentGroup.Id - 1] ?? CurrentGroup );
            }
        }

        public void LocateGroupByTick(int tick)
        {
            foreach (var group in EventGroups)
            {
                if (Math.Abs(tick - group.Tick) <= 2)
                {
                    SetCurrentGroup(group);
                    return;
                }

                if (group.Tick - tick > 2)
                {
                    SetCurrentGroup(group.PrevGroup());
                    return;
                }
            }
        }

        public EventGroup FindGroupByTick(int tick)
        {
            foreach (var group in EventGroups)
            {
                if (Math.Abs(group.Tick - tick) < 2)
                {
                    return group;
                }

                if (group.Tick > tick + 120)
                {
                    return null;
                }
            }
            
            return null;
        }

        private void SelectGroup(EventGroup target)
        {
            if (SelectGroups.Count > 1)
            {
                DeselectGroup();
                SelectGroups.Add(target);
                target.IsSelected.SetActive(true);
            }
            else if (SelectGroups.Count == 1)
            {
                if (SelectGroups[0].Id > target.Id)
                {
                    for (int i = target.Id; i <= SelectGroups[0].Id - 1; i++)
                    {
                        SelectGroups.Add(EventGroups[i]);
                        EventGroups[i].IsSelected.SetActive(true);
                    }
                }
                else if (SelectGroups[0].Id < target.Id)
                {
                    for (int i = SelectGroups[0].Id + 1; i <= target.Id; i++)
                    {
                        SelectGroups.Add(EventGroups[i]);
                        EventGroups[i].IsSelected.SetActive(true);
                    }
                }
            }
            else
            {
                SelectGroups.Add(target);
                target.IsSelected.SetActive(true);
            }
        }
        
        private void DeselectGroup()
        {
            foreach (var group in SelectGroups)
            {
                group.IsSelected.SetActive(false);
            }
            SelectGroups.Clear();
        }

        public static void RefreshPanel()
        {
            int ticks = 0, maxTicks = EditorManager.GetMaxTicks();
            int step = Int32.Parse(UIController.Instance.stepInputField.text);

            Instance.DeleteAll();
            Instance.DeselectGroup();

            while (ticks < maxTicks)
            {
                Instance.GenerateBar(ticks);
                for (int i = 0; i < step; i++)
                {
                    Instance.GenerateGroup(ticks);
                    ticks += 960 / step;
                }
            }

            Instance.MaxTick = ticks;
            Instance.SetCurrentGroup(Instance.EventGroups[0]);

            NoteManager.ReRelateAllNotes();
        }

        public void SetCurrentGroup(EventGroup target)
        {
            if (CurrentGroup)
            {
                CurrentGroup.image.color = Color.white;
            }
            CurrentGroup = target;
            CurrentGroup.image.color = Color.red;
        }

        private void DeleteAll()
        {
            for (int i = 0; i < content.childCount; i++)
            {
                Destroy(content.GetChild(i).gameObject);
            }
            EventGroups.Clear();
        }

        private void GenerateBar(int ticks)
        {
            GameObject bar = Instantiate(Prefabs[0], content);
            bar.GetComponentInChildren<TMP_Text>().text = $"{ticks / 960 + 1} : 000";
        }

        private void GenerateGroup(int ticks)
        {
            GameObject group = Instantiate(Prefabs[1], content);
            var newEventGroup = group.GetComponent<EventGroup>();
            newEventGroup.InitGroup(EventGroups.Count, ticks, FromTickToPcm(ticks));

            EventGroups.Add(newEventGroup);
        }
        
        public static void GenerateHold()
        {
            var note = startButton.note;
            note.CancelRelation();
            note.Relate(startButton, BtnType.Hold);
            note.UpdateEndTime(endButton.group.Pcm + EditorManager.Instance.offset);
            for (int i = startButton.group.Id + 1; i <= endButton.group.Id; i++)
            {
                var btn = Instance.EventGroups[i].transform.Find($"{startButton.Id}").GetComponent<EventButton>();
                note.Relate(btn, BtnType.Holding);
            }
        }

        public static void PaintHold(Note note, EventButton start, EventButton end)
        {
            for (int i = start.group.Id + 1; i <= end.group.Id; i++)
            {
                var btn = Instance.EventGroups[i].FindButtonById(start.Id);
                if (btn.type != BtnType.None)
                {
                    btn.note.CancelRelation();
                    btn.note.Relate(btn.group.FindFirstEmptyButton(), btn.type);
                    btn.CancelRelation();
                }
                note.Relate(btn, BtnType.Holding);
            }
        }

        private static int FromTickToPcm(int tick)
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
    }
}