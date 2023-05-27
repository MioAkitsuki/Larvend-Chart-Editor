using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Larvend.Gameplay
{
    public class EventManager : MonoBehaviour
    {
        public static EventManager Instance { get; private set; }
        private List<Note> speedAdjusts = new ();

    }
}