using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Larvend.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Larvend
{
    public class Line
    {
        internal Type type;
        internal int time;
        internal Vector2 position;
        internal int endTime;
        internal float targetBpm;

        internal Line(float bpm)
        {
            type = Type.SpeedAdjust;
            time = 0;
            endTime = 0;
            targetBpm = bpm;
        }

        internal Line(Type type, int time, Vector2 pos)
        {
            this.type = type;
            this.time = time;
            this.position = pos;
        }

        internal Line(Type type, int time, Vector2 pos, int endTime)
        {
            this.type = type;
            this.time = time;
            this.position = pos;
            this.endTime = endTime;
        }

        internal Line(Type type, int time, float targetBpm, int endTime)
        {
            this.type = type;
            this.time = time;
            this.targetBpm = targetBpm;
            this.endTime = endTime;
        }
    }

    public class ChartManager
    {
        private static string path;
        static string[] chart; // Lines of Chart
        static List<Line> notes = new(); // Notes in Chart

        private static bool isNotesReading = false;
        private static bool isNotesWriting = false;
        private static bool isBaseBpmReading;

        public static void ReadChart(int difficulty)
        {
            path = Global.FolderPath + $"/{difficulty}.lff";
            notes = new();
            isNotesReading = false;
            isBaseBpmReading = false;

            try
            {
                chart = File.ReadAllLines(path);

                if (chart.Length == 0)
                {
                    MsgBoxManager.ShowMessage(MsgType.Warning, "Warning", Localization.GetString(Global.Language, "LoadEmptyChartAttempt"), InitChart);
                    return;
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

                    if (line.Contains("offset="))
                    {
                        EditorManager.Instance.offset = Int32.Parse(line.Replace("offset=", ""));
                        continue;
                    }

                    switch (line)
                    {
                        case "[NOTES]" when !isNotesReading:
                            isBaseBpmReading = true;
                            continue;
                        case "":
                            continue;
                        case "[END]":
                            isNotesReading = false;
                            continue;
                    }

                    if (isBaseBpmReading)
                    {
                        Line note = ReadNote(line);

                        if (NoteManager.Instance.BaseSpeed == null && note.time == 0 && note.endTime == 0)
                        {
                            NoteManager.Instance.BaseSpeed = new Line(note.targetBpm);
                            EditorManager.Instance.InitializeBPM(note.targetBpm);
                        }

                        if (NoteManager.Instance.BaseSpeed == null)
                        {
                            throw new Exception(Localization.GetString(Global.Language, "BaseBpmNonexistent"));
                        }

                        isBaseBpmReading = false;
                        isNotesReading = true;

                        continue;
                    }

                    if (isNotesReading)
                    {
                        notes.Add(ReadNote(line));
                    }
                }
                foreach (var note in notes)
                {
                    NoteManager.LoadNote(note);
                }
                
                MsgBoxManager.ShowMessage(MsgType.Info, "Event Load Complete", Localization.GetString(Global.Language, "EventLoadSuccess"));
            }
            catch (Exception e)
            {
                MsgBoxManager.ShowMessage(MsgType.Error, "Chart Read Failed", e.Message);
            }
        }

        public static Line ReadNote(string line)
        {
            int time;
            float x, y; 
            int endTime;
            float targetBpm;

            string[] splitedLine = line.Split(new [] { '(', ')' });

            switch (splitedLine[0]) 
            { 
                case "tap":
                    LineDivider(splitedLine[1], out time, out x, out y);
                    return new Line(Type.Tap, time, new Vector2(x, y));
                case "hold":
                    LineDivider(splitedLine[1], out time, out x, out y, out endTime);
                    return new Line(Type.Hold, time, new Vector2(x, y), endTime);
                case "flick":
                    LineDivider(splitedLine[1], out time, out x, out y);
                    return new Line(Type.Flick, time, new Vector2(x, y));
                case "speed":
                    LineDivider(splitedLine[1], out time, out targetBpm, out endTime);
                    return new Line(Type.SpeedAdjust, time, targetBpm, endTime);
                default:
                    throw new Exception(Localization.GetString(Global.Language, "UnknownNoteType") + $"\n{line}");
            }
        }

        public static void WriteChart(int difficulty)
        {
            path = Global.FolderPath + $"/{difficulty}.lff";
            notes = NoteManager.GetAllNotes();

            try
            {
                StreamWriter chartWriter = new StreamWriter(path);

                if (isNotesWriting)
                    throw (new Exception("There was an unfinished writing process."));

                chartWriter.WriteLine($"version={Global.ChartVersion}");
                chartWriter.WriteLine($"offset={EditorManager.Instance.offset}\n");

                WriteNotes(chartWriter);

                chartWriter.WriteLine("[END]");
                chartWriter.Close();
            }
            catch (Exception e)
            {
                MsgBoxManager.ShowMessage(MsgType.Error, "Written Failed", e.Message);
            }
        }

        private static void WriteNotes(StreamWriter chartWriter)
        {
            List<Line> toWriteNotes = notes;

            chartWriter.WriteLine("[NOTES]");
            chartWriter.WriteLine($"speed(0,{NoteManager.Instance.BaseSpeed.targetBpm},0)");

            foreach (var note in toWriteNotes)
            {
                switch (note.type)
                {
                    case Type.Tap:
                        chartWriter.WriteLine("tap(" + note.time + "," + note.position.x + "," + note.position.y + ")");
                        continue;
                    case Type.Hold:
                        chartWriter.WriteLine("hold(" + note.time + "," + note.position.x + "," + note.position.y + "," + note.endTime + ")");
                        continue;
                    case Type.Flick:
                        chartWriter.WriteLine("flick(" + note.time + "," + note.position.x + "," + note.position.y + ")");
                        continue;
                    case Type.SpeedAdjust:
                        chartWriter.WriteLine("speed(" + note.time + "," + note.targetBpm + "," + note.endTime + ")");
                        continue;
                }
            }
            
            isNotesWriting = false;
        }

        public static void InitChart()
        {
            WriteChart(EditorManager.Instance.difficulty);
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

    [Serializable]
    public class DifficultyInfo
    {
        public int diffIndex;
        public string arranger;
        public string rating;

        public DifficultyInfo(int diffIndex, string arranger, string rating)
        {
            this.diffIndex = diffIndex;
            this.arranger = arranger;
            this.rating = rating;
        }

        public DifficultyInfo()
        {
            this.diffIndex = 0;
            this.arranger = "Sample Arranger";
            this.rating = "TBD";
        }
    }
    
    public class Info
    {
        public int index;
        public string id;
        public string title;
        public string composer;

        public List<DifficultyInfo> difficulties;

        public Info(int index, string id, string title, string composer, List<DifficultyInfo> difficulties)
        {
            this.index = index;
            this.id = id;
            this.title = title;
            this.composer = composer;
            this.difficulties = difficulties;
        }

        public Info()
        {
            this.index = 0;
            this.id = "Sample Id";
            this.title = "Sample Title";
            this.composer = "Sample Composer";
            this.difficulties = new List<DifficultyInfo>();
        }
    }

    public class InfoManager
    {
        public static void ReadInfo()
        {
            string path = Global.FolderPath + "/info.json";

            try
            {
                StreamReader str = File.OpenText(path);
                string raw = str.ReadToEnd();

                Info info = JsonUtility.FromJson<Info>(raw);
                EditorManager.UpdateInfo(info);
                str.Close();
            }
            catch (Exception e)
            {
                MsgBoxManager.ShowMessage(MsgType.Error, "Error", e.Message);
            }
        }

        public static void WriteInfo(Info info)
        {
            string path = Global.FolderPath + "/info.json";

            try
            {
                string json = JsonUtility.ToJson(info);

                StreamWriter sw = new StreamWriter(path);
                sw.Write(FormatJson(json));

                sw.Close();
            }
            catch (Exception e)
            {
                MsgBoxManager.ShowMessage(MsgType.Error, "Error", e.Message);
            }
        }

        public static string FormatJson(string sourceJson)
        {
            sourceJson += " ";
            int itap = 0;
            string newjson = "";

            for (int i = 0; i < sourceJson.Length - 1; i++)
            {
                if (sourceJson[i] == ':' && sourceJson[i + 1] != '{' && sourceJson[i + 1] != '[')
                {
                    newjson += sourceJson[i] + " ";
                }
                else if (sourceJson[i] == ':' && (sourceJson[i + 1] == '{' || sourceJson[i + 1] == '['))
                {
                    newjson += sourceJson[i] + "\n";
                    for (var a = 0; a < itap; a++)
                    {
                        newjson += "\t";
                    }
                }
                else if (sourceJson[i] == '{' || sourceJson[i] == '[')
                {
                    itap++;
                    newjson += sourceJson[i] + "\n";
                    for (var a = 0; a < itap; a++)
                    {
                        newjson += "\t";
                    }
                }
                else if ((sourceJson[i] == '}' || sourceJson[i] == ']'))
                {
                    itap--;
                    newjson += "\n";
                    for (var a = 0; a < itap; a++)
                    {
                        newjson += "\t";
                    }

                    newjson += sourceJson[i] + "" + ((sourceJson[i + 1] == ',') ? ",\n" : "");
                    if (sourceJson[i + 1] == ',')
                    {
                        i++;
                        for (var a = 0; a < itap; a++)
                        {
                            newjson += "\t";
                        }
                    }
                }
                else if (sourceJson[i] != '}' && sourceJson[i] != ']' && sourceJson[i + 1] == ',')
                {
                    newjson += sourceJson[i] + "" + sourceJson[i + 1] + "\n";
                    i++;
                    for (var a = 0; a < itap; a++)
                    {
                        newjson += "\t";
                    }
                }
                else
                {
                    newjson += sourceJson[i];
                }
            }
            return newjson;
        }
    }

    public class DirectoryManager
    {
        public static void ReadFolder()
        {
            try
            {
                if (Directory.Exists(Global.FolderPath))
                {
                    DirectoryInfo dir = new DirectoryInfo(Global.FolderPath);
                    FileInfo [] files = dir.GetFiles();

                    // FileInfo def = new FileInfo(Global.FolderPath + "/0.lff");
                    if (files.Length == 0)
                        throw new Exception(Localization.GetString(Global.Language, "LoadEmptyDirectoryAttempt"));
                
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
                            case "info.json":
                                InfoManager.ReadInfo();
                                continue;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MsgBoxManager.ShowMessage(MsgType.Info, "Empty Directory", e.Message, InitDirectory);
            }
        }

        public static void InitDirectory()
        {
            throw new Exception("Unfinished Method.");
        }

        public static void CreateChart(int difficulty)
        {
            FileInfo chart = new FileInfo($"{Global.FolderPath}/{difficulty}.lff");

            if (chart.Exists)
            {
                MsgBoxManager.ShowMessage(MsgType.Error, "Error", "Already exist a chart.\n文件夹下已存在相应难度谱面。");
            }
            else
            {
                chart.Create();
                Global.Difficulties[difficulty] = true;
                
                MsgBoxManager.ShowMessage(MsgType.Info, "Created Successfully", Localization.GetString(Global.Language, "CreateChartSuccess"), ChartManager.InitChart);
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
                yield break;
            }
            else
            {
                clip = DownloadHandlerAudioClip.GetContent(uwr);
            }

            // Global.song = clip;
            Global.IsAudioLoaded = true;
            EditorManager.InitAudio(clip);
            UIController.InitUI();
            NoteManager.RefreshAllNotes();
        }
    }

    public class ImageManager
    {
        public static IEnumerator LoadImg()
        {
            Texture2D texture = null;
            string path = Path.Combine(Global.FolderPath, "base.jpg");

            UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(path);
            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.ConnectionError ||
                uwr.result == UnityWebRequest.Result.DataProcessingError)
            {
                Debug.Log(uwr.error);
                yield break;
            }
            else
            {
                texture = DownloadHandlerTexture.GetContent(uwr);
            }

            Sprite tempSp = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0, 0));
            
            UIController.InitAlbumCover(tempSp);
        }
    }
}