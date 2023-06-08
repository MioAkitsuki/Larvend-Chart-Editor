using System;
using System.Collections;
using System.Numerics;
using System.Runtime.CompilerServices;
using Larvend.Gameplay;
using Mono.Cecil;
using Unity.VisualScripting;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
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

        private void Awake()
        {
            _animator = this.GetComponent<Animator>();
            _animator.enabled = true;
            _animator.speed = 0;
            RefreshState();
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

            if (proportion < -1 || proportion > 1)
            {
                _animator.Play("Tap_Disappear", 0, 1);
                return;
            }

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
                _animator.Play("Flick_Disappear", 0, 1);
                return;
            }

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
                _animator.Play("Hold_Disappear", 0, 1f);
                return;
            }

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
            if (Global.IsModifyTimeAllowed)
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