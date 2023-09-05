using UnityEngine;
using System.Collections;
using TMPro;
using Larvend.Gameplay;
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Larvend
{
    public class EventTrack : MonoBehaviour
    {
        public static EventTrack Instance { get; set; }

        public int MaxTick;
        public int copyStart = -1;
        public int copyEnd = -1;
        public List<EventGroup> copyGroups;
        public GameObject[] Prefabs;
        public List<EventGroup> eventGroups;
        private Transform content;
        public static bool IsHoldEditing;
        public static EventButton startButton;
        public static EventButton endButton;

        private void Start()
        {
            Instance = this;
            eventGroups = new List<EventGroup>();
            copyGroups = new List<EventGroup>();
            content = transform.Find("Scroll View/Viewport/Content");

            IsHoldEditing = false;
        }

        private void Update()
        {
            if (Input.GetKeyUp(KeyCode.C) && !Global.IsPlaying && copyStart == -1)
            {
                copyStart = (EditorManager.Instance.beatTick[0] - 1) * 960 + EditorManager.Instance.beatTick[1];
            }
            else if (Input.GetKeyUp(KeyCode.C) && !Global.IsPlaying && copyStart != -1)
            {
                copyEnd = (EditorManager.Instance.beatTick[0] - 1) * 960 + EditorManager.Instance.beatTick[1];
                CopyNotes(copyStart, copyEnd);
                copyStart = -1;
                copyEnd = -1;
            }

            if (Input.GetKeyUp(KeyCode.V) && !Global.IsPlaying && copyGroups.Count > 0)
            {
                int count = 0;
                foreach (var group in eventGroups)
                {
                    if (count >= copyGroups.Count)
                    {
                        break;
                    }

                    if (group.Tick - (EditorManager.Instance.beatTick[0] - 1) * 960 - EditorManager.Instance.beatTick[1] is > -5)
                    {
                        group.Copy(copyGroups[count]);
                        count++;
                    }
                }
            }
            if (Input.GetKeyUp(KeyCode.B) && !Global.IsPlaying && copyGroups.Count > 0)
            {
                int count = 0;
                foreach (var group in eventGroups)
                {
                    if (count >= copyGroups.Count)
                    {
                        break;
                    }

                    if (group.Tick - (EditorManager.Instance.beatTick[0] - 1) * 960 - EditorManager.Instance.beatTick[1] is > -5)
                    {
                        group.InverseCopy(copyGroups[count]);
                        count++;
                    }
                }
            }
            if (Input.GetKeyUp(KeyCode.X) && !Global.IsPlaying && copyGroups.Count > 0)
            {
                int count = 0;
                foreach (var group in eventGroups)
                {
                    if (count >= copyGroups.Count)
                    {
                        break;
                    }

                    if (group.Tick - (EditorManager.Instance.beatTick[0] - 1) * 960 - EditorManager.Instance.beatTick[1] is > -5)
                    {
                        group.Cut(copyGroups[count]);
                        count++;
                    }
                }
            }
        } 

        private void CopyNotes(int start, int end)
        {
            copyGroups.Clear();
            foreach (var group in eventGroups)
            {
                if (group.Tick >= start - 2 && group.Tick <= end + 2)
                {
                    copyGroups.Add(group);
                }
            }
        }

        public static void RefreshPanel()
        {
            int ticks = 0, maxTicks = EditorManager.GetMaxTicks();
            int step = Int32.Parse(UIController.Instance.stepInputField.text);

            Instance.DeleteAll();

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

            NoteManager.ReRelateAllNotes();
        }

        private void DeleteAll()
        {
            for (int i = 0; i < content.childCount; i++)
            {
                Destroy(content.GetChild(i).gameObject);
            }
            eventGroups.Clear();
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
            newEventGroup.InitGroup(eventGroups.Count, ticks, FromTickToPcm(ticks));

            eventGroups.Add(newEventGroup);
        }
        
        public static void GenerateHold()
        {
            var note = startButton.note;
            note.CancelRelation();
            note.Relate(startButton, BtnType.Hold);
            note.UpdateEndTime(endButton.group.Pcm + EditorManager.Instance.offset);
            for (int i = startButton.group.Id + 1; i <= endButton.group.Id; i++)
            {
                var btn = Instance.eventGroups[i].transform.Find($"{startButton.Id}").GetComponent<EventButton>();
                note.Relate(btn, BtnType.Holding);
            }
        }

        public static void PaintHold(Note note, EventButton start, EventButton end)
        {
            for (int i = start.group.Id + 1; i <= end.group.Id; i++)
            {
                var btn = Instance.eventGroups[i].transform.Find($"{start.Id}").GetComponent<EventButton>();
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