using System;
using System.Collections;
using System.Collections.Generic;
using Larvend;
using UnityEngine;

namespace Larvend.Gameplay
{
    public class Global
    {
        public static string FolderPath;
        public static string ChartVersion = "1.0.0"; // Version that expected
        public static string Language = "zh_cn";

        public static bool IsDirectorySelected;
        public static bool IsFileSelected;
        public static bool IsAudioLoaded;
        public static bool IsDialoging;
        public static bool IsEditing;
        public static bool IsPrepared;
        public static bool IsPlaying;
        public static bool IsAbsorption;
        public static bool IsSaved = true;
    }
}
