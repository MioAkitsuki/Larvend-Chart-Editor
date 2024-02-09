using System;
using System.Collections;
using System.Collections.Generic;
using Larvend.Gameplay;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using QFramework;

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

        public bool IsPlayed;

        public List<EventButtonData> eventButtons;

        private Animator _animator;
        private Collider2D _collider;
        public bool isDisplaying;

        private void Awake()
        {
            _animator = this.GetComponent<Animator>();
            _animator.enabled = true;
            _animator.speed = 0;

            _collider = GetComponent<Collider2D>();
            _collider.enabled = false;

            IsPlayed = false;

            eventButtons = new List<EventButtonData>();

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

            var oldLine = new Line(this);
            this.GetComponentInChildren<SpriteRenderer>().material = UIController.Instance.materials[1];

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

            OperationTracker.Record(new Operation(OperationType.Modify, oldLine, new Line(this)));
        }

        private void OnMouseUp()
        {
            this.GetComponentInChildren<SpriteRenderer>().material = UIController.Instance.materials[0];

            bool flag = false;
            if (this.position.x < 0.05f)
            {
                this.position.x = 0.05f;
                flag = true;
            }
            else if (this.position.x > 0.95f)
            {
                this.position.x = 0.95f;
                flag = true;
            }
            if (this.position.y < 0.15f)
            {
                this.position.y = 0.15f;
                flag = true;
            }
            else if (this.position.y > 0.85f)
            {
                this.position.y = 0.85f;
                flag = true;
            }

            if (flag)
            {
                transform.position = Camera.main.ViewportToWorldPoint(new Vector3(position.x, position.y, time / 10000f));
                OperationTracker.EditTarget(new Line(this));

                // yield return new WaitForFixedUpdate();
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

        private void Update()
        {
            if (Mathf.Abs(EditorManager.GetAudioPCMTime() - time) < 1000 && !IsPlayed && Global.IsPlaying)
            {
                if (type == Type.Hold && time == endTime)
                {
                    AudioKit.PlaySound("Resources://Touch");
                }
                else AudioKit.PlaySound("Resources://Tap");
                IsPlayed = true;
            }
        }

        public IEnumerator StartPlay()
        {
            _animator.enabled = true;
            IsPlayed = false;
            _animator.speed = EditorManager.GetBPM() / 60f * EditorManager.song.pitch;

            if (EditorManager.GetAudioPCMTime() >= time && type is Type.Tap or Type.Flick)
            {
                _animator.Play($"{Enum.GetName(typeof(Type), this.type)}_Disappear", 0, (EditorManager.GetAudioPCMTime() - time) / 44100f);
            }
            else if (EditorManager.GetAudioPCMTime() >= time && type is Type.Hold)
            {
                _animator.Play($"{Enum.GetName(typeof(Type), this.type)}_Disappear", 0, (float) (EditorManager.GetAudioPCMTime() - time) / (endTime - time));
                _animator.speed = EditorManager.GetBPM() / 60f / ((float) (endTime - time) / EditorManager.Instance.BeatPCM) * EditorManager.song.pitch;
            }

            while (_animator.enabled)
            {
                if (type == Type.Hold && time == endTime && EditorManager.GetAudioPCMTime() > endTime)
                {
                    _animator.Play("Hold_Appear", 0, 0f);
                    _animator.speed = 0;
                    _animator.enabled = false;
                    gameObject.SetActive(false);
                }
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

        public void Relate(EventButtonData btnData, BtnType type)
        {
            eventButtons.Add(btnData);
            btnData.type = type;
            btnData.note = this;
        }

        public void Relate()
        {
            int targetId = EventTrackController.FromTickToId(EventTrackController.FromPcmToTick(time));

            var targetButton = EventTrackController.GetModel().EventGroups[targetId].FindFirstEmptyButton();
            eventButtons.Add(targetButton);

            if (targetButton == null) return;

            if (type is Type.Tap or Type.Flick)
            {
                switch (type)
                {
                    case Type.Tap when Mathf.Abs(targetButton.group.Pcm - time) < 10:
                        targetButton.type = BtnType.Tap;
                        break;
                    case Type.Tap:
                        targetButton.type = BtnType.TapInIt;
                        break;
                    case Type.Flick when Mathf.Abs(targetButton.group.Pcm - time) < 10:
                        targetButton.type = BtnType.Flick;
                        break;
                    case Type.Flick:
                        targetButton.type = BtnType.FlickInIt;
                        break;
                }
                targetButton.note = this;
                return;
            }

            if (time == endTime)
            {
                if (Mathf.Abs(targetButton.group.Pcm - time) < 10)
                {
                    targetButton.type = BtnType.Hold;
                }
                else
                {
                    targetButton.type = BtnType.HoldInIt;
                }
                targetButton.note = this;
                return;
            }

            EventButtonData start = EventTrackController.GetModel().EventGroups[targetId].FindFirstEmptyButton();
            int endId = EventTrackController.FromTickToId(EventTrackController.FromPcmToTick(endTime));
            EventButtonData end = EventTrackController.GetModel().EventGroups[endId].FindFirstEmptyButton();

            if (Mathf.Abs(start.group.Pcm - time) < 10)
            {
                start.type = BtnType.Hold;
            }
            else
            {
                start.type = BtnType.HoldInIt;
            }
            start.note = this;

            EventTrackController.PaintHold(this, start, end);
        }

        public void CancelRelation()
        {
            if (eventButtons.Count == 0) return;

            foreach (var btn in eventButtons)
            {
                if (btn == null) continue;
                btn.type = BtnType.None;
                btn.note = null;
            }
            eventButtons.Clear();

            TypeEventSystem.Global.Send(new GroupRefreshEvent());
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

        public Note Copy(Line line)
        {
            position = line.position;
            scale = line.scale;

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

            _collider.enabled = false;
            if (proportion is >-0.5f and <0.3f)
            {
                _collider.enabled = true;
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

            _collider.enabled = false;
            if (proportion is >-0.5f and <0.3f)
            {
                _collider.enabled = true;
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

            if (deltaSustainTime == 0)
            {
                if (proportion < -1 || proportion > 0)
                {
                    if (!isDisplaying)
                    {
                        return;
                    }
                    _collider.enabled = false;
                    _animator.Play("Hold_Appear", 0, 0f);
                    isDisplaying = false;
                    this.gameObject.SetActive(false);
                    return;
                }
                else if (proportion > -0.5f)
                {
                    _collider.enabled = true;
                }

                isDisplaying = true;
                this.gameObject.SetActive(true);
                _animator.enabled = true;
                _animator.speed = 0;
                _animator.Play("Hold_Appear", 0, 1 + proportion);
                return;
            }

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

            _collider.enabled = false;
            if (proportion > -0.5f && sustainProportion < 0.9f)
            {
                _collider.enabled = true;
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
                    CancelRelation();
                    Relate();
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
                // var oldLine = new Line(this);

                if (newTime >= time)
                {
                    endTime = newTime;
                }
                else
                {
                    endTime = time;
                }

                // OperationTracker.Record(new Operation(OperationType.Modify, oldLine, new Line(this)));
                // UIController.RefreshUI();

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
                var oldLine = new Line(this);

                if (newValue < 0.05f)
                {
                    this.position.x = 0.05f;
                }
                else if (newValue > 0.95f)
                {
                    this.position.x = 0.95f;
                }
                else
                {
                    this.position.x = Single.Parse(value);
                }

                var newPos = Camera.main.ViewportToWorldPoint(this.position);
                this.transform.position = new Vector3(newPos.x, newPos.y, time / 10000f);

                OperationTracker.Record(new Operation(OperationType.Modify, oldLine, new Line(this)));

                // UIController.RefreshUI();
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
                var oldLine = new Line(this);

                if (newValue < 0.15f)
                {
                    this.position.y = 0.15f;
                }
                else if (newValue > 0.85f)
                {
                    this.position.y = 0.85f;
                }
                else
                {
                    this.position.y = newValue;
                }

                var newPos = Camera.main.ViewportToWorldPoint(this.position);
                this.transform.position = new Vector3(newPos.x, newPos.y, time / 10000f);

                OperationTracker.Record(new Operation(OperationType.Modify, oldLine, new Line(this)));

                // UIController.RefreshUI();
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
                var oldLine = new Line(this);
                if (newValue >= 0)
                {
                    this.scale = newValue;
                    // this.transform.localScale = new Vector3(this.scale, this.scale, 1f);
                    OperationTracker.Record(new Operation(OperationType.Modify, oldLine, new Line(this)));
                }

                // UIController.RefreshUI();
                RefreshState();
            }
            catch (Exception e)
            {
                MsgBoxManager.ShowMessage(MsgType.Error, "Error", e.Message);
            }
        }

        public void UpdateInfo(Note note)
        {
            time = note.time;
            endTime = note.endTime;
            position = note.position;
            scale = note.scale;
            
            RefreshState();
        }

        public void UpdateInfo(Line line)
        {
            time = line.time;
            endTime = line.endTime;
            position = line.position;
            scale = line.scale;
            
            RefreshState();
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