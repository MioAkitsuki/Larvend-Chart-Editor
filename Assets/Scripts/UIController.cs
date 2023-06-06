using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Larvend;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Mathematics;
using UnityEngine.EventSystems;
using Unity.Burst.CompilerServices;

namespace Larvend.Gameplay
{
    public delegate void Callback();
    public class UIController : MonoBehaviour
    {
        public static UIController Instance { get; private set; }
        public float[] vel;
        private int[] beatTick;

        private Camera UICamera;

        private Button openInfoMenu;

        private GameObject infoPanel;
        private GameObject gridPanel;
        private TMP_Text audioTime;
        
        private Button saveButton;
        private Button cancelButton;

        // UI under Left Toolbar
        private Button showSpeedPanelButton;
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
        private TMP_Text currentBPMStatus;

        // UI under SpeedPanel
        private CanvasGroup speedPanel;
        private TMP_InputField speedInput;
        private bool isSpeedInputChanged;
        private Button speedPanelConfirm;
        private Button speedPanelCancel;

        // UI under NotePanel
        private RectTransform notePanel;
        private TMP_Dropdown typeSelector;
        private TMP_InputField timeInput;
        private TMP_InputField posXInput;
        private TMP_InputField posYInput;
        private TMP_InputField endTimeInput;
        private Button deleteNote;

        // UI under Play Controller
        private Button playSwitchButton;
        [SerializeField] private Sprite[] playAndPause;
        private Button stepBackwardButton;
        private Button stepForwardButton;
        private Button backToBeginningButton;
        private Button adjustPointerButton;
        private TMP_Text beatInfo;

        private TMP_InputField songNameInputField;
        private TMP_InputField composerInputField;
        private TMP_InputField arrangerInputField;
        private TMP_InputField bpmInputField;
        private TMP_InputField offsetInputField;

        void Start()
        {
            Instance = this;
            vel = new float[2];
            beatTick = new int[] {1, 0};

            UICamera = GameObject.Find("UICamera").GetComponent<Camera>();

            openInfoMenu = this.gameObject.transform.Find("OpenInfoButton").GetComponent<Button>();
            
            infoPanel = this.gameObject.transform.Find("InfoPanel").gameObject;
            gridPanel = this.gameObject.transform.Find("GridPanel").gameObject;
            audioTime = this.gameObject.transform.Find("AudioTime").Find("AudioTimeText").GetComponent<TMP_Text>();
            
            saveButton = infoPanel.transform.Find("SaveInfo").GetComponent<Button>();
            cancelButton = infoPanel.transform.Find("CancelInfo").GetComponent<Button>();

            // UI under Left Toolbar
            showSpeedPanelButton = this.gameObject.transform.Find("LeftToolbar").Find("OpenSpeedPanel").GetComponent<Button>();
            showGridButton = this.gameObject.transform.Find("LeftToolbar").Find("ShowGrid").GetComponent<Button>();
            enableAbsorptionButton = this.gameObject.transform.Find("LeftToolbar").Find("EnableAbsorption").GetComponent<Button>();
            openStepPanelButton = this.gameObject.transform.Find("LeftToolbar").Find("OpenStepPanel")
                .GetComponent<Button>();

            showSpeedPanelButton.onClick.AddListener(ToggleSpeedPanel);
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
            currentBPMStatus = this.gameObject.transform.Find("StepPanel").Find("CurrentBPMStatus")
                .GetComponent<TMP_Text>();

            stepSelector.onValueChanged.AddListener(RefreshStep);
            tripletToggle.onValueChanged.AddListener(RefreshStep);
            dottedToggle.onValueChanged.AddListener(RefreshStep);
            currentStepStatus.SetText($"1 Step = 1.000 Beat(s)");
            currentBPMStatus.SetText("Chart Unloaded");
            tripletToggle.isOn = false;
            dottedToggle.isOn = false;

            // UI under SpeedPanel
            speedPanel = this.gameObject.transform.Find("SpeedPanel").GetComponent<CanvasGroup>();
            speedInput = this.gameObject.transform.Find("SpeedPanel").Find("EditArea").GetComponent<TMP_InputField>();
            speedPanelConfirm = this.gameObject.transform.Find("SpeedPanel").Find("Confirm").GetComponent<Button>();
            speedPanelCancel = this.gameObject.transform.Find("SpeedPanel").Find("Cancel").GetComponent<Button>();

            speedPanel.alpha = 0;
            speedPanel.gameObject.SetActive(false);
            speedInput.onValueChanged.AddListener((value) => { isSpeedInputChanged = true; });
            speedPanelConfirm.onClick.AddListener(SaveSpeedEvent);
            speedPanelCancel.onClick.AddListener((() => StartCoroutine("closeSpeedPanelEnumerator")));

            // UI under NotePanel
            notePanel = this.gameObject.transform.Find("NotePanel").GetComponent<RectTransform>();
            typeSelector = this.gameObject.transform.Find("NotePanel").Find("TypeSelector").GetComponent<TMP_Dropdown>();
            timeInput = this.gameObject.transform.Find("NotePanel").Find("TimeInput").GetComponent<TMP_InputField>();
            posXInput = this.gameObject.transform.Find("NotePanel").Find("PosXInput").GetComponent<TMP_InputField>();
            posYInput = this.gameObject.transform.Find("NotePanel").Find("PosYInput").GetComponent<TMP_InputField>();
            endTimeInput = this.gameObject.transform.Find("NotePanel").Find("EndTimeInput").GetComponent<TMP_InputField>();
            deleteNote = this.gameObject.transform.Find("NotePanel").Find("DeleteNote").GetComponent<Button>();

            notePanel.gameObject.SetActive(false);

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
            adjustPointerButton = this.gameObject.transform.Find("PlayController").Find("AdjustPointer").GetComponent<Button>();
            beatInfo = this.gameObject.transform.Find("BeatInfo").Find("CurrentBeat").GetComponent<TMP_Text>();

            playSwitchButton.onClick.AddListener(PlaySwitch);
            stepBackwardButton.onClick.AddListener(StepBackward);
            stepForwardButton.onClick.AddListener(StepForward);
            backToBeginningButton.onClick.AddListener(ResetPointer);
            adjustPointerButton.onClick.AddListener(AdjustPointer);
            beatInfo.SetText("Audio Unloaded");

            openInfoMenu.onClick.AddListener(OpenInfoPanel);
            saveButton.onClick.AddListener(SaveInfo);
            cancelButton.onClick.AddListener(CloseInfoPanel);

            infoPanel.SetActive(false);
            gridPanel.SetActive(false);
        }

        private void Update()
        {
            if (EditorManager.isAudioPlaying)
            {
                RefreshUI();
            }
            if (Input.GetKeyUp(KeyCode.RightArrow) && Global.IsAudioLoaded && !EditorManager.isAudioPlaying && !Global.IsDialoging )
            {
                StepForward();
            }
            if (Input.GetKeyUp(KeyCode.LeftArrow) && Global.IsAudioLoaded && !EditorManager.isAudioPlaying && !Global.IsDialoging )
            {
                StepBackward();
            }
            
            if (Input.GetMouseButtonUp(1))
            {
                Collider2D col = Physics2D.OverlapPoint(Camera.main.ScreenToWorldPoint(Input.mousePosition), LayerMask.GetMask("Note"));

                if (col != null)
                {
                    Note note = col.gameObject.GetComponent<Note>();
                    typeSelector.value = (int) note.type;
                    timeInput.text = $"{note.time}";
                    posXInput.text = $"{note.position.x}";
                    posYInput.text = $"{note.position.y}";

                    if (note.type == Type.Hold)
                    {
                        endTimeInput.text = $"{note.endTime}";
                        endTimeInput.interactable = true;
                    }
                    else
                    {
                        endTimeInput.text = "";
                        endTimeInput.interactable = false;
                    }

                    Vector2 panelPos = (Vector2) Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    
                    if (panelPos.y < -1.5)
                    {
                        panelPos += new Vector2(0, 2.8f);
                    }
                    if (panelPos.x > 6)
                    {
                        panelPos -= new Vector2(2.8f, 0);
                    }

                    notePanel.position = panelPos;
                    notePanel.gameObject.SetActive(true);
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                if (Physics2D.OverlapPoint(Camera.main.ScreenToWorldPoint(Input.mousePosition)) != null)
                {
                    if (notePanel.gameObject.activeSelf)
                    {
                        notePanel.gameObject.SetActive(false);
                    }
                }
            }
        }

        public static void RefreshUI()
        {
            Instance.audioTime.SetText(EditorManager.GetAudioPCMTime().ToString());
        }

        private static int[] GetStep()
        {
            int[] value = new int[] {Instance.stepSelector.value, Convert.ToInt32(Instance.tripletToggle.isOn), Convert.ToInt32(Instance.dottedToggle.isOn)};
            return value;
        }

        private void RefreshStep(int value)
        {
            double step = 4.0 / Math.Pow(2, stepSelector.value) *
                          Math.Pow(2f / 3f, Convert.ToDouble(tripletToggle.isOn)) *
                          Math.Pow(3f / 2f, Convert.ToDouble(dottedToggle.isOn));
            string res = String.Format("1 Step = {0:N3} Beat(s)", step);
            Instance.currentStepStatus.SetText(res);
            EditorManager.SetStep(step);
        }

        private void RefreshStep(bool value)
        {
            double step = 4.0 / Math.Pow(2, stepSelector.value) *
                          Math.Pow(2f / 3f, Convert.ToDouble(tripletToggle.isOn)) *
                          Math.Pow(3f / 2f, Convert.ToDouble(dottedToggle.isOn));
            string res = String.Format("1 Step = {0:N3} Beat(s)", step);
            Instance.currentStepStatus.SetText(res);
            EditorManager.SetStep(step);
        }

        /// <summary>
        /// Switch the pause and play status.
        /// </summary>
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

        private void StepBackward()
        {
            if (!Global.IsAudioLoaded || EditorManager.isAudioPlaying)
            {
                return;
            }

            int ticks = Convert.ToInt32(3840 / Math.Pow(2, stepSelector.value) *
                                        Math.Pow(2f / 3f, Convert.ToDouble(tripletToggle.isOn)) *
                                        Math.Pow(3f / 2f, Convert.ToDouble(dottedToggle.isOn)));

            if (beatTick[0] * 960 + beatTick[1] - ticks < 0)
            {
                beatInfo.SetText("1:000");
            }
            else
            {
                int res = beatTick[0] * 960 + beatTick[1] - ticks;
                beatTick[0] = res / 960;
                beatTick[1] = res % 960;
                beatInfo.SetText($"{beatTick[0]}: {Convert.ToString(beatTick[1]).PadLeft(3, '0')}");
            }

            EditorManager.StepBackward();
        }

        private void StepForward()
        {
            if (!Global.IsAudioLoaded || EditorManager.isAudioPlaying)
            {
                return;
            }

            int ticks = Convert.ToInt32(3840 / Math.Pow(2, stepSelector.value) *
                                        Math.Pow(2f / 3f, Convert.ToDouble(tripletToggle.isOn)) *
                                        Math.Pow(3f / 2f, Convert.ToDouble(dottedToggle.isOn)));

            int res = beatTick[0] * 960 + beatTick[1] + ticks;
            beatTick[0] = res / 960;
            beatTick[1] = res % 960;
            beatInfo.SetText($"{beatTick[0]}: {Convert.ToString(beatTick[1]).PadLeft(3, '0')}");

            EditorManager.StepForward();
        }

        private void AdjustPointer()
        {
            if (Global.IsAudioLoaded && !Global.IsDialoging)
            {
                beatInfo.SetText($"{beatTick[0]}: 000");
                EditorManager.AdjustPointer(beatTick[0] * 960);
            }
        }

        private void ResetPointer()
        {
            if (Global.IsAudioLoaded && !Global.IsDialoging)
            {
                audioTime.SetText("0");
                beatInfo.SetText("1: 000");
                EditorManager.ResetAudio();
            }
        }

        private void SaveSpeedEvent()
        {
            List<string> list = new List<string>(speedInput.text.Split('\n'));
            NoteManager.UpdateSpeedEvents(list);
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

        private void ToggleSpeedPanel()
        {
            if (speedPanel.gameObject.activeSelf)
            {
                StopCoroutine("openSpeedPanelEnumerator");
                StartCoroutine("closeSpeedPanelEnumerator");
                Global.IsDialoging = false;
            }
            else
            {
                speedPanel.gameObject.SetActive(true);
                StopCoroutine("closeSpeedPanelEnumerator");
                StartCoroutine("openSpeedPanelEnumerator");
                Global.IsDialoging = true;
            }
        }

        IEnumerator openSpeedPanelEnumerator()
        {
            speedPanel.alpha = Mathf.Lerp(speedPanel.alpha, 1f, 0.2f);

            if (speedPanel.alpha > 0.98f)
            {
                speedPanel.alpha = 1;
                yield break;
            }

            yield return new WaitForFixedUpdate();
            StartCoroutine("openSpeedPanelEnumerator");
        }

        IEnumerator closeSpeedPanelEnumerator()
        {
            speedPanel.alpha = Mathf.Lerp(speedPanel.alpha, 0f, 0.2f);

            if (speedPanel.alpha < 0.02f)
            {
                speedPanel.alpha = 0;
                speedPanel.gameObject.SetActive(false);
                yield break;
            }

            yield return new WaitForFixedUpdate();
            StartCoroutine("closeSpeedPanelEnumerator");
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

        public static void InitSpeedPanel(List<String> lines)
        {
            string res = "";
            foreach (var line in lines)
            {
                res += line + '\n';
            }
            Instance.speedInput.text = res;
        }

        public static void InitAudioLabel(float length)
        {
            Instance.audioTime.SetText("0");
            Instance.beatInfo.SetText("1: 000");
            Instance.currentBPMStatus.SetText(EditorManager.GetBPM().ToString());
            EditorManager.SetStep(1);
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