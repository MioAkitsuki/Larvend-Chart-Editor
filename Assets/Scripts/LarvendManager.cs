using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace Larvend
{
    public class ChartManager
    {
        private static string path;
        static string[] chart; // Lines of Chart
        static List<Note> notes = new(); // Notes in Chart

        private static bool isInfoReading = false;
        private static bool isNotesReading = false;
        private static bool isInfoWriting = false;
        private static bool isNotesWriting = false;

        public static void ReadChart(int difficulty)
        {
            path = Global.FolderPath + "/" + difficulty + ".lff";
            notes = new();
            isInfoReading = false;
            isNotesReading = false;

            try
            {
                chart = File.ReadAllLines(path);

                if (chart.Length == 0)
                {
                    throw new Exception("Empty Chart.");
                }

                foreach (var line in chart)
                {
                    if (line.Contains("version="))
                    {
                        if (!line.EndsWith(Global.ChartVersion))
                        {
                            throw new Exception("Unexpected version of the chart.");
                        }

                        continue;
                    }

                    switch (line)
                    {
                        case "[INFO]" when !isInfoReading && !isNotesReading:
                            isInfoReading = true;
                            continue;
                        case "[ENDINFO]" when isInfoReading && !isNotesReading:
                            isInfoReading = false;
                            continue;
                        case "[NOTES]" when !isInfoReading && !isNotesReading:
                            isNotesReading = true;
                            continue;
                        case "[ENDNOTES]" when !isInfoReading && isNotesReading:
                            isNotesReading = false;
                            continue;
                        case "[END]" when notes.Count == Global.Chart.count:
                            isInfoReading = false;
                            isNotesReading = false;
                            continue;
                        case "[END]" when notes.Count != Global.Chart.count:
                            throw new Exception("Error number of notes. Please check the file integrity.");
                    }

                    if (isInfoReading) ReadInfo(line);
                    if (isNotesReading) ReadNote(line);
                }
                foreach (var note in notes)
                {
                    NoteManager.LoadNote(note);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("The file could not be read:");
                Debug.LogError(e.Message);
            }
        }

        private static void ReadNote(string line)
        {
            int time;
            float x, y; 
            int endTime;
            float targetBpm;

            string[] splitedLine = line.Split(new char[2] { '(', ')' });

            if (notes.Count == 0 && splitedLine[0] == "speed")
            {
                LineDivider(splitedLine[1], out time, out targetBpm, out endTime);
                if (time > 0 || endTime > 0)
                {
                    throw new Exception("Invalid bpm setting!");
                }

                Global.Chart.bpm = targetBpm;
                notes.Add(new Note(Note.Type.SpeedAdjust, time, targetBpm, endTime));
                return;
            }

            switch (splitedLine[0]) 
            { 
                case "tap":
                    LineDivider(splitedLine[1], out time, out x, out y); 
                    notes.Add(new Note(Note.Type.Tap, time, new Vector2(x, y))); 
                    break;
                case "hold": 
                    LineDivider(splitedLine[1], out time, out x, out y, out endTime);
                    notes.Add(new Note(Note.Type.Hold, time, new Vector2(x, y), endTime)); 
                    break;
                case "flick": 
                    LineDivider(splitedLine[1], out time, out x, out y);
                    notes.Add(new Note(Note.Type.Flick, time, new Vector2(x, y))); 
                    break;
                case "speed": 
                    LineDivider(splitedLine[1], out time, out targetBpm, out endTime);
                    notes.Add(new Note(Note.Type.SpeedAdjust, time, targetBpm, endTime)); 
                    break;
                default:
                    throw new Exception("Unknown Note Type.");
            }
        }

        private static void ReadInfo(string line)
        {
            string[] splitedLine = line.Split('=');

            switch (splitedLine[0])
            {
                case "title":
                    Global.Chart.title = splitedLine[1];
                    break;
                case "composer":
                    Global.Chart.composer = splitedLine[1]; 
                    break;
                case "arranger":
                    Global.Chart.arranger = splitedLine[1];
                    break;
                case "bpm":
                    Global.Chart.bpm = float.Parse(splitedLine[1]); 
                    break;
                case "offset":
                    Global.Chart.offset = float.Parse(splitedLine[1]); 
                    break;
                case "count":
                    Global.Chart.count = int.Parse(splitedLine[1]); 
                    break;
                default:
                    throw new Exception("Unknown Info Type.");
            }
        }

        private static void WriteChart(int difficulty)
        {
            path = Global.FolderPath + "/" + difficulty + ".lff";
            notes = Global.Notes;

            try
            {
                StreamWriter chartWriter;
                if (File.Exists(path))
                    chartWriter = new(path);
                else
                    throw (new Exception("There doesn't exist chart file."));

                if (isInfoWriting || isNotesWriting)
                    throw (new Exception("There is an unfinished writing process."));

                chartWriter.WriteLine("version=" + Global.ChartVersion);

                isInfoWriting = true;
                WriteInfo(chartWriter);

                if (!isInfoWriting)
                {
                    isNotesWriting = true;
                    WriteNotes(chartWriter);
                }

                chartWriter.WriteLine("[END]");
                chartWriter.Close();
            }
            catch (Exception e)
            {
                Debug.LogError("The file could not be written:");
                Debug.LogError(e.Message);
            }
        }

        private static void WriteNotes(StreamWriter chartWriter)
        {
            List<Note> toWriteNotes = ChartManager.notes;

            chartWriter.WriteLine("[NOTES]");

            foreach (var note in toWriteNotes)
            {
                switch (note.type)
                {
                    case Note.Type.Tap:
                        chartWriter.WriteLine("tap(" + note.time + "," + note.position.x + "," + note.position.y + ")");
                        continue;
                    case Note.Type.Hold:
                        chartWriter.WriteLine("hold(" + note.time + "," + note.position.x + "," + note.position.y + "," + note.endTime + ")");
                        continue;
                    case Note.Type.Flick:
                        chartWriter.WriteLine("flick(" + note.time + "," + note.position.x + "," + note.position.y + ")");
                        continue;
                    case Note.Type.SpeedAdjust:
                        chartWriter.WriteLine("speed(" + note.time + "," + note.targetBpm + "," + note.endTime + ")");
                        continue;
                }
            }

            chartWriter.WriteLine("[ENDNOTES]");
            isNotesWriting = false;
            return;
        }

        private static void WriteInfo(StreamWriter chartWriter)
        {
            chartWriter.WriteLine("[INFO]");
            chartWriter.WriteLine("title=" + Global.Chart.title);
            chartWriter.WriteLine("composer=" + Global.Chart.composer);
            chartWriter.WriteLine("arranger=" + Global.Chart.arranger);
            chartWriter.WriteLine("bpm=" + Global.Chart.bpm);
            chartWriter.WriteLine("offset=" + Global.Chart.offset);
            chartWriter.WriteLine("count=" + Global.Chart.count);
            chartWriter.WriteLine("[ENDINFO]");

            isInfoWriting = false;
            return;
        }

        private static void LineDivider(string line, out int arg1, out float arg2, out float arg3)
        {
            string[] splitedLine = line.Split(',');
            arg1 = int.Parse(splitedLine[0]);
            arg2 = float.Parse(splitedLine[1]);
            arg3 = float.Parse(splitedLine[2]);
        }

        private static void LineDivider(string line, out int arg1, out float arg2, out int arg3)
        {
            string[] splitedLine = line.Split(',');
            arg1 = int.Parse(splitedLine[0]);
            arg2 = float.Parse(splitedLine[1]);
            arg3 = int.Parse(splitedLine[2]);
        }

        private static void LineDivider(string line, out int arg1, out float arg2, out float arg3, out int arg4)
        {
            string[] splitedLine = line.Split(',');
            arg1 = int.Parse(splitedLine[0]);
            arg2 = float.Parse(splitedLine[1]);
            arg3 = float.Parse(splitedLine[2]);
            arg4 = int.Parse(splitedLine[3]);
        }
    }

    public class DirectoryManager
    {
        public static void ReadFolder()
        {
            if (Directory.Exists(Global.FolderPath))
            {
                DirectoryInfo dir = new DirectoryInfo(Global.FolderPath);
                FileInfo [] files = dir.GetFiles();

                FileInfo def = new FileInfo(Global.FolderPath + "/0.lff");
                if (files.Length == 0)
                    def.Create();
                
                foreach (var file in files)
                {
                    switch (file.Name)
                    {
                        case "0.lff":
                            Global.Difficulties[0] = true;
                            continue;
                        case "1.lff":
                            Global.Difficulties[1] = true;
                            continue;
                        case "2.lff":
                            Global.Difficulties[2] = true;
                            continue;
                        case "3.lff":
                            Global.Difficulties[3] = true;
                            continue;
                        case "base.mp3":
                            continue;
                        case "preview.mp3":
                            continue;
                    }
                }
            }
        }

        public static void CreateChart(int difficulty)
        {
            FileInfo chart = new FileInfo(Global.FolderPath + "/" + difficulty + ".lff");

            if (chart.Exists)
            {
                Debug.LogError("Already Exist!");
            }
            else
            {
                chart.Create();
                Global.Difficulties[difficulty] = true;
            }
        }
    }

    public class AudioManager
    {
        public static IEnumerator LoadAudio()
        {
            AudioClip clip = null;
            string path = Path.Combine(Global.FolderPath, "base.ogg");

            UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(path, AudioType.OGGVORBIS);
            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.ConnectionError ||
                uwr.result == UnityWebRequest.Result.DataProcessingError)
            {
                Debug.Log(uwr.error);
            }
            else
            {
                clip = DownloadHandlerAudioClip.GetContent(uwr);
            }

            Global.song = clip;
            Global.IsAudioLoaded = true;
            UIController.InitAudioLabel();
            GameManager.InitAudio();
        }
    }
}