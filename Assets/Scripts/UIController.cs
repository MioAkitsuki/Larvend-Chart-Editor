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
        public float[] vel;

        private Button openInfoMenu;

        private GameObject infoPanel;
        private GameObject gridPanel;
        private TMP_Text audioTime;
        
        private Button saveButton;
        private Button cancelButton;

        // UI under Left Toolbar
        private Button showGridButton;
        private Button enableAbsorptionButton;
        private Button openStepPanelButton;
        private bool isStepPanelOpen;

        // UI under SongPanel
        private RectTransform songPanel;
        private Image albumCover;
        private Button selectFolder;
        private TMP_Text songName;
        private TMP_Text artistName;
        private TMP_Dropdown difficultySelector;

        // UI under StepPanel
        private RectTransform stepPanel;
        private TMP_Dropdown stepSelector;
        private Toggle tripletToggle;
        private Toggle dottedToggle;
        private TMP_Text currentStepStatus;

        // UI under Play Controller
        private Button playSwitchButton;
        [SerializeField] private Sprite[] playAndPause;
        private Button stepBackwardButton;
        private Button stepForwardButton;
        private Button backToBeginningButton;

        private TMP_InputField songNameInputField;
        private TMP_InputField composerInputField;
        private TMP_InputField arrangerInputField;
        private TMP_InputField bpmInputField;
        private TMP_InputField offsetInputField;

        void Start()
        {
            Instance = this;
            vel = new float[2];

            openInfoMenu = this.gameObject.transform.Find("OpenInfoButton").GetComponent<Button>();
            
            infoPanel = this.gameObject.transform.Find("InfoPanel").gameObject;
            gridPanel = this.gameObject.transform.Find("GridPanel").gameObject;
            audioTime = this.gameObject.transform.Find("AudioTime").Find("AudioTimeText").GetComponent<TMP_Text>();
            
            saveButton = infoPanel.transform.Find("SaveInfo").GetComponent<Button>();
            cancelButton = infoPanel.transform.Find("CancelInfo").GetComponent<Button>();

            // UI under Left Toolbar
            showGridButton = this.gameObject.transform.Find("LeftToolbar").Find("ShowGrid").GetComponent<Button>();
            enableAbsorptionButton = this.gameObject.transform.Find("LeftToolbar").Find("EnableAbsorption").GetComponent<Button>();
            openStepPanelButton = this.gameObject.transform.Find("LeftToolbar").Find("OpenStepPanel")
                .GetComponent<Button>();

            showGridButton.onClick.AddListener(SwitchGridStatus);
            enableAbsorptionButton.onClick.AddListener(SwitchAbsorptionStatus);
            openStepPanelButton.onClick.AddListener(ToggleStepPanel);
            showGridButton.gameObject.GetComponent<Image>().color = Color.white;
            enableAbsorptionButton.gameObject.GetComponent<Image>().color = Color.white;
            enableAbsorptionButton.interactable = false;
            isStepPanelOpen = false;

            // UI under SongPanel
            songPanel = this.gameObject.transform.Find("SongPanel").GetComponent<RectTransform>();
            albumCover = this.gameObject.transform.Find("SongPanel").Find("AlbumCover").GetComponent<Image>();
            selectFolder = this.gameObject.transform.Find("SongPanel").Find("AlbumCover").GetComponent<Button>();
            songName = this.gameObject.transform.Find("SongPanel").Find("SongName").GetComponent<TMP_Text>();
            artistName = this.gameObject.transform.Find("SongPanel").Find("ArtistName").GetComponent<TMP_Text>();
            difficultySelector = this.gameObject.transform.Find("SongPanel").Find("DifficultySelector").GetComponent<TMP_Dropdown>();

            selectFolder.onClick.AddListener(SelectFolder);
            difficultySelector.onValueChanged.AddListener(SelectDifficulty);

            // UI under StepPanel
            stepPanel = this.gameObject.transform.Find("StepPanel").GetComponent<RectTransform>();
            stepSelector = this.gameObject.transform.Find("StepPanel").Find("StepSelector")
                .GetComponent<TMP_Dropdown>();
            tripletToggle = this.gameObject.transform.Find("StepPanel").Find("TripletToggle").GetComponent<Toggle>();
            dottedToggle = this.gameObject.transform.Find("StepPanel").Find("DottedToggle").GetComponent<Toggle>();
            currentStepStatus = this.gameObject.transform.Find("StepPanel").Find("CurrentStepStatus")
                .GetComponent<TMP_Text>();

            stepSelector.onValueChanged.AddListener(RefreshStep);
            tripletToggle.onValueChanged.AddListener(RefreshStep);
            dottedToggle.onValueChanged.AddListener(RefreshStep);
            Instance.currentStepStatus.SetText($"1 Step = 1.000 Beat(s)");
            tripletToggle.isOn = false;
            dottedToggle.isOn = false;

            // UI under Info Panel
            songNameInputField = infoPanel.transform.Find("SongNameInfo").Find("SongNameInput").GetComponent<TMP_InputField>();
            composerInputField = infoPanel.transform.Find("ComposerInfo").Find("ComposerInput").GetComponent<TMP_InputField>();
            arrangerInputField = infoPanel.transform.Find("ArrangerInfo").Find("ArrangerInput").GetComponent<TMP_InputField>();
            bpmInputField = infoPanel.transform.Find("BPMInfo").Find("BPMInput").GetComponent<TMP_InputField>();
            offsetInputField = infoPanel.transform.Find("OffsetInfo").Find("OffsetInput").GetComponent<TMP_InputField>();

            // UI under Play Controller
            playSwitchButton = this.gameObject.transform.Find("PlayController").Find("PlaySwitch").GetComponent<Button>();
            stepBackwardButton = this.gameObject.transform.Find("PlayController").Find("StepBackward").GetComponent<Button>();
            stepForwardButton = this.gameObject.transform.Find("PlayController").Find("StepForward").GetComponent<Button>();
            backToBeginningButton = this.gameObject.transform.Find("PlayController").Find("BackToBeginning").GetComponent<Button>();

            playSwitchButton.onClick.AddListener(PlaySwitch);
            backToBeginningButton.onClick.AddListener(EditorManager.ResetAudio);

            openInfoMenu.onClick.AddListener(OpenInfoPanel);
            saveButton.onClick.AddListener(SaveInfo);
            cancelButton.onClick.AddListener(CloseInfoPanel);

            Instance.audioTime.SetText("0");

            infoPanel.SetActive(false);
            gridPanel.SetActive(false);
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
            Instance.audioTime.SetText(EditorManager.GetAudioPCMTime().ToString());
        }

        private static int[] GetStepValue()
        {
            int[] value = new int[] { Instance.stepSelector.value, Convert.ToInt32(Instance.tripletToggle.isOn), Convert.ToInt32(Instance.dottedToggle.isOn) };
            return value;
        }

        private void RefreshStep(int value)
        {
            string res = String.Format("1 Step = {0:N3} Beat(s)", 4.0 / Math.Pow(2, stepSelector.value) * Math.Pow(2f / 3f, Convert.ToDouble(tripletToggle.isOn)) * Math.Pow(3f / 2f, Convert.ToDouble(dottedToggle.isOn)));
            Instance.currentStepStatus.SetText(res);
        }

        private void RefreshStep(bool value)
        {
            string res = String.Format("1 Step = {0:N3} Beat(s)", 4.0 / Math.Pow(2, stepSelector.value) * Math.Pow(2f / 3f, Convert.ToDouble(tripletToggle.isOn)) * Math.Pow(3f / 2f, Convert.ToDouble(dottedToggle.isOn)));
            Instance.currentStepStatus.SetText(res);
        }

        private void PlaySwitch()
        {
            if (!Global.IsAudioLoaded)
            {
                return;
            }

            if (playSwitchButton.gameObject.GetComponent<Image>().sprite == playAndPause[0])
            {
                playSwitchButton.gameObject.GetComponent<Image>().sprite = playAndPause[1];
            }
            else
            {
                playSwitchButton.gameObject.GetComponent<Image>().sprite = playAndPause[0];
            }
        }

        private void SwitchGridStatus()
        {
            gridPanel.SetActive(!gridPanel.activeSelf);
            if (showGridButton.gameObject.GetComponent<Image>().color == Color.white)
            {
                showGridButton.gameObject.GetComponent<Image>().color = new Color(255, 255, 0, 170);
                enableAbsorptionButton.interactable = true;
            }
            else
            {
                showGridButton.gameObject.GetComponent<Image>().color = Color.white;
                enableAbsorptionButton.gameObject.GetComponent<Image>().color = Color.white;
                enableAbsorptionButton.interactable = false;
            }
        }

        private void SwitchAbsorptionStatus()
        {
            if (!gridPanel.activeSelf)
            {
                return;
            }

            if (enableAbsorptionButton.gameObject.GetComponent<Image>().color == Color.white)
            {
                enableAbsorptionButton.gameObject.GetComponent<Image>().color = new Color(255, 255, 0, 170);
            }
            else
            {
                enableAbsorptionButton.gameObject.GetComponent<Image>().color = Color.white;
            }
        }

        private void ToggleStepPanel()
        {
            if (isStepPanelOpen)
            {
                StopCoroutine("openStepPanelEnumerator");
                StartCoroutine("closeStepPanelEnumerator");
            }
            else
            {
                StopCoroutine("closeStepPanelEnumerator");
                StartCoroutine("openStepPanelEnumerator");
            }

            isStepPanelOpen = !isStepPanelOpen;
        }

        IEnumerator openStepPanelEnumerator()
        {
            // Vector2 endPos = new Vector2(-705f, 6.35f);

            float x = Mathf.SmoothDamp(stepPanel.localPosition.x, -705f, ref vel[1], 0.1f, 1000f);
            Vector3 updatePos = new Vector3(x, stepPanel.localPosition.y, 0);
            stepPanel.localPosition = updatePos;

            yield return new WaitForFixedUpdate();
            StartCoroutine("openStepPanelEnumerator");
        }

        IEnumerator closeStepPanelEnumerator()
        {
            // Vector2 endPos = new Vector2(-705f, 6.35f);

            float x = Mathf.SmoothDamp(stepPanel.localPosition.x, -1105f, ref vel[1], 0.1f, 1000f);
            Vector3 updatePos = new Vector3(x, stepPanel.localPosition.y, 0);
            stepPanel.localPosition = updatePos;

            yield return new WaitForFixedUpdate();
            StartCoroutine("closeStepPanelEnumerator");
        }

        public void DropSongPanel()
        {
            StopCoroutine("floatSongPanelEnumerator");
            StartCoroutine("dropSongPanelEnumerator");
        }

        IEnumerator dropSongPanelEnumerator()
        {
            // Vector2 endPos = new Vector2(750f, 449.5f);

            float y = Mathf.SmoothDamp(songPanel.localPosition.y, 449.5f, ref vel[0], 0.1f, 1000f);
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
            // Vector2 endPos = new Vector2(750f, 649.5f);

            float y = Mathf.SmoothDamp(songPanel.localPosition.y, 649.5f, ref vel[0], 0.1f, 1000f);
            Vector3 updatePos = new Vector3(songPanel.localPosition.x, y, 0);
            songPanel.localPosition = updatePos;

            yield return new WaitForFixedUpdate();
            StartCoroutine("floatSongPanelEnumerator");
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