using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Larvend.Gameplay
{
    public enum MsgType
    {
        Info,
        Warning,
        Error,
    }

    public struct Msg
    {
        public string title;
        public string msg;
        public MsgType type;
        public Callback callback;

        public Msg(MsgType type, string title, string msg, params Callback[] callbacks)
        {
            this.title = title;
            this.msg = msg;
            this.type = type;
            this.callback = null;

            if (callbacks.Length > 0)
            {
                this.callback = callbacks[0];
            }
        }
    }

    public class MsgBoxManager : MonoBehaviour
    {
        public static MsgBoxManager Instance { get; private set; }

        [SerializeField]
        private GameObject[] msgBoxPrefabs;

        private DialogBox dialogBox;
        private List<Msg> messages = new List<Msg>();

        public static bool isDisplaying = false;

        void Awake()
        {
            Instance = this;
            isDisplaying = false;
        }

        public static void DialogClosed()
        {
            isDisplaying = false;
            Instance.dialogBox = null;
            ShowMessage();
        }

        private static void InitDialogBox(params object[] type)
        {
            GameObject newBox;

            switch (type[0])
            {
                case MsgType.Info:
                    newBox = Instantiate(Instance.msgBoxPrefabs[0], Instance.transform.localPosition, Quaternion.identity, Instance.transform);
                    break;
                case MsgType.Warning:
                    newBox = Instantiate(Instance.msgBoxPrefabs[1], Instance.transform.localPosition, Quaternion.identity, Instance.transform);
                    break;
                case MsgType.Error:
                    newBox = Instantiate(Instance.msgBoxPrefabs[2], Instance.transform.localPosition, Quaternion.identity, Instance.transform);
                    break;
                default:
                    newBox = Instantiate(Instance.msgBoxPrefabs[0], Instance.transform.localPosition, Quaternion.identity, Instance.transform);
                    break;
            }

            newBox.SetActive(false);
            Instance.dialogBox = newBox.GetComponent<DialogBox>();
            Instance.dialogBox.GetReferences();
        }

        public static void ShowMessage(MsgType type, string title, string msg)
        {
            if (!Instance.dialogBox || Instance.messages.Count == 0)
            {
                InitDialogBox(type);

                Instance.dialogBox.SetMessage(title, msg);
            }
            else
            {
                Instance.messages.Add(new Msg(type, title, msg));
            }

        }
        public static void ShowMessage(MsgType type, string title, string msg, Callback confirmCallback)
        {
            if (!Instance.dialogBox)
            {
                InitDialogBox(type);

                Instance.dialogBox.SetMessage(title, msg, confirmCallback);
                return;
            }

            Instance.messages.Add(new Msg(type, title, msg, confirmCallback));
        }

        public static void ShowMessage()
        {
            if (!Instance.dialogBox && Instance.messages.Count > 0)
            {
                Instance.messages.Sort((m1, m2) =>
                {
                    if (m1.type == MsgType.Error && m1.type != m2.type)
                    {
                        return 1;
                    }
                    else if (m1.type == MsgType.Warning && m1.type != m2.type)
                    {
                        return 1;
                    }
                    else return 0;
                });
                InitDialogBox(Instance.messages[0].type);

                Instance.dialogBox.SetMessage(Instance.messages[0].title, Instance.messages[0].msg, Instance.messages[0].callback);
                Instance.messages.RemoveAt(0);
            }
        }
    }
}
