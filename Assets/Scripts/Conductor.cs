using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace Larvend.Gameplay
{
    public class Conductor
    {
        public static Conductor Instance { get; private set; }
        private float startDspTime;

        private Conductor()
        {
            Instance = Instance == null ? new Conductor() : this;
        }

        

    }
}