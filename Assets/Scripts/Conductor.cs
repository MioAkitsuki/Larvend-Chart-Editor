using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Larvend.Gameplay
{
    public class Conductor
    {
        public static Conductor Instance { get; private set; }

        private Conductor()
        {
            Instance = Instance == null ? new Conductor() : this;
        }
    }
}