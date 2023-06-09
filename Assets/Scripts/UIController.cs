using System;
using System.Collections;
using System.Collections.Generic;
using Larvend;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Larvend.Gameplay
{
    public delegate void Callback();

    public delegate void Callback<T>(T obj);
    public class UIController : MonoBehaviour
    {
        public static UIController Instance { get; private set; }
        public float[] vel;
        private int[] beatTick;

        private GameObject gridPanel;

        // UI under Top Bar
        private TMP_Text audioTime;
        private Button newProject;
        private Button openProject;
        private Button saveProject;
        private Button openSettings;
        private Button deleteAllNotes;

        // UI under Settings Panel
        private CanvasGroup settingsPanel;
        private Button saveSettings;
        private Button cancelSettings;

        private bool isInfoEdited;
        private TMP_InputField titleInputField;
        private TMP_InputField composerInputField;
        private TMP_InputField arrangerInputField;
        private TMP_InputField offsetInputField;
        private TMP_InputField baseBpmInputField;
        private TMP_InputField ratingInputField;

        private TMP_Dropdown languageSelector;
        private Dictionary<int, string> languageDictionary;
        private Button resetPlayerPrefs;

        // UI under Left Toolbar
        private Button createTapButton;
        private Button createHoldButton;
        private Button createFlickButton;
        private Button showSpeedPanelButton;
        private Button showGridButton;
        private Button enableAbsorptionButton;
        private Button openStepPanelButton;
        private bool isStepPanelOpen;

        // UI under SongPanel
        private RectTransform songPanel;
        private Image albumCover;
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
        public bool isSpeedInputChanged;
        private Button speedPanelConfirm;
        private Button speedPanelCancel;

        // UI under NotePanel
        private RectTransform notePanel;
        private Note selectedNote;

        private TMP_Dropdown typeSelector;
        private TMP_InputField timeInput;
        private TMP_InputField posXInput;
        private TMP_InputField posYInput;
        private TMP_InputField endTimeInput;
        private Button deleteNote;
        private Button closeNotePanel;

        // UI under Play Controller
        private Button playSwitchButton;
        [SerializeField] private Sprite[] playAndPause;
        private Button stepBackwardButton;
        private Button stepForwardButton;
        private Button backToBeginningButton;
        private Button adjustPointerButton;
        private TMP_Text beatInfo;

        void Start()
        {
            Instance = this;
            vel = new float[2];
            beatTick = new int[] {1, 0};

            
            gridPanel = this.gameObject.transform.Find("GridPanel").gameObject;
            gridPanel.SetActive(false);

            // UI under Top Bar
            audioTime = this.gameObject.transform.Find("TopBar").Find("AudioTime").Find("AudioTimeText").GetComponent<TMP_Text>();
            newProject = this.gameObject.transform.Find("TopBar").Find("NewProject").GetComponent<Button>();
            openProject = this.gameObject.transform.Find("TopBar").Find("OpenProject").GetComponent<Button>();
            saveProject = this.gameObject.transform.Find("TopBar").Find("SaveProject").GetComponent<Button>();
            openSettings = this.gameObject.transform.Find("TopBar").Find("OpenSettings").GetComponent<Button>();
            deleteAllNotes = this.gameObject.transform.Find("TopBar").Find("DeleteAllNotes").GetComponent<Button>();

            newProject.onClick.AddListener(() =>
            {
                if (!Global.IsSaved)
                {
                    MsgBoxManager.ShowMessage(MsgType.Warning, "Project Unsaved", Localization.GetString("UnsavedChart"),
                        delegate()
                        {
                            EditorManager.SaveProject();
                        },
                        delegate()
                        {
                            Global.IsSaved = true;
                            string path = SelectFolder();
                            if (path != null)
                            {
                                Global.FolderPath = path;
                                DirectoryManager.InitDirectory();
                            }
                        });
                }
                else
                {
                    Global.IsSaved = true;
                    string path = SelectFolder();
                    if (path != null)
                    {
                        Global.FolderPath = path;
                        DirectoryManager.InitDirectory();
                    }
                }
            });
            openProject.onClick.AddListener(() =>
            {
                if (!Global.IsSaved)
                {
                    MsgBoxManager.ShowMessage(MsgType.Warning, "Project Unsaved", Localization.GetString("UnsavedChart"),
                        delegate ()
                        {
                            EditorManager.SaveProject();
                        },
                        delegate ()
                        {
                            Global.IsSaved = true;
                            string path = SelectFolder();
                            if (path != null)
                            {
                                Global.FolderPath = path;
                                DirectoryManager.ReadFolder();
                            }
                        });
                }
                else
                {
                    string path = SelectFolder();
                    if (path != null)
                    {
                        Global.FolderPath = path;
                        DirectoryManager.ReadFolder();
                    }
                }
            });
            saveProject.onClick.AddListener(EditorManager.SaveProject);
            openSettings.onClick.AddListener(ToggleSettingsPanel);
            deleteAllNotes.onClick.AddListener((() =>
            {
                if (Global.IsFileSelected)
                {
                    MsgBoxManager.ShowMessage(MsgType.Warning, "Warning",
                        Localization.GetString("DeleteAllNotesAlert"), NoteManager.ClearAllNotes);
                }
            }));

            // UI under Settings Panel
            settingsPanel = this.gameObject.transform.Find("SettingsPanel").GetComponent<CanvasGroup>();
            saveSettings = this.gameObject.transform.Find("SettingsPanel").Find("SaveSettings").GetComponent<Button>();
            cancelSettings = this.gameObject.transform.Find("SettingsPanel").Find("CancelSettings").GetComponent<Button>();

            titleInputField = this.gameObject.transform.Find("SettingsPanel").Find("TitleInput").GetComponent<TMP_InputField>();
            composerInputField = this.gameObject.transform.Find("SettingsPanel").Find("ComposerInput").GetComponent<TMP_InputField>();
            arrangerInputField = this.gameObject.transform.Find("SettingsPanel").Find("ArrangerInput").GetComponent<TMP_InputField>();
            offsetInputField = this.gameObject.transform.Find("SettingsPanel").Find("OffsetInput").GetComponent<TMP_InputField>();
            baseBpmInputField = this.gameObject.transform.Find("SettingsPanel").Find("BaseBpmInput").GetComponent<TMP_InputField>();
            ratingInputField = this.gameObject.transform.Find("SettingsPanel").Find("RatingInput").GetComponent<TMP_InputField>();

            languageSelector = this.gameObject.transform.Find("SettingsPanel").Find("LanguageSelector").GetComponent<TMP_Dropdown>();
            languageDictionary = new Dictionary<int, string>() { {0, "zh_cn"}, {1, "en"} };
            resetPlayerPrefs = this.gameObject.transform.Find("SettingsPanel").Find("ResetPlayerPrefs").GetComponent<Button>();

            saveSettings.onClick.AddListener(SaveSettings);
            cancelSettings.onClick.AddListener(() => StartCoroutine("closeSettingsPanelEnumerator"));
            settingsPanel.alpha = 0;
            settingsPanel.gameObject.SetActive(false);
            resetPlayerPrefs.onClick.AddListener(() =>
            {
                MsgBoxManager.ShowMessage(MsgType.Info, "Reset Preferences", Localization.GetString("ResetPrefsAttempt"), EditorManager.InitPlayerPrefs);
            });

            titleInputField.onValueChanged.AddListener(value => isInfoEdited = true);
            composerInputField.onValueChanged.AddListener(value => isInfoEdited = true);
            arrangerInputField.onValueChanged.AddListener(value => isInfoEdited = true);
            offsetInputField.onValueChanged.AddListener(value => isInfoEdited = true);
            baseBpmInputField.onValueChanged.AddListener(value => isInfoEdited = true);
            ratingInputField.onValueChanged.AddListener(value => isInfoEdited = true);
            baseBpmInputField.onSelect.AddListener(ModifyBaseBpmAttempt);

            // UI under Left Toolbar
            createTapButton = this.gameObject.transform.Find("LeftToolbar").Find("CreateTap").GetComponent<Button>();
            createHoldButton = this.gameObject.transform.Find("LeftToolbar").Find("CreateHold").GetComponent<Button>();
            createFlickButton = this.gameObject.transform.Find("LeftToolbar").Find("CreateFlick").GetComponent<Button>();
            showSpeedPanelButton = this.gameObject.transform.Find("LeftToolbar").Find("OpenSpeedPanel").GetComponent<Button>();
            showGridButton = this.gameObject.transform.Find("LeftToolbar").Find("ShowGrid").GetComponent<Button>();
            enableAbsorptionButton = this.gameObject.transform.Find("LeftToolbar").Find("EnableAbsorption").GetComponent<Button>();
            openStepPanelButton = this.gameObject.transform.Find("LeftToolbar").Find("OpenStepPanel")
                .GetComponent<Button>();

            createTapButton.onClick.AddListener((() => NoteManager.CreateNote(Type.Tap)));
            createHoldButton.onClick.AddListener((() => NoteManager.CreateNote(Type.Hold)));
            createFlickButton.onClick.AddListener((() => NoteManager.CreateNote(Type.Flick)));
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
            songName = this.gameObject.transform.Find("SongPanel").Find("SongName").GetComponent<TMP_Text>();
            artistName = this.gameObject.transform.Find("SongPanel").Find("ArtistName").GetComponent<TMP_Text>();
            difficultySelector = this.gameObject.transform.Find("SongPanel").Find("DifficultySelector").GetComponent<TMP_Dropdown>();

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
            closeNotePanel = this.gameObject.transform.Find("NotePanel").Find("CloseButton").GetComponent<Button>();

            notePanel.gameObject.SetActive(false);
            selectedNote = null;
            timeInput.interactable = true;
            if (PlayerPrefs.HasKey("IsModifyNoteTimeAllowed"))
            {
                if(PlayerPrefs.GetInt("IsModifyNoteTimeAllowed") == 0)
                {
                    timeInput.interactable = false;
                }
            }

            posXInput.onSelect.AddListener((value) => Global.IsEditing = true);
            posYInput.onSelect.AddListener((value) => Global.IsEditing = true);
            endTimeInput.onSelect.AddListener((value) => Global.IsEditing = true);
            closeNotePanel.onClick.AddListener(() => notePanel.gameObject.SetActive(false));

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
        }

        private void Update()
        {
            if (EditorManager.isAudioPlaying)
            {
                RefreshUI();
            }
            if (Input.GetKeyUp(KeyCode.RightArrow) && Global.IsAudioLoaded && !EditorManager.isAudioPlaying && !Global.IsDialoging && !Global.IsEditing)
            {
                StepForward();
            }
            if (Input.GetKeyUp(KeyCode.LeftArrow) && Global.IsAudioLoaded && !EditorManager.isAudioPlaying && !Global.IsDialoging && !Global.IsEditing)
            {
                StepBackward();
            }
            
            if (Input.GetMouseButtonUp(1))
            {
                Collider2D col = Physics2D.OverlapPoint(Camera.main.ScreenToWorldPoint(Input.mousePosition), LayerMask.GetMask("Note"));

                if (col != null)
                {
                    Note note = col.gameObject.GetComponent<Note>();
                    selectedNote = note;
                }

                if (selectedNote != null && col != null)
                {
                    typeSelector.value = (int)selectedNote.type;
                    timeInput.text = $"{selectedNote.time}";
                    posXInput.text = $"{selectedNote.position.x:N2}";
                    posYInput.text = $"{selectedNote.position.y:N2}";

                    if (selectedNote.type == Type.Hold)
                    {
                        endTimeInput.text = $"{selectedNote.endTime}";
                        endTimeInput.interactable = true;
                    }
                    else
                    {
                        endTimeInput.text = "";
                        endTimeInput.interactable = false;
                    }

                    Vector2 panelPos = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);

                    if (panelPos.y < -1.5)
                    {
                        panelPos += new Vector2(0, 2.8f);
                    }
                    if (panelPos.x > 6)
                    {
                        panelPos -= new Vector2(2.8f, 0);
                    }

                    if (!PlayerPrefs.HasKey("IsModifyNoteTimeAllowed"))
                    {
                        timeInput.onSelect.AddListener(UpdateTimeAttempt);
                    }
                    timeInput.onEndEdit.RemoveAllListeners();
                    timeInput.onEndEdit.AddListener(value =>
                    {
                        if (PlayerPrefs.GetInt("IsModifyNoteTimeAllowed") == 0)
                        {
                            RefreshUI();
                            return;
                        }
                        selectedNote.UpdateTime(value);
                        Global.IsEditing = false;
                    });

                    posXInput.onEndEdit.RemoveAllListeners();
                    posXInput.onEndEdit.AddListener(value =>
                    {
                        selectedNote.UpdatePosX(value);
                        Global.IsEditing = false;
                    });
                    posYInput.onEndEdit.RemoveAllListeners();
                    posYInput.onEndEdit.AddListener(value =>
                    {
                        selectedNote.UpdatePosY(value);
                        Global.IsEditing = false;
                    });
                    endTimeInput.onEndEdit.RemoveAllListeners();
                    endTimeInput.onEndEdit.AddListener(value =>
                    {
                        selectedNote.UpdateEndTime(value);
                        Global.IsEditing = false;
                    });

                    deleteNote.onClick.RemoveAllListeners();
                    deleteNote.onClick.AddListener((() => { selectedNote.DeleteSelf(); notePanel.gameObject.SetActive(false); }));

                    notePanel.position = panelPos;
                    notePanel.gameObject.SetActive(true);
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                if (Physics2D.OverlapPoint(Camera.main.ScreenToWorldPoint(Input.mousePosition)) != null && !Global.IsEditing)
                {
                    if (notePanel.gameObject.activeSelf)
                    {
                        notePanel.gameObject.SetActive(false);
                        selectedNote = null;
                    }
                }
            }
        }

        public static void RefreshUI()
        {
            Instance.audioTime.SetText(EditorManager.GetAudioPCMTime().ToString());

            if (Instance.notePanel.gameObject.activeSelf && Instance.selectedNote != null)
            {
                Instance.timeInput.text = $"{Instance.selectedNote.time}";
                Instance.posXInput.text = $"{Instance.selectedNote.position.x}";
                Instance.posYInput.text = $"{Instance.selectedNote.position.y}";
            }
        }

        private void ModifyBaseBpmAttempt(string value)
        {
            if (Global.IsModifyBaseBpmAllowed)
            {
                return;
            }
            MsgBoxManager.ShowMessage(MsgType.Warning, "Warning", Localization.GetString("ModBaseBpmAttempt"),
                delegate()
                {
                    Global.IsModifyBaseBpmAllowed = true;
                    baseBpmInputField.onSelect.RemoveAllListeners();
                    baseBpmInputField.onSelect.AddListener(value => Global.IsEditing = true);
                });
        }

        private void UpdateTimeAttempt(string value)
        {
            if (!PlayerPrefs.HasKey("IsModifyNoteTimeAllowed"))
            {
                PlayerPrefs.SetInt("IsModifyNoteTimeAllowed", 0);
                MsgBoxManager.ShowMessage(MsgType.Warning, "Warning", Localization.GetString("ModNoteTimeAttempt"),
                    delegate ()
                    {
                        // Global.IsModifyTimeAllowed = true;
                        PlayerPrefs.SetInt("IsModifyNoteTimeAllowed", 1);
                        timeInput.onSelect.RemoveAllListeners();
                        timeInput.onSelect.AddListener((value) => Global.IsEditing = true);
                    });
            }
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

            if ((beatTick[0] - 1) * 960 + beatTick[1] - ticks <= 0)
            {
                beatInfo.SetText("1: 000");
                beatTick[0] = 1;
                beatTick[1] = 0;
            }
            else
            {
                int res = (beatTick[0] - 1) * 960 + beatTick[1] - ticks;
                beatTick[0] = 1 + res / 960;
                beatTick[1] = res % 960;
                beatInfo.SetText($"{beatTick[0]}: {Convert.ToString(beatTick[1]).PadLeft(3, '0')}");
            }

            EditorManager.StepBackward();
            NoteManager.RefreshAllNotes();
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

            int res;
            if ((beatTick[0] - 1) * 960 + beatTick[1] + ticks >= EditorManager.GetAudioPCMLength())
            {
                res = EditorManager.GetAudioPCMLength();
            }
            else
            {
                res = (beatTick[0] - 1) * 960 + beatTick[1] + ticks;
            }
            beatTick[0] = 1 + res / 960;
            beatTick[1] = res % 960;
            beatInfo.SetText($"{beatTick[0]}: {Convert.ToString(beatTick[1]).PadLeft(3, '0')}");

            EditorManager.StepForward();
            NoteManager.RefreshAllNotes();
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
                Global.IsAbsorption = false;
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
                Global.IsAbsorption = true;
            }
            else
            {
                enableAbsorptionButton.gameObject.GetComponent<Image>().color = Color.white;
                Global.IsAbsorption = false;
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

        private void ToggleSettingsPanel()
        {
            if (!Global.IsDirectorySelected)
            {
                return;
            }
            if (settingsPanel.gameObject.activeSelf)
            {
                StopCoroutine("openSettingsPanelEnumerator");
                StartCoroutine("closeSettingsPanelEnumerator");
                Global.IsDialoging = false;
            }
            else
            {
                Info info = EditorManager.GetInfo();
                DifficultyInfo diffInfo = EditorManager.GetDifficultyInfo();

                titleInputField.text = info.title;
                composerInputField.text = info.composer;
                arrangerInputField.text = diffInfo.arranger;
                offsetInputField.text = EditorManager.Instance.offset.ToString();
                baseBpmInputField.text = NoteManager.Instance.BaseSpeed.targetBpm.ToString();
                ratingInputField.text = diffInfo.rating;

                settingsPanel.gameObject.SetActive(true);
                StopCoroutine("closeSettingsPanelEnumerator");
                StartCoroutine("openSettingsPanelEnumerator");
                Global.IsDialoging = true;
            }
        }

        IEnumerator openSettingsPanelEnumerator()
        {
            settingsPanel.alpha = Mathf.Lerp(settingsPanel.alpha, 1f, 0.2f);

            if (settingsPanel.alpha > 0.98f)
            {
                settingsPanel.alpha = 1;
                yield break;
            }

            yield return new WaitForFixedUpdate();
            StartCoroutine("openSettingsPanelEnumerator");
        }

        IEnumerator closeSettingsPanelEnumerator()
        {
            settingsPanel.alpha = Mathf.Lerp(settingsPanel.alpha, 0f, 0.2f);

            if (settingsPanel.alpha < 0.02f)
            {
                settingsPanel.alpha = 0;
                settingsPanel.gameObject.SetActive(false);
                yield break;
            }

            yield return new WaitForFixedUpdate();
            StartCoroutine("closeSettingsPanelEnumerator");
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

        private void SelectDifficultyAttempt(int diff)
        {
            if (!Global.IsSaved)
            {
                MsgBoxManager.ShowMessage(MsgType.Warning, "Warning", Localization.GetString("UnsavedChart"),
                    delegate()
                    {
                        EditorManager.SaveProject();
                        EditorManager.SwitchDifficulty(diff);
                        difficultySelector.options[diff].text = Enum.GetName(typeof(Chart.Difficulties), diff);
                        difficultySelector.gameObject.transform.Find("Label").GetComponent<TMP_Text>().text = Enum.GetName(typeof(Chart.Difficulties), diff);
                    });
            }
            else
            {
                EditorManager.SwitchDifficulty(diff);
                difficultySelector.options[diff].text = Enum.GetName(typeof(Chart.Difficulties), diff);
                difficultySelector.gameObject.transform.Find("Label").GetComponent<TMP_Text>().text = Enum.GetName(typeof(Chart.Difficulties), diff);
            }
        }

        public void InitDifficultySelector()
        {
            difficultySelector.ClearOptions();
            bool[] diffBools = new bool[4];

            foreach (var diff in EditorManager.GetInfo().difficulties)
            {
                diffBools[diff.diffIndex] = true;
            }

            for (int i = 0; i < 4; i++)
            {
                TMP_Dropdown.OptionData op = new TMP_Dropdown.OptionData();
                
                if (diffBools[i])
                {
                    op.text = Enum.GetName(typeof(Chart.Difficulties), i);
                    difficultySelector.options.Add(op);
                    if (!Global.IsFileSelected)
                    {
                        difficultySelector.GetComponentInChildren<TMP_Text>().text = Enum.GetName(typeof(Chart.Difficulties), i);
                        difficultySelector.value = i;
                        EditorManager.SwitchDifficulty(i);
                        Global.IsFileSelected = true;
                    }
                }
                else
                {
                    op.text = "Create " + Enum.GetName(typeof(Chart.Difficulties), i);
                    difficultySelector.options.Add(op);
                }
            }

            difficultySelector.onValueChanged.AddListener(SelectDifficultyAttempt);
        }

        private string SelectFolder()
        {
            string path = Schwarzer.Windows.Dialog.OpenFolderDialog("Select Folder", "/");
            return path;
        }

        public static void RefreshSpeedPanel(List<Line> lines)
        {
            string res = "";
            foreach (var line in lines)
            {
                res += $"speed({line.time},{line.targetBpm},{line.endTime})" + '\n';
            }
            Instance.speedInput.text = res;
        }

        public static void InitUI()
        {
            Instance.audioTime.SetText("0");
            Instance.beatInfo.SetText("1: 000");
            Instance.currentBPMStatus.SetText(EditorManager.GetBPM().ToString());

            Instance.stepSelector.value = 2;
            Instance.RefreshStep(1);
        }

        public static void InitAlbumCover(Sprite sprite)
        {
            Instance.albumCover.sprite = sprite;
        }

        public static void InitSongInfo()
        {
            Instance.songName.text = EditorManager.GetInfo().title == null ? "Sample Song" : EditorManager.GetInfo().title;
            Instance.artistName.text = EditorManager.GetInfo().composer == null ? "Artist: Sample Artist" : $"Artist: {EditorManager.GetInfo().composer}";
            Instance.difficultySelector.interactable = true;
        }

        private void SaveSettings()
        {
            if (isInfoEdited)
            {
                EditorManager.UpdateInfo(titleInputField.text, composerInputField.text, arrangerInputField.text, ratingInputField.text);
                EditorManager.Instance.offset = Int32.Parse(offsetInputField.text);
                if (Global.IsModifyBaseBpmAllowed)
                {
                    EditorManager.Instance.InitializeBPM(Single.Parse(baseBpmInputField.text));
                    NoteManager.Instance.BaseSpeed = new Line(Single.Parse(baseBpmInputField.text));
                }

                Global.IsSaved = false;
            }

            PlayerPrefs.SetString("Language", languageDictionary[languageSelector.value]);
        }
    }
}