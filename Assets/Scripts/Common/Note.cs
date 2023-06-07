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

        public void UpdateTime(string value)
        {
            if (Global.IsModifyTimeAllowed)
            {
                int newTime = Int32.Parse(value);
                if (newTime >= 0)
                {
                    time = newTime;
                }
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
                    NoteManager.Instance.TapNotes.Remove(this.gameObject);
                    break;
                case Type.Hold:
                    NoteManager.Instance.HoldNotes.Remove(this.gameObject);
                    break;
                case Type.Flick:
                    NoteManager.Instance.FlickNotes.Remove(this.gameObject);
                    break;
            }
            Destroy(this.gameObject);
        }
    }
}