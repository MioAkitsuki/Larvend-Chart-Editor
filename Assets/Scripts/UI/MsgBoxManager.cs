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
        Input
    }

    public class Msg
    {
        public string title;
        public string msg;
        public MsgType type;
        public Callback confirmCallback;
        public Callback<string> paramCallback;
        public Callback cancelCallback;
        public bool flag;

        public Msg(MsgType type, string title, string msg)
        {
            this.title = title;
            this.msg = msg;
            this.type = type;
            this.confirmCallback = null;
            this.cancelCallback = null;
            this.flag = false;
        }

        public Msg(MsgType type, string title, string msg, Callback confirmCallback)
        {
            this.title = title;
            this.msg = msg;
            this.type = type;
            this.flag = false;
            
            this.confirmCallback = confirmCallback;
            this.cancelCallback = null;
        }

        public Msg(MsgType type, string title, string msg, Callback confirmCallback, Callback cancelCallback)
        {
            this.title = title;
            this.msg = msg;
            this.type = type;
            this.flag = false;

            this.confirmCallback = confirmCallback;
            this.cancelCallback = cancelCallback;
        }

        public Msg(MsgType type, string title, string msg, Callback<string> paramCallback, Callback cancelCallback)
        {
            this.title = title;
            this.msg = msg;
            this.type = type;
            this.flag = false;

            this.paramCallback = paramCallback;
            this.cancelCallback = cancelCallback;
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
            Global.IsDialoging = false;
            isDisplaying = false;
            Instance.dialogBox = null;

            if (Instance.messages.Count > 0)
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
                case MsgType.Input:
                    newBox = Instantiate(Instance.msgBoxPrefabs[3], Instance.transform.localPosition, Quaternion.identity, Instance.transform);
                    break;
                default:
                    newBox = Instantiate(Instance.msgBoxPrefabs[0], Instance.transform.localPosition, Quaternion.identity, Instance.transform);
                    break;
            }

            newBox.SetActive(false);
            Instance.dialogBox = newBox.GetComponent<DialogBox>();
            Instance.dialogBox.GetReferences();
        }

        /// <summary>
        /// Use Dialog Box to show message.
        /// </summary>
        /// <param name="type">Info, Warning or Error</param>
        /// <param name="title"></param>
        /// <param name="msg"></param>
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

        public static void ShowMessage(MsgType type, string title, string msg, Callback confirmCallback, Callback cancelCallback)
        {
            if (!Instance.dialogBox)
            {
                InitDialogBox(type);

                Instance.dialogBox.SetMessage(title, msg, confirmCallback, cancelCallback);
                return;
            }

            Instance.messages.Add(new Msg(type, title, msg, confirmCallback, cancelCallback));
        }

        public static void ShowInputDialog(string title, string msg, Callback<string> callback)
        {
            InitDialogBox(MsgType.Input);

            Instance.dialogBox.SetMessage(title, msg, callback);
            return;
        }

        public static void ShowInputDialog(string title, string msg, Callback<string> callback, Callback cancelCallback)
        {
            if (!Instance.dialogBox)
            {
                InitDialogBox(MsgType.Input);

                Instance.dialogBox.SetMessage(title, msg, callback, cancelCallback);
                return;
            }

            Instance.messages.Add(new Msg(MsgType.Input, title, msg, callback, cancelCallback));
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

                if (Instance.messages[0].type == MsgType.Input)
                {
                    Instance.dialogBox.SetMessage(Instance.messages[0].title, Instance.messages[0].msg, Instance.messages[0].paramCallback, Instance.messages[0].cancelCallback);
                }
                else
                {
                    Instance.dialogBox.SetMessage(Instance.messages[0].title, Instance.messages[0].msg, Instance.messages[0].confirmCallback, Instance.messages[0].cancelCallback);
                }
                
                Instance.messages.RemoveAt(0);
            }
        }
    }
}
