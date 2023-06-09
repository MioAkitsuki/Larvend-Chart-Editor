using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Larvend.Gameplay
{
    public class DialogBox : MonoBehaviour
    {
        private TMP_Text dialogTitle;
        private TMP_Text dialogMsg;
        private Button dialogConfirm;
        private Button dialogCancel;
        public TMP_InputField dialogInput;

        public void GetReferences()
        {
            dialogTitle = transform.Find("Title").Find("TitleLabel").GetComponent<TMP_Text>();
            dialogMsg = transform.Find("Body").Find("BodyLabel").GetComponent<TMP_Text>();
            dialogConfirm = transform.Find("Confirm").GetComponent<Button>();
            dialogCancel = transform.Find("Cancel")?.GetComponent<Button>();
            dialogInput = transform.Find("InputField")?.GetComponent<TMP_InputField>();
        }

        public void SetMessage(string title, string msg)
        {
            this.GetComponent<CanvasGroup>().alpha = 0;
            dialogTitle.text = title;
            dialogMsg.text = msg;
            this.gameObject.SetActive(true);
            StartCoroutine("DialogBoxFadeIn");

            dialogCancel?.onClick.AddListener(CancelDialogBox);
            dialogConfirm.onClick.AddListener(ConfirmDialogBox);
        }

        public void SetMessage(string title, string msg, Callback callback)
        {
            this.GetComponent<CanvasGroup>().alpha = 0;
            dialogTitle.text = title;
            dialogMsg.text = msg;
            this.gameObject.SetActive(true);
            StartCoroutine("DialogBoxFadeIn");

            if (callback != null)
            {
                dialogConfirm.onClick.AddListener(delegate () { this.ConfirmDialogBox(callback); });
            }
            else
            {
                dialogConfirm.onClick.AddListener(ConfirmDialogBox);
            }
            dialogCancel?.onClick.AddListener(CancelDialogBox);
        }

        public void SetMessage(string title, string msg, Callback<string> callback)
        {
            this.GetComponent<CanvasGroup>().alpha = 0;
            dialogTitle.text = title;
            dialogMsg.text = msg;
            this.gameObject.SetActive(true);
            StartCoroutine("DialogBoxFadeIn");
            
            dialogConfirm.onClick.AddListener(delegate()
            {
                this.ConfirmDialogBox(callback);
            });
            
            dialogCancel?.onClick.AddListener(CancelDialogBox);
        }

        public void SetMessage(string title, string msg, Callback confirmCallback, Callback cancelCallback)
        {
            this.GetComponent<CanvasGroup>().alpha = 0;
            dialogTitle.text = title;
            dialogMsg.text = msg;
            this.gameObject.SetActive(true);
            StartCoroutine("DialogBoxFadeIn");

            if (confirmCallback != null && cancelCallback != null)
            {
                dialogConfirm.onClick.AddListener(delegate () { this.ConfirmDialogBox(confirmCallback); });
                dialogCancel.onClick.AddListener(delegate () { this.CancelDialogBox(cancelCallback); });
            }
            else
            {
                dialogConfirm.onClick.AddListener(ConfirmDialogBox);
                dialogCancel.onClick.AddListener(CancelDialogBox);
            }
        }

        public void SetMessage(string title, string msg, Callback<string> confirmCallback, Callback cancelCallback)
        {
            this.GetComponent<CanvasGroup>().alpha = 0;
            dialogTitle.text = title;
            dialogMsg.text = msg;
            this.gameObject.SetActive(true);
            StartCoroutine("DialogBoxFadeIn");

            if (confirmCallback != null && cancelCallback != null)
            {
                dialogConfirm.onClick.AddListener(delegate () { this.ConfirmDialogBox(confirmCallback); });
                dialogCancel.onClick.AddListener(delegate () { this.CancelDialogBox(cancelCallback); });
            }
            else
            {
                dialogConfirm.onClick.AddListener(ConfirmDialogBox);
                dialogCancel.onClick.AddListener(CancelDialogBox);
            }
        }

        private void ConfirmDialogBox()
        {
            StartCoroutine("DialogBoxFadeOut");
            dialogConfirm.onClick.RemoveListener(ConfirmDialogBox);
        }
        private void ConfirmDialogBox(Callback confirmCallback)
        {
            StartCoroutine("DialogBoxFadeOut");
            confirmCallback();
            dialogConfirm.onClick.RemoveListener(delegate () { this.ConfirmDialogBox(confirmCallback); });
        }
        private void ConfirmDialogBox(Callback<string> confirmCallback)
        {
            StartCoroutine("DialogBoxFadeOut");
            confirmCallback(dialogInput.text);
            dialogConfirm.onClick.RemoveListener(delegate () { this.ConfirmDialogBox(confirmCallback); });
        }
        private void CancelDialogBox()
        {
            StartCoroutine("DialogBoxFadeOut");
            dialogCancel.onClick.RemoveListener(CancelDialogBox);
        }
        private void CancelDialogBox(Callback cancelCallback)
        {
            StartCoroutine("DialogBoxFadeOut");
            cancelCallback();
            dialogCancel.onClick.RemoveListener(delegate () { this.CancelDialogBox(cancelCallback); });
        }

        IEnumerator DialogBoxFadeIn()
        {
            this.GetComponent<CanvasGroup>().alpha = Mathf.Lerp(this.GetComponent<CanvasGroup>().alpha, 1f, 0.2f);

            if (this.GetComponent<CanvasGroup>().alpha > 0.98f)
            {
                this.GetComponent<CanvasGroup>().alpha = 1;
                MsgBoxManager.isDisplaying = true;
                Global.IsDialoging = true;
                yield break;
            }

            yield return new WaitForFixedUpdate();
            StartCoroutine("DialogBoxFadeIn");
        }

        IEnumerator DialogBoxFadeOut()
        {
            this.GetComponent<CanvasGroup>().alpha = Mathf.Lerp(this.GetComponent<CanvasGroup>().alpha, 0f, 0.2f);

            if (this.GetComponent<CanvasGroup>().alpha < 0.02f)
            {
                this.GetComponent<CanvasGroup>().alpha = 0;
                this.gameObject.SetActive(false);
                MsgBoxManager.DialogClosed();
                Destroy(this);
                yield break;
            }

            yield return new WaitForFixedUpdate();
            StartCoroutine("DialogBoxFadeOut");
        }
    }

}