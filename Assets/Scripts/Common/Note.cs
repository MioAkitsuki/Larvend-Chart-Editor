using System.Numerics;
using System;
using System.Collections;
using System.Collections.Generic;
using Larvend.Gameplay;
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
        public float scale;

        public List<EventButton> eventButtons;

        private Animator _animator;
        public bool isDisplaying;

        private void Awake()
        {
            _animator = this.GetComponent<Animator>();
            _animator.enabled = true;
            _animator.speed = 0;

            eventButtons = new List<EventButton>();

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
                    if (Math.Abs(Mathf.Round(absRes.x * 10) - absRes.x * 10) < 0.1)
                    {
                        absRes.x = Mathf.Round(absRes.x * 10) / 10f;
                    }
                    if (Math.Abs(Mathf.Round(absRes.y * 10) - absRes.y * 10) < 0.1)
                    {
                        absRes.y = Mathf.Round(absRes.y * 10) / 10f;
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
            if (this.position.x < 0.05f)
            {
                this.position.x = 0.1f;
                flag = true;
            }
            else if (this.position.x > 0.95f)
            {
                this.position.x = 0.9f;
                flag = true;
            }
            if (this.position.y < 0.15f)
            {
                this.position.y = 0.2f;
                flag = true;
            }
            else if (this.position.y > 0.85f)
            {
                this.position.y = 0.8f;
                flag = true;
            }

            if (flag)
            {
                transform.position = Camera.main.ViewportToWorldPoint(new Vector3(position.x, position.y, time / 10000f));
                yield return new WaitForFixedUpdate();
            }
        }

        public void InitNote(Type _type, int _time, Vector2 _pos)
        {
            type = _type;
            time = _time;
            position = _pos;
            scale = 1f;
        }

        public void InitNote(Type _type, int _time, Vector2 _pos, int _endTime)
        {
            type = _type;
            time = _time;
            position = _pos;
            endTime = _endTime;
            scale = 1f;
        }

        public void InitNote(Type _type, int _time, Vector2 _pos, float _scale)
        {
            type = _type;
            time = _time;
            position = _pos;
            scale = _scale;
        }

        public void InitNote(Type _type, int _time, Vector2 _pos, int _endTime, float _scale)
        {
            type = _type;
            time = _time;
            position = _pos;
            endTime = _endTime;
            scale = _scale;
        }

        public IEnumerator StartPlay()
        {
            _animator.enabled = true;
            _animator.speed = EditorManager.GetBPM() / 60f * EditorManager.song.pitch;

            while (_animator.enabled)
            {
                if ((_animator.GetCurrentAnimatorStateInfo(0).IsTag("Appear") && this.type == Type.Hold && _animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.95) ||
                    _animator.GetCurrentAnimatorStateInfo(0).IsTag("Disappear") && this.type == Type.Hold && _animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.05)
                {
                    _animator.speed = EditorManager.GetBPM() / 60f / ((float) (endTime - time) / EditorManager.Instance.BeatPCM) * EditorManager.song.pitch;
                }
                if (_animator.GetCurrentAnimatorStateInfo(0).IsTag("Disappear") && _animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.95)
                {
                    _animator.Play($"{Enum.GetName(typeof(Type), this.type)}_Disappear", 0, 1);
                    _animator.speed = 0;
                    _animator.enabled = false;
                    this.gameObject.SetActive(false);
                }

                yield return new WaitForFixedUpdate();
            }
        }

        public void Relate(EventButton btn, BtnType type)
        {
            eventButtons.Add(btn);
            btn.type = type;
            btn.note = this;

            btn.Refresh();
        }

        public void Relate()
        {
            if (type is Type.Tap or Type.Flick)
            {
                foreach (var group in EventTrack.Instance.EventGroups)
                {
                    if (Math.Abs(group.Pcm + EditorManager.Instance.offset - this.time) <= 10)
                    {
                        var btn = group.FindFirstEmptyButton() ?? throw new Exception("Too many event in a time");
                        eventButtons.Add(btn);
                        switch (type)
                        {
                            case Type.Tap:
                                btn.type = BtnType.Tap;
                                break;
                            case Type.Flick:
                                btn.type = BtnType.Flick;
                                break;
                        }
                        btn.note = this;

                        btn.Refresh();
                        break;
                    }
                    else if (group.Pcm + EditorManager.Instance.offset - this.time > 10)
                    {
                        var btn = EventTrack.Instance.EventGroups[group.Id - 1].FindFirstEmptyButton() ?? throw new Exception("Too many event in a time");
                        eventButtons.Add(btn);
                        switch (type)
                        {
                            case Type.Tap:
                                btn.type = BtnType.TapInIt;
                                break;
                            case Type.Flick:
                                btn.type = BtnType.FlickInIt;
                                break;
                        }
                        btn.note = this;

                        btn.Refresh();
                        break;
                    }
                }
                return;
            }

            if (time == endTime)
            {
                foreach (var group in EventTrack.Instance.EventGroups)
                {
                    if (Math.Abs(group.Pcm + EditorManager.Instance.offset - this.time) <= 10)
                    {
                        var note = group.FindFirstEmptyButton() ?? throw new Exception("Too many event in a time");
                        eventButtons.Add(note);
                        note.type = BtnType.Hold;
                        note.note = this;

                        note.Refresh();
                        break;
                    }
                    else if (group.Pcm + EditorManager.Instance.offset - this.time > 10)
                    {
                        var note = EventTrack.Instance.EventGroups[group.Id - 1].FindFirstEmptyButton() ?? throw new Exception("Too many event in a time");
                        eventButtons.Add(note);
                        note.type = BtnType.HoldInIt;
                        note.note = this;

                        note.Refresh();
                        break;
                    }
                }
                return;
            }

            EventButton start = null;
            
            foreach (var group in EventTrack.Instance.EventGroups)
            {
                if (Math.Abs(group.Pcm + EditorManager.Instance.offset - this.time) <= 10 && start == null)
                {
                    start = group.FindFirstEmptyButton() ?? throw new Exception("Too many event in a time");
                    eventButtons.Add(start);
                    start.type = BtnType.Hold;
                    start.note = this;

                    start.Refresh();
                    continue;
                }
                else if (group.Pcm + EditorManager.Instance.offset - this.time > 10 && start == null)
                {
                    start = EventTrack.Instance.EventGroups[group.Id - 1].FindFirstEmptyButton() ?? throw new Exception("Too many event in a time");
                    eventButtons.Add(start);
                    start.type = BtnType.HoldInIt;
                    start.note = this;

                    start.Refresh();
                    continue;
                }

                if (Math.Abs(group.Pcm + EditorManager.Instance.offset - this.endTime) <= 10 && start != null)
                {
                    var end = group.FindButtonById(start.Id) ?? throw new Exception("Too many event in a time");
                    EventTrack.PaintHold(this, start, end);
                    break;
                }
                else if (group.Pcm + EditorManager.Instance.offset - this.endTime > 10 && start != null)
                {
                    var end = EventTrack.Instance.EventGroups[group.Id - 1].FindButtonById(start.Id) ?? throw new Exception("Too many event in a time");
                    EventTrack.PaintHold(this, start, end);
                    break;
                }
            }
        }

        public void CancelRelation()
        {
            foreach (var btn in eventButtons)
            {
                btn.type = BtnType.None;
                btn.Refresh();
            }
            eventButtons.Clear();
        }

        public Note Inherit(Note oldNote)
        {
            this.position = oldNote.position;
            this.scale = oldNote.scale;

            RefreshState();

            oldNote.DeleteSelf();

            return this;
        }

        public Note Copy(Note note)
        {
            this.position = note.position;
            this.scale = note.scale;

            RefreshState();

            return this;
        }

        public Note HorizontalMirror()
        {
            this.position = new Vector2(1 - position.x, position.y);

            RefreshState();

            return this;
        }

        public Note VerticalMirror()
        {
            this.position = new Vector2(position.x, 1 - position.y);

            RefreshState();

            return this;
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

            var newPos = Camera.main.ViewportToWorldPoint(this.position);
            transform.position = new Vector3(newPos.x, newPos.y, time / 10000f);
            transform.localScale = new Vector3(scale, scale, 1);

            this.StopCoroutine("StartPlay");
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

        public void UpdateEndTime(int value)
        {
            if (type != Type.Hold)
            {
                return;
            }
            try
            {
                int newTime = value;
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
                this.transform.position = new Vector3(newPos.x, newPos.y, time / 10000f);

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
                this.transform.position = new Vector3(newPos.x, newPos.y, time / 10000f);

                UIController.RefreshUI();
            }
            catch (Exception e)
            {
                MsgBoxManager.ShowMessage(MsgType.Error, "Error", e.Message);
            }
        }

        public void UpdateScale(string value)
        {
            try
            {
                float newValue = Single.Parse(value);
                if (newValue is >=0.5f and <=1f)
                {
                    this.scale = newValue;
                    this.transform.localScale = new Vector3(this.scale, this.scale, 1f);
                }

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
            CancelRelation();
            Destroy(this.gameObject);
        }
    }
}