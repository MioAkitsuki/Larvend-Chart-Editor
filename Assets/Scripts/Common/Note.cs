using System.Numerics;
using System.Runtime.CompilerServices;
using Mono.Cecil;
using Unity.VisualScripting;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Larvend
{
    public class Note
    {
        public enum Type
        {
            Tap,
            Hold,
            Flick,
            SpeedAdjust
        }

        public Type type;
        public int time;
        public Vector2 position;
        public int endTime;
        public float targetBpm;

        internal Note(Type type, int time, Vector2 pos)
        {
            this.type = type;
            this.time = time;
            this.position = pos;
        }

        internal Note(Type type, int time, Vector2 pos, int endTime)
        {
            this.type = type;
            this.time = time;
            this.position = pos;
            this.endTime = endTime;
        }

        internal Note(Type type, int time, float targetBpm, int endTime)
        {
            this.type = type;
            this.time = time;
            this.targetBpm = targetBpm;
            this.endTime = endTime;
        }
    }
}