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
        public static bool IsAbsorption;
        public static bool IsSaved = true;

        public static bool IsModifyBaseBpmAllowed = false;
        public static bool IsModifyTimeAllowed = false;
        
        public static string[] TimeFormat(float sec)
        {
            string[] formattedTime = new string[3];
            formattedTime.Initialize();

            int itg = (int)Math.Truncate(sec);
            int dec = (int)((sec - itg) * 1000);

            switch (dec)
            {
                case < 10:
                    formattedTime.SetValue($"00{dec}", 2);
                    break;
                case < 100:
                    formattedTime.SetValue($"0{dec}", 2);
                    break;
                default:
                    formattedTime.SetValue($"{dec}", 2);
                    break;
            }

            formattedTime.SetValue(itg % 60 < 10 ? $"0{itg % 60}" : $"{itg % 60}", 1);

            formattedTime.SetValue(itg / 60 < 10 ? $"0{itg / 60}" : $"{itg / 60}", 0);

            return formattedTime;
        }
    }
}
