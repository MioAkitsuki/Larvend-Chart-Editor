using System;
using System.Collections;
using System.Numerics;
using Larvend.Gameplay;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Larvend
{
    public enum Type
    {
        Tap,
        Hold,
        Flick,
        SpeedAdjust
    }

    public class Note : MonoBehaviour
    {
        public Type type;
        public int time;
        public Vector2 position;
        public int endTime;
        public float targetBpm;

        private Animator _animator;
        public bool isDisplaying;

        private void Awake()
        {
            _animator = this.GetComponent<Animator>();
            _animator.enabled = true;
            _animator.speed = 0;

            isDisplaying = false;
            this.gameObject.SetActive(false);
            RefreshState();
        }

        Vector3 m_Offset;
        Vector3 m_TargetScreenVec;

        private IEnumerator OnMouseDown()
        {
            m_TargetScreenVec = Camera.main.WorldToScreenPoint(transform.position);
            m_Offset = transform.position - Camera.main.ScreenToWorldPoint(new Vector3
                (Input.mousePosition.x, Input.mousePosition.y, 1f));

            while (Input.GetMouseButton(0))
            {
                Vector3 res = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x,
                    Input.mousePosition.y, 1f)) + m_Offset;
                if (Global.IsAbsorption)
                {
                    Vector3 absRes = Camera.main.WorldToViewportPoint(res);
                    if (Math.Abs(Mathf.Floor(absRes.x * 10) - absRes.x * 10) < 0.1)
                    {
                        absRes.x = Mathf.Floor(absRes.x * 10) / 10f;
                    }
                    if (Math.Abs(Mathf.Floor(absRes.y * 10) - absRes.y * 10) < 0.1)
                    {
                        absRes.y = Mathf.Floor(absRes.y * 10) / 10f;
                    }

                    transform.position = Camera.main.ViewportToWorldPoint(absRes);
                    this.position = absRes;
                    yield return new WaitForFixedUpdate();
                }
                else
                {
                    transform.position = res;
                    this.position = Camera.main.WorldToViewportPoint(transform.position);
                    yield return new WaitForFixedUpdate();
                }
            }
        }

        private IEnumerator OnMouseUp()
        {
            bool flag = false;
            if (this.position.x < 0.1f)
            {
                this.position.x = 0.1f;
                flag = true;
            }
            else if (this.position.x > 0.9f)
            {
                this.position.x = 0.9f;
                flag = true;
            }
            if (this.position.y < 0.2f)
            {
                this.position.y = 0.2f;
                flag = true;
            }
            else if (this.position.y > 0.8f)
            {
                this.position.y = 0.8f;
                flag = true;
            }

            if (flag)
            {
                transform.position = Camera.main.ViewportToWorldPoint(new Vector3(position.x, position.y, 1f));
                yield return new WaitForFixedUpdate();
            }
        }

        public void InitNote(Type _type, int _time, Vector2 _pos)
        {
            type = _type;
            time = _time;
            position = _pos;
        }

        public void InitNote(Type _type, int _time, Vector2 _pos, int _endTime)
        {
            type = _type;
            time = _time;
            position = _pos;
            endTime = _endTime;
        }

        public void InitNote(Type _type, int _time, Vector2 _pos, float _targetBpm)
        {
            type = _type;
            time = _time;
            position = _pos;
            targetBpm = _targetBpm;
        }

        public void RefreshState()
        {
            switch (this.type)
            {
                case Type.Tap:
                    RefreshTapState();
                    break;
                case Type.Hold:
                    RefreshHoldState();
                    break;
                case Type.Flick:
                    RefreshFlickState();
                    break;
            }
        }

        private void RefreshTapState()
        {
            if (this.type != Type.Tap)
            {
                return;
            }

            int deltaTime = EditorManager.GetAudioPCMTime() - time;
            float proportion = deltaTime / 44100f / (60f / EditorManager.GetBPM());

            if (proportion is < -1 or > 1)
            {
                if (!isDisplaying)
                {
                    return;
                }
                _animator.Play("Tap_Disappear", 0, 1);
                isDisplaying = false;
                this.gameObject.SetActive(false);
                return;
            }

            isDisplaying = true;
            this.gameObject.SetActive(true);
            _animator.enabled = true;
            _animator.speed = 0;
            if (proportion <= 0)
            {
                _animator.Play("Tap_Appear", 0, 1 + proportion);
            }
            else if (proportion > 0)
            {
                _animator.Play("Tap_Disappear", 0, proportion);
            }
        }

        private void RefreshFlickState()
        {
            if (this.type != Type.Flick)
            {
                return;
            }

            int deltaTime = EditorManager.GetAudioPCMTime() - time;
            float proportion = deltaTime / 44100f / (60f / EditorManager.GetBPM());

            if (proportion < -1 || proportion > 1)
            {
                if (!isDisplaying)
                {
                    return;
                }
                _animator.Play("Flick_Disappear", 0, 1);
                isDisplaying = false;
                this.gameObject.SetActive(false);
                return;
            }

            isDisplaying = true;
            this.gameObject.SetActive(true);
            _animator.enabled = true;
            _animator.speed = 0;
            if (proportion <= 0)
            {
                _animator.Play("Flick_Appear", 0, 1 + proportion);
            }
            else if (proportion > 0)
            {
                _animator.Play("Flick_Disappear", 0, proportion);
            }
        }

        private void RefreshHoldState()
        {
            if (this.type != Type.Hold)
            {
                return;
            }

            int deltaTime = EditorManager.GetAudioPCMTime() - time;
            int deltaSustainTime = endTime - time;
            float proportion = deltaTime / 44100f / (60f / EditorManager.GetBPM());
            float sustainProportion = (float) deltaTime / deltaSustainTime;
            
            if (proportion < -1 || sustainProportion > 1)
            {
                if (!isDisplaying)
                {
                    return;
                }
                _animator.Play("Hold_Disappear", 0, 1f);
                isDisplaying = false;
                this.gameObject.SetActive(false);
                return;
            }

            isDisplaying = true;
            this.gameObject.SetActive(true);
            _animator.enabled = true;
            _animator.speed = 0;
            if (proportion <= 0)
            {
                _animator.Play("Hold_Appear", 0, 1 + proportion);
            }
            else if (proportion > 0)
            {
                _animator.Play("Hold_Disappear", 0, sustainProportion);
            }
        }

        public void UpdateTime(string value)
        {
            if (PlayerPrefs.GetInt("IsModifyNoteTimeAllowed") == 1)
            {
                try
                {
                    int newTime = Int32.Parse(value);
                    if (newTime >= 0)
                    {
                        time = newTime;
                    }
                    else
                    {
                        time = 0;
                    }
                    UIController.RefreshUI();

                    RefreshState();
                }
                catch(Exception e)
                {
                    MsgBoxManager.ShowMessage(MsgType.Error, "Error", e.Message);
                }
            }
        }

        public void UpdateEndTime(string value)
        {
            if (type != Type.Hold)
            {
                return;
            }
            try
            {
                int newTime = Int32.Parse(value);
                if (newTime >= time)
                {
                    endTime = newTime;
                }
                else
                {
                    endTime = time;
                }
                UIController.RefreshUI();

                RefreshState();
            }
            catch (Exception e)
            {
                MsgBoxManager.ShowMessage(MsgType.Error, "Error", e.Message);
            }
        }

        public void UpdatePosX(string value)
        {
            try
            {
                float newValue = Single.Parse(value);
                if (newValue < 0.1f)
                {
                    this.position.x = 0.1f;
                }
                else if (newValue > 0.9f)
                {
                    this.position.x = 0.9f;
                }
                else
                {
                    this.position.x = Single.Parse(value);
                }

                var newPos = Camera.main.ViewportToWorldPoint(this.position);
                this.transform.position = new Vector3(newPos.x, newPos.y, 1f);

                UIController.RefreshUI();
            }
            catch (Exception e)
            {
                MsgBoxManager.ShowMessage(MsgType.Error, "Error", e.Message);
            }
        }

        public void UpdatePosY(string value)
        {
            try
            {
                float newValue = Single.Parse(value);
                if (newValue < 0.2f)
                {
                    this.position.y = 0.2f;
                }
                else if (newValue > 0.8f)
                {
                    this.position.y = 0.8f;
                }
                else
                {
                    this.position.y = newValue;
                }

                var newPos = Camera.main.ViewportToWorldPoint(this.position);
                this.transform.position = new Vector3(newPos.x, newPos.y, 1f);

                UIController.RefreshUI();
            }
            catch (Exception e)
            {
                MsgBoxManager.ShowMessage(MsgType.Error, "Error", e.Message);
            }
        }

        public void UpdateInfo(params string[] param)
        {
            type = (Type) Int32.Parse(param[0]);
            time = (int) Int32.Parse(param[1]);
            position = new Vector2(Single.Parse(param[2]), Single.Parse(param[3]));
            if (param.Length > 4)
            {
                endTime = (int) Int32.Parse(param[4]);
            }
        }

        public void DeleteSelf()
        {
            switch (type)
            {
                case Type.Tap:
                    NoteManager.Instance.TapNotes.Remove(this);
                    break;
                case Type.Hold:
                    NoteManager.Instance.HoldNotes.Remove(this);
                    break;
                case Type.Flick:
                    NoteManager.Instance.FlickNotes.Remove(this);
                    break;
            }
            Destroy(this.gameObject);
        }
    }
}