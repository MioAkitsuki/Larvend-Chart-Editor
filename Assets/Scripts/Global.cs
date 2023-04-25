using System;
using System.Collections;
using System.Collections.Generic;
using Larvend;
using Unity.VisualScripting;
using UnityEngine;

public class Global
{
    public static string FolderPath;
    public static string ChartVersion = "1.1.2"; // Version that expected

    public static bool IsDirectorySelected;
    public static bool IsFileSelected;
    public static bool IsAudioLoaded;
    public static Chart Chart = new Chart();
    public static bool[] Difficulties = new bool[4];
    public static bool IsSaved;

    public static List<Note> Notes;
    public static AudioClip song;

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
