using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Larvend.Gameplay
{
    public class Conductor
    {
        public static Conductor Instance { get; private set; }
        private float BPM;

        private Conductor()
        {
            Instance = Instance == null ? new Conductor() : this;
        }

        public static float GetBPM()
        {
            return Instance.BPM;
        }
    }
}