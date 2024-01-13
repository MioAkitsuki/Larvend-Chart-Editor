using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Larvend.Gameplay;
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
        internal float scale;
        internal float sustainSection;

        internal Line(float bpm)
        {
            type = Type.SpeedAdjust;
            time = 0;
            endTime = 0;
            targetBpm = bpm;
            scale = 1f;
        }

        internal Line(Note note)
        {
            type = note.type;
            time = note.time;
            position = note.position;
            endTime = note.endTime;
            scale = note.scale;
        }

        internal Line(Type type, int time, Vector2 pos)
        {
            this.type = type;
            this.time = time;
            this.position = pos;
            scale = 1f;
        }

        internal Line(Type type, int time, Vector2 pos, int endTime)
        {
            this.type = type;
            this.time = time;
            this.position = pos;
            this.endTime = endTime;
            scale = 1f;
        }

        internal Line(Type type, int time, float targetBpm, int endTime)
        {
            this.type = type;
            this.time = time;
            this.targetBpm = targetBpm;
            this.endTime = endTime;
            sustainSection = 1;
        }

        internal Line(Type type, int time, float targetBpm, int endTime, int sustainSection)
        {
            this.type = type;
            this.time = time;
            this.targetBpm = targetBpm;
            this.endTime = endTime;
            this.sustainSection = sustainSection;
        }

        internal Line(Type type, int time, Vector2 pos, float scale)
        {
            this.type = type;
            this.time = time;
            this.position = pos;
            this.scale = scale;
        }

        internal Line(Type type, int time, Vector2 pos, int endTime, float scale)
        {
            this.type = type;
            this.time = time;
            this.position = pos;
            this.endTime = endTime;
            this.scale = scale;
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
            path = Global.FolderPath + $"/{(Difficulties)difficulty}.bytes";
            notes = new();
            isNotesReading = false;
            isBaseBpmReading = false;

            if (!File.Exists(path))
            {
                InitChart();
                return;
            }

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
                            MsgBoxManager.ShowMessage(MsgType.Warning, "Warning", Localization.GetString("ChartVersionNotExpected"));
                        }

                        continue;
                    }

                    if (line.Contains("offset="))
                    {
                        EditorManager.SetOffset(Int32.Parse(line.Replace("offset=", "")));
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
                NoteManager.Instance.StartCoroutine(NoteManager.Instance.RefreshSpeedEnumerator());
                
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
            int sustainSection;
            float scale;

            string[] splitedLine = line.Split(new [] { '(', ')' });
            string[] lineInfo = splitedLine[1].Split(',');

            switch (splitedLine[0]) 
            { 
                case "tap" when lineInfo.Length == 3:
                    LineDivider(splitedLine[1], out time, out x, out y);
                    return new Line(Type.Tap, time, new Vector2(x, y));
                case "tap" when lineInfo.Length == 4:
                    LineDivider(splitedLine[1], out time, out x, out y, out scale);
                    return new Line(Type.Tap, time, new Vector2(x, y), scale);
                case "hold" when lineInfo.Length == 4:
                    LineDivider(splitedLine[1], out time, out x, out y, out endTime);
                    return new Line(Type.Hold, time, new Vector2(x, y), endTime);
                case "hold" when lineInfo.Length == 5:
                    LineDivider(splitedLine[1], out time, out x, out y, out endTime, out scale);
                    return new Line(Type.Hold, time, new Vector2(x, y), endTime, scale);
                case "flick" when lineInfo.Length == 3:
                    LineDivider(splitedLine[1], out time, out x, out y);
                    return new Line(Type.Flick, time, new Vector2(x, y));
                case "flick" when lineInfo.Length == 4:
                    LineDivider(splitedLine[1], out time, out x, out y, out scale);
                    return new Line(Type.Flick, time, new Vector2(x, y), scale);
                case "speed" when lineInfo.Length == 3:
                    LineDivider(splitedLine[1], out time, out targetBpm, out endTime);
                    return new Line(Type.SpeedAdjust, time, targetBpm, endTime);
                case "speed" when lineInfo.Length == 4:
                    LineDivider(splitedLine[1], out time, out targetBpm, out endTime, out sustainSection);
                    return new Line(Type.SpeedAdjust, time, targetBpm, endTime, sustainSection);
                default:
                    throw new Exception(Localization.GetString("UnknownNoteType") + $"\n{line}");
            }
        }

        public static void WriteChart(int difficulty)
        {
            path = Global.FolderPath + $"/{(Difficulties)difficulty}.bytes";
            notes = NoteManager.GetAllNotes();

            if (!File.Exists(path))
            {
                File.Create(path).Dispose();
            }

            try
            {
                StreamWriter chartWriter = new StreamWriter(path);

                if (isNotesWriting)
                    throw new Exception("There was an unfinished writing process.");

                chartWriter.WriteLine($"version={Global.ChartVersion}");
                chartWriter.WriteLine($"offset={EditorManager.Instance.offset}\n");

                chartWriter.WriteLine("[NOTES]");
                chartWriter.WriteLine($"speed(0,{NoteManager.Instance.BaseSpeed.targetBpm},0,1)");

                if (notes.Count > 0)
                {
                    WriteNotes(chartWriter);
                }

                chartWriter.WriteLine("[END]");
                chartWriter.Close();
            }
            catch (Exception e)
            {
                MsgBoxManager.ShowMessage(MsgType.Error, "Written Failed", e.Message);
                Debug.LogError(e);
            }
        }

        public static void Backup()
        {
            string path = $"{Global.FolderPath}/Backups/{Enum.GetName(typeof(Difficulties), EditorManager.Instance.difficulty)}_{System.DateTime.Now.ToString("yyyyMMddHHmmss")}.bytes";
            notes = NoteManager.GetAllNotes();

            if (!Directory.Exists($"{Global.FolderPath}/Backups"))
            {
                Directory.CreateDirectory($"{Global.FolderPath}/Backups");
            }

            if (!File.Exists(path))
            {
                File.Create(path).Dispose();
            }

            try
            {
                StreamWriter chartWriter = new StreamWriter(path);

                if (isNotesWriting)
                    throw new Exception("There was an unfinished writing process.");

                chartWriter.WriteLine($"version={Global.ChartVersion}");
                chartWriter.WriteLine($"offset={EditorManager.Instance.offset}\n");

                chartWriter.WriteLine("[NOTES]");
                chartWriter.WriteLine($"speed(0,{NoteManager.Instance.BaseSpeed.targetBpm},0,1)");

                if (notes.Count > 0)
                {
                    WriteNotes(chartWriter);
                }

                chartWriter.WriteLine("[END]");
                chartWriter.Close();
            }
            catch (Exception e)
            {
                // MsgBoxManager.ShowMessage(MsgType.Error, "Backup Failed", e.Message);
                Debug.LogError(e);
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
                        chartWriter.WriteLine($"tap({note.time},{note.position.x:N2},{note.position.y:N2},{note.scale:N2})");
                        continue;
                    case Type.Hold:
                        chartWriter.WriteLine($"hold({note.time},{note.position.x:N2},{note.position.y:N2},{note.endTime},{note.scale:N2})");
                        continue;
                    case Type.Flick:
                        chartWriter.WriteLine($"flick({note.time},{note.position.x:N2},{note.position.y:N2},{note.scale:N2})");
                        continue;
                    case Type.SpeedAdjust:
                        chartWriter.WriteLine($"speed({note.time},{note.targetBpm:N2},{note.endTime},{note.sustainSection})");
                        continue;
                }
            }
            
            isNotesWriting = false;
        }

        public static void InitProject()
        {
            UIController.Instance.InitDifficultySelector();

            System.Diagnostics.Process.Start(Global.FolderPath);
            Global.IsDirectorySelected = true;
            Global.IsFileSelected = true;

            EditorManager.Instance.StartCoroutine(ImageManager.LoadImg());
        }

        public static void InitChart(params bool[] param)
        {
            NoteManager.ClearAllNotes();
            try
            {
                MsgBoxManager.ShowInputDialog("Initialize Chart", "Please Set Base BPM", delegate(string value)
                {
                    NoteManager.UpdateBaseBpm(Single.Parse(value));
                    EditorManager.Instance.InitializeBPM(Single.Parse(value));

                    if (param.Length > 0 && param[0])
                    {
                        UIController.Instance.InitDifficultySelector();

                        System.Diagnostics.Process.Start(Global.FolderPath);
                        Global.IsDirectorySelected = true;
                        Global.IsFileSelected = true;

                        EditorManager.Instance.StartCoroutine(ImageManager.LoadImg());
                    }
                    else
                    {
                        MsgBoxManager.ShowMessage(MsgType.Info, "Initialize Success",
                            Localization.GetString("InitializeChartSuccess"));
                    }

                    WriteChart(EditorManager.Instance.difficulty);
                }, AbortInitChart);
            }
            catch (Exception e)
            {
                MsgBoxManager.ShowMessage(MsgType.Error, "Error in Initializing Chart", e.Message);
                Debug.LogError(e);
            }
            MsgBoxManager.ShowMessage(MsgType.Info, "Initialize Directory Successfully",
                Localization.GetString("InitDirectorySuccess"));
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

        private static void LineDivider(string line, out int arg1, out float arg2, out int arg3, out int arg4)
        {
            string[] splitedLine = line.Split(',');
            arg1 = int.Parse(splitedLine[0]);
            arg2 = float.Parse(splitedLine[1]);
            arg3 = int.Parse(splitedLine[2]);
            arg4 = int.Parse(splitedLine[3]);
        }

        private static void LineDivider(string line, out int arg1, out float arg2, out float arg3, out int arg4)
        {
            string[] splitedLine = line.Split(',');
            arg1 = int.Parse(splitedLine[0]);
            arg2 = float.Parse(splitedLine[1]);
            arg3 = float.Parse(splitedLine[2]);
            arg4 = int.Parse(splitedLine[3]);
        }

        private static void LineDivider(string line, out int arg1, out float arg2, out float arg3, out float arg4)
        {
            string[] splitedLine = line.Split(',');
            arg1 = int.Parse(splitedLine[0]);
            arg2 = float.Parse(splitedLine[1]);
            arg3 = float.Parse(splitedLine[2]);
            arg4 = float.Parse(splitedLine[3]);
        }

        private static void LineDivider(string line, out int arg1, out float arg2, out float arg3, out int arg4, out float arg5)
        {
            string[] splitedLine = line.Split(',');
            arg1 = int.Parse(splitedLine[0]);
            arg2 = float.Parse(splitedLine[1]);
            arg3 = float.Parse(splitedLine[2]);
            arg4 = int.Parse(splitedLine[3]);
            arg5 = float.Parse(splitedLine[4]);
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
                            case "Light.bytes":
                                isChartExist = true;
                                continue;
                            case "Order.bytes":
                                isChartExist = true;
                                continue;
                            case "Chaos.bytes":
                                isChartExist = true;
                                continue;
                            case "Error.bytes":
                                isChartExist = true;
                                continue;
                            case "full.ogg":
                                EditorManager.Instance.StartCoroutine(AudioManager.LoadAudio());
                                continue;
                            case "preview.ogg":
                                continue;
                            case "cover.jpg":
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
                        InfoManager.WriteInfo();
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

                    Global.IsDirectorySelected = true;
                }
            }
            catch (Exception e)
            {
                MsgBoxManager.ShowMessage(MsgType.Error, "Load Directory Failed", e.Message);
                Debug.LogException(e);
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
                    DirectoryInfo dir = new DirectoryInfo(Global.FolderPath);
                    FileInfo[] files = dir.GetFiles();
                    MsgBoxManager.ShowMessage(MsgType.Warning, "Directory Not Empty", Localization.GetString("InitNonEmptyDirectoryAttempt"),
                        delegate
                        {
                            EditorManager.UpdateInfo(new Info());
                            EditorManager.AddDifficulty();
                            EditorManager.Instance.difficulty = 0;
                            UIController.InitSongInfo();

                            ChartManager.InitProject();
                        }, () => Global.IsDirectorySelected = false);
                }
                else
                {
                    AudioManager.SelectAudio();

                    EditorManager.UpdateInfo(new Info());
                    EditorManager.AddDifficulty();
                    EditorManager.Instance.difficulty = 0;
                    UIController.InitSongInfo();

                    ChartManager.InitProject();
                }

                Global.IsDirectorySelected = true;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                Global.IsDirectorySelected = false;
            }
        }

        public static void CreateChart(int difficulty)
        {
            string path = $"{Global.FolderPath}/{(Difficulties)difficulty}.bytes";

            if (File.Exists(path))
            {
                MsgBoxManager.ShowMessage(MsgType.Warning, "Already Exists", "Already exist a chart in this difficulty. Do you want to replace it?",
                    delegate()
                    {
                        ChartManager.InitChart();
                    });
            }
            else
            {
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
        public static void SelectAudio()
        {
            try
            {
                string audioPath = Schwarzer.Windows.Dialog.OpenFileDialog("Select Audio");
                if (File.Exists(audioPath))
                {
                    string fileName, destFile;
                    if (audioPath[^3..] == "ogg")
                    {
                        fileName = Path.GetFileName(audioPath);
                        destFile = Path.Combine(Global.FolderPath, "full.ogg");
                        File.Copy(audioPath, destFile, true);
                        EditorManager.Instance.StartCoroutine(LoadAudio());
                    }
                    else
                    {
                        throw new Exception("Unsupported Format.");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        public static IEnumerator LoadAudio()
        {
            AudioClip clip = null;
            string path = Path.Combine(Global.FolderPath, "full.ogg");

            if (!File.Exists(path))
            {
                yield break;
            }

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
            
            Global.IsAudioLoaded = true;
            EditorManager.InitAudio(clip);
            NoteManager.RefreshAllNotes();
        }
    }

    public class ImageManager
    {
        public static IEnumerator LoadImg()
        {
            Texture2D texture = null;
            string path = Path.Combine(Global.FolderPath, "cover.jpg");

            if (!File.Exists(path))
            {
                yield break;
            }

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