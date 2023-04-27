using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Larvend;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Larvend.Gameplay
{
    public delegate void Callback();
    public class UIController : MonoBehaviour
    {
        public static UIController Instance { get; private set; }
        public float vel;

        private Button openInfoMenu;
        private TMP_Dropdown difficultySelector;

        private GameObject infoPanel;
        private GameObject gridPanel;
        private TMP_Text audioTime;
        private TMP_Text timePlayed;
        private TMP_Text timeTotal;
        private Slider slider;
        private Button saveButton;
        private Button cancelButton;
        private Toggle showGridToggle;
        private Toggle enableAdsorptionToggle;

        // UI under SongPanel
        private RectTransform songPanel;
        private Image albumCover;
        private Button selectFolder;
        private TMP_Text songName;
        private TMP_Text artistName;

        private TMP_InputField songNameInputField;
        private TMP_InputField composerInputField;
        private TMP_InputField arrangerInputField;
        private TMP_InputField bpmInputField;
        private TMP_InputField offsetInputField;

        void Start()
        {
            Instance = this;

            openInfoMenu = this.gameObject.transform.Find("OpenInfoButton").GetComponent<Button>();
            
            infoPanel = this.gameObject.transform.Find("InfoPanel").gameObject;
            gridPanel = this.gameObject.transform.Find("GridPanel").gameObject;
            audioTime = this.gameObject.transform.Find("AudioTime").Find("AudioTimeText").GetComponent<TMP_Text>();
            timePlayed = this.gameObject.transform.Find("Timer").Find("TimePlayed").GetComponent<TMP_Text>();
            timeTotal = this.gameObject.transform.Find("Timer").Find("TimeTotal").GetComponent<TMP_Text>();
            slider = this.gameObject.transform.Find("Slider").GetComponent<Slider>();
            saveButton = infoPanel.transform.Find("SaveInfo").GetComponent<Button>();
            cancelButton = infoPanel.transform.Find("CancelInfo").GetComponent<Button>();
            showGridToggle = this.gameObject.transform.Find("ShowGrid").GetComponent<Toggle>();
            enableAdsorptionToggle = this.gameObject.transform.Find("EnableAdsorption").GetComponent<Toggle>();

            // UI under SongPanel
            songPanel = this.gameObject.transform.Find("SongPanel").GetComponent<RectTransform>();
            albumCover = this.gameObject.transform.Find("SongPanel").Find("AlbumCover").GetComponent<Image>();
            selectFolder = this.gameObject.transform.Find("SongPanel").Find("AlbumCover").GetComponent<Button>();
            songName = this.gameObject.transform.Find("SongPanel").Find("SongName").GetComponent<TMP_Text>();
            artistName = this.gameObject.transform.Find("SongPanel").Find("ArtistName").GetComponent<TMP_Text>();
            difficultySelector = this.gameObject.transform.Find("SongPanel").Find("DifficultySelector").GetComponent<TMP_Dropdown>();

            // UI under Info Panel
            songNameInputField = infoPanel.transform.Find("SongNameInfo").Find("SongNameInput").GetComponent<TMP_InputField>();
            composerInputField = infoPanel.transform.Find("ComposerInfo").Find("ComposerInput").GetComponent<TMP_InputField>();
            arrangerInputField = infoPanel.transform.Find("ArrangerInfo").Find("ArrangerInput").GetComponent<TMP_InputField>();
            bpmInputField = infoPanel.transform.Find("BPMInfo").Find("BPMInput").GetComponent<TMP_InputField>();
            offsetInputField = infoPanel.transform.Find("OffsetInfo").Find("OffsetInput").GetComponent<TMP_InputField>();

            selectFolder.onClick.AddListener(SelectFolder);
            openInfoMenu.onClick.AddListener(OpenInfoPanel);
            difficultySelector.onValueChanged.AddListener(SelectDifficulty);
            saveButton.onClick.AddListener(SaveInfo);
            cancelButton.onClick.AddListener(CloseInfoPanel);
            showGridToggle.onValueChanged.AddListener((bool isOn) => { TriggerGrid(isOn); });
            slider.onValueChanged.AddListener((float value) => { TriggerTime(value); });

            Instance.audioTime.SetText("0");
            Instance.timePlayed.SetText("00:00.000");
            Instance.timeTotal.SetText("/  00:00.000");

            infoPanel.SetActive(false);
            gridPanel.SetActive(false);
            showGridToggle.isOn = false;
            enableAdsorptionToggle.isOn = false;
        }

        private void Update()
        {
            if (EditorManager.isAudioPlaying)
            {
                RefreshUI();
            }
        }

        public static void RefreshUI()
        {
            var time = Global.TimeFormat(EditorManager.GetAudioTime());
            Instance.timePlayed.SetText($"{time[0]}:{time[1]}.{time[2]}");

            Instance.audioTime.SetText(EditorManager.GetAudioPCMTime().ToString());

            Instance.slider.value = EditorManager.GetAudioTime() / EditorManager.GetAudioLength();
        }

        public void DropSongPanel()
        {
            StopCoroutine("floatSongPanelEnumerator");
            StartCoroutine("dropSongPanelEnumerator");
        }

        IEnumerator dropSongPanelEnumerator()
        {
            Vector2 endPos = new Vector2(750f, 449.5f);

            float y = Mathf.SmoothDamp(songPanel.localPosition.y, 449.5f, ref vel, 0.1f, 1000f);
            Vector3 updatePos = new Vector3(songPanel.localPosition.x, y, 0);
            songPanel.localPosition = updatePos;
            
            yield return new WaitForFixedUpdate();
            StartCoroutine("dropSongPanelEnumerator");
        }

        public void FloatSongPanel()
        {
            if (difficultySelector.IsExpanded)
            {
                return;
            }
            StopCoroutine("dropSongPanelEnumerator");
            StartCoroutine("floatSongPanelEnumerator");
        }
        IEnumerator floatSongPanelEnumerator()
        {
            Vector2 endPos = new Vector2(750f, 649.5f);

            float y = Mathf.SmoothDamp(songPanel.localPosition.y, 649.5f, ref vel, 0.1f, 1000f);
            Vector3 updatePos = new Vector3(songPanel.localPosition.x, y, 0);
            songPanel.localPosition = updatePos;

            yield return new WaitForFixedUpdate();
            StartCoroutine("floatSongPanelEnumerator");
        }

        private void TriggerTime(float value)
        {
            var time = Global.TimeFormat(value * EditorManager.GetAudioLength());
            Instance.timePlayed.SetText($"{time[0]}:{time[1]}.{time[2]}");

            EditorManager.SetPlayTime(value * EditorManager.GetAudioLength());

            Instance.audioTime.SetText(EditorManager.GetAudioPCMTime().ToString());
        }

        private void TriggerGrid(bool state)
        {
            gridPanel.SetActive(state);
        }

        private void SelectDifficulty(int diff)
        {
            if (!Global.IsSaved)
            {
                MsgBoxManager.ShowMessage(MsgType.Warning, "Warning", "The chart is unsaved, do you want to save it?\nÆ×ÃæÎ´±£´æ£¬ÄúÏ£Íû±£´æÂð£¿");
            }

            if (Global.Difficulties[diff])
            {
                ChartManager.ReadChart(diff);
                EditorManager.ResetAudio();
                RefreshUI();
            }
            else
            {
                DirectoryManager.CreateChart(diff);
                difficultySelector.options[diff].text = Enum.GetName(typeof(Chart.Difficulties), diff);
                difficultySelector.gameObject.transform.Find("Label").GetComponent<TMP_Text>().text = Enum.GetName(typeof(Chart.Difficulties), diff);
            }
        }

        private void InitDifficultySelector()
        {
            difficultySelector.ClearOptions();

            for (int i = 0; i < 4; i++)
            {
                TMP_Dropdown.OptionData op = new TMP_Dropdown.OptionData();
                if (Global.Difficulties[i])
                    op.text = Enum.GetName(typeof(Chart.Difficulties), i);
                else
                    op.text = "Create " + Enum.GetName(typeof(Chart.Difficulties), i);
                difficultySelector.options.Add(op);
            }

            difficultySelector.gameObject.transform.Find("Label").GetComponent<TMP_Text>().text = "Light";
            ChartManager.ReadChart(0);
            Global.IsFileSelected = true;
        }

        void SelectFolder()
        {
            string path = Schwarzer.Windows.Dialog.OpenFolderDialog("Select Folder", "/");
            if (path != null && Global.FolderPath != path)
            {
                Global.FolderPath = path;
                Global.IsDirectorySelected = true;
                DirectoryManager.ReadFolder();
                StartCoroutine(AudioManager.LoadAudio());
                StartCoroutine(ImageManager.LoadImg());
                InitDifficultySelector();
                InitSongInfo();
            }
        }

        public static void InitAudioLabel(float length)
        {
            Instance.audioTime.SetText("0");
            Instance.timePlayed.SetText("00:00.000");

            var time = Global.TimeFormat(length);
            Instance.timeTotal.SetText($"/  {time[0]}:{time[1]}.{time[2]}");
        }

        public static void InitAlbumCover(Sprite sprite)
        {
            Instance.albumCover.sprite = sprite;
        }

        public static void InitSongInfo()
        {
            Instance.songName.text = Global.Chart.title == null ? "Sample Song" : Global.Chart.title;
            Instance.artistName.text = Global.Chart.composer == null ? "Artist: Sample Artist" : "Artist: " + Global.Chart.composer;
            Instance.difficultySelector.interactable = true;
        }

        void OpenInfoPanel()
        {
            if (Global.IsFileSelected)
            {
                UpdateInfo();
                infoPanel.SetActive(true);
            }
        }

        private void UpdateInfo()
        {
            songName.text = Global.Chart.title;
            songNameInputField.text = Global.Chart.title;
            artistName.text = "Artist: " + Global.Chart.composer;
            composerInputField.text = Global.Chart.composer;
            arrangerInputField.text = Global.Chart.arranger;
            bpmInputField.text = Global.Chart.bpm.ToString();
            offsetInputField.text = Global.Chart.offset.ToString();
        }

        public void CloseInfoPanel()
        {
            infoPanel.SetActive(false);
        }

        void SaveInfo()
        {
            Global.Chart.UpdateChartInfo(songNameInputField.text, composerInputField.text, arrangerInputField.text,
                float.Parse(bpmInputField.text, CultureInfo.InvariantCulture.NumberFormat),
                float.Parse(offsetInputField.text, CultureInfo.InvariantCulture.NumberFormat));
        }
    }
}