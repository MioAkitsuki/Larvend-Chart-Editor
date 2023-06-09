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

            NoteManager.ClearAllNotes();
            try
            {
                chart = File.ReadAllLines(path);

                if (chart.Length == 0)
                {
                    MsgBoxManager.ShowMessage(MsgType.Warning, "Warning", Localization.GetString("LoadEmptyChartAttempt"),
                        delegate()
                        {
                            InitChart();
                        });
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
                            throw new Exception(Localization.GetString("BaseBpmNonexistent"));
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
                
                MsgBoxManager.ShowMessage(MsgType.Info, "Event Load Complete", Localization.GetString("EventLoadSuccess"));
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
                    throw new Exception(Localization.GetString("UnknownNoteType") + $"\n{line}");
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

                chartWriter.WriteLine("[NOTES]");
                chartWriter.WriteLine($"speed(0,{NoteManager.Instance.BaseSpeed.targetBpm},0)");

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

            foreach (var note in toWriteNotes)
            {
                switch (note.type)
                {
                    case Type.Tap:
                        chartWriter.WriteLine($"tap({note.time},{note.position.x:N2},{note.position.y:N2})");
                        continue;
                    case Type.Hold:
                        chartWriter.WriteLine($"hold({note.time},{note.position.x:N2},{note.position.y:N2},{note.endTime})");
                        continue;
                    case Type.Flick:
                        chartWriter.WriteLine($"flick({note.time},{note.position.x:N2},{note.position.y:N2})");
                        continue;
                    case Type.SpeedAdjust:
                        chartWriter.WriteLine($"speed({note.time},{note.targetBpm:N2},{note.endTime})");
                        continue;
                }
            }
            
            isNotesWriting = false;
        }

        public static void InitChart(params bool[] param)
        {
            NoteManager.ClearAllNotes();
            try
            {
                MsgBoxManager.ShowInputDialog("Initialize Chart", "Please Set Base BPM", delegate(string value)
                {
                    EditorManager.Instance.InitializeBPM(Single.Parse(value));
                    NoteManager.Instance.BaseSpeed = new Line(Single.Parse(value));

                    WriteChart(EditorManager.Instance.difficulty);

                    if (param.Length > 0)
                    {
                        MsgBoxManager.ShowMessage(MsgType.Info, "Initialize Directory Successfully",
                            Localization.GetString("InitDirectorySuccess"));

                        UIController.Instance.InitDifficultySelector();

                        System.Diagnostics.Process.Start(Global.FolderPath);
                        Global.IsDirectorySelected = true;
                    }
                    else
                    {
                        MsgBoxManager.ShowMessage(MsgType.Info, "Initialize Success",
                            Localization.GetString("InitializeChartSuccess"));
                    }
                }, AbortInitChart);
            }
            catch (Exception e)
            {
                MsgBoxManager.ShowMessage(MsgType.Error, "Error in Initializing Chart", e.Message);
                throw;
            }
        }

        public static void AbortInitChart()
        {
            throw new Exception("Initialization Aborted.");
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
        public static Info ReadInfo()
        {
            string path = Global.FolderPath + "/info.json";

            try
            {
                StreamReader str = File.OpenText(path);
                string raw = str.ReadToEnd();

                Info info = JsonUtility.FromJson<Info>(raw);
                str.Close();

                return info;
            }
            catch (Exception e)
            {
                MsgBoxManager.ShowMessage(MsgType.Error, "Error", e.Message);
            }

            return null;
        }

        public static void WriteInfo()
        {
            string path = Global.FolderPath + "/info.json";
            Info info = EditorManager.GetInfo();

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
                if (Global.FolderPath == null)
                {
                    return;
                }

                if (Directory.Exists(Global.FolderPath))
                {
                    DirectoryInfo dir = new DirectoryInfo(Global.FolderPath);
                    FileInfo [] files = dir.GetFiles();
                    
                    bool isProjectExist = false;
                    bool isChartExist = false;

                    if (files.Length == 0)
                    {
                        MsgBoxManager.ShowMessage(MsgType.Info, "Empty Directory", Localization.GetString("LoadEmptyDirectoryAttempt"),
                            InitDirectory, () => throw new Exception("The directory is empty."));
                    }

                    foreach (var file in files)
                    {
                        switch (file.Name)
                        {
                            case "0.lff":
                                isChartExist = true;
                                continue;
                            case "1.lff":
                                isChartExist = true;
                                continue;
                            case "2.lff":
                                isChartExist = true;
                                continue;
                            case "3.lff":
                                isChartExist = true;
                                continue;
                            case "base.ogg":
                                EditorManager.Instance.StartCoroutine(AudioManager.LoadAudio());
                                continue;
                            case "preview.mp3":
                                continue;
                            case "base.jpg":
                                EditorManager.Instance.StartCoroutine(ImageManager.LoadImg());
                                continue;
                            case "info.json":
                                isProjectExist = true;
                                EditorManager.UpdateInfo(InfoManager.ReadInfo());
                                UIController.InitSongInfo();
                                continue;
                        }
                    }

                    if (!isProjectExist)
                    {
                        throw new Exception(Localization.GetString("ProjectNotFound"));
                    }

                    if (!isChartExist)
                    {
                        MsgBoxManager.ShowMessage(MsgType.Info, "Chart Not Found", Localization.GetString("NoChartInDirectory"),
                            delegate ()
                            {
                                EditorManager.Instance.difficulty = 0;
                                ChartManager.InitChart();
                                UIController.Instance.InitDifficultySelector();
                            });
                    }
                    else
                    {
                        UIController.Instance.InitDifficultySelector();
                    }
                }
            }
            catch (Exception e)
            {
                MsgBoxManager.ShowMessage(MsgType.Error, "Load Directory Failed", e.Message);
                Global.IsDirectorySelected = false;
            }
        }

        public static void InitDirectory()
        {
            try
            {
                if (Global.FolderPath == null)
                {
                    return;
                }

                if (Directory.GetFiles(Global.FolderPath).Length > 0)
                {
                    MsgBoxManager.ShowMessage(MsgType.Warning, "Directory Not Empty", Localization.GetString("InitNonEmptyDirectoryAttempt"),
                        delegate
                        {
                            EditorManager.UpdateInfo(new Info());
                            EditorManager.AddDifficulty();
                            EditorManager.Instance.difficulty = 0;
                            UIController.InitSongInfo();

                            ChartManager.InitChart(true);
                        });
                }
                else
                {
                    EditorManager.UpdateInfo(new Info());
                    EditorManager.AddDifficulty();
                    EditorManager.Instance.difficulty = 0;
                    UIController.InitSongInfo();

                    ChartManager.InitChart(true);
                }
            }
            catch (Exception e)
            {
                MsgBoxManager.ShowMessage(MsgType.Error, "Init Directory Failed", e.Message);
                Global.IsDirectorySelected = false;
            }
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
                MsgBoxManager.ShowMessage(MsgType.Info, "Created Successfully", Localization.GetString("CreateChartSuccess"),
                    delegate()
                    {
                        ChartManager.InitChart();
                    });
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