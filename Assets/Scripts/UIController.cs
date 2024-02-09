using System;
using System.Collections;
using System.Collections.Generic;
using Larvend;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using QFramework;

namespace Larvend.Gameplay
{
    public delegate void Callback();

    public delegate void Callback<T>(T obj);
    public class UIController : MonoBehaviour, IController
    {
        public static UIController Instance { get; private set; }
        public float[] vel;
        public Material[] materials;

        private GameObject gridPanel;

        // UI under Top Bar
        private TMP_Text audioTime;
        private Button newProject;
        private Button openProject;
        private Button saveProject;
        private Button openSettings;
        private Button deleteAllNotes;
        private Button triangle;

        // UI under Settings Panel
        private CanvasGroup settingsPanel;
        private Button saveSettings;
        private Button cancelSettings;

        private bool isInfoEdited;
        private bool isOffsetEdited;
        private bool isBaseBpmEdited;
        private TMP_InputField titleInputField;
        private TMP_InputField composerInputField;
        private TMP_InputField arrangerInputField;
        private TMP_InputField offsetInputField;
        private TMP_InputField baseBpmInputField;
        private TMP_InputField ratingInputField;

        private TMP_Dropdown languageSelector;
        private Dictionary<int, string> languageDictionary;
        private TMP_Text keyVolumeValue;
        private Slider keyVolumeSlider;
        private Button exportCsv;
        private Button resetPlayerPrefs;

        // UI under Left Toolbar
        private Button createTapButton;
        private Button createHoldButton;
        private Button createFlickButton;
        private Button showSpeedPanelButton;
        private Button showGridButton;
        private Button enableAbsorptionButton;
        private Button openStepPanelButton;
        private Button eventTrackToggle;
        private RectTransform eventTrackPanel;
        private ScrollRect eventTrack;
        private bool isStepPanelOpen;

        // UI under SongPanel
        private RectTransform songPanel;
        private Image albumCover;
        private TMP_Text songName;
        private TMP_Text artistName;
        private TMP_Dropdown difficultySelector;

        // UI under StepPanel
        private RectTransform stepPanel;
        public TMP_InputField stepInputField;
        private string previousStep;
        private TMP_Dropdown speedSelector;
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
        private TMP_InputField scaleInput;
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
        private TMP_Text currentPos;
        private Slider progressBar;
        private TMP_Text totalLength;

        void Awake()
        {
            Instance = this;
            vel = new float[5];
            
            gridPanel = this.gameObject.transform.Find("GridPanel").gameObject;
            gridPanel.SetActive(false);

            // UI under Top Bar
            audioTime = this.gameObject.transform.Find("TopBar/AudioTime/AudioTimeText").GetComponent<TMP_Text>();
            newProject = this.gameObject.transform.Find("TopBar/NewProject").GetComponent<Button>();
            openProject = this.gameObject.transform.Find("TopBar/OpenProject").GetComponent<Button>();
            saveProject = this.gameObject.transform.Find("TopBar/SaveProject").GetComponent<Button>();
            openSettings = this.gameObject.transform.Find("TopBar/OpenSettings").GetComponent<Button>();
            deleteAllNotes = this.gameObject.transform.Find("TopBar/DeleteAllNotes").GetComponent<Button>();
            triangle = this.gameObject.transform.Find("TopBar/Triangle").GetComponent<Button>();

            newProject.onClick.AddListener(NewProjectAttempt);
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
                            NoteManager.ClearAllNotes();

                            Global.IsDirectorySelected = false;
                            Global.IsFileSelected = false;
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
                        NoteManager.ClearAllNotes();

                        Global.IsDirectorySelected = false;
                        Global.IsFileSelected = false;

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

            triangle.onClick.AddListener(() => {
                GUIUtility.systemCopyBuffer = audioTime.text;
                MsgBoxManager.ShowMessage(MsgType.Info, "Copy to Clipboard", Localization.GetString("CopyToClipboard"));
            });

            // UI under Settings Panel
            settingsPanel = transform.Find("SettingsPanel").GetComponent<CanvasGroup>();
            saveSettings = transform.Find("SettingsPanel/SaveSettings").GetComponent<Button>();
            cancelSettings = transform.Find("SettingsPanel/CancelSettings").GetComponent<Button>();

            titleInputField = transform.Find("SettingsPanel/TitleInput").GetComponent<TMP_InputField>();
            composerInputField = transform.Find("SettingsPanel/ComposerInput").GetComponent<TMP_InputField>();
            arrangerInputField = transform.Find("SettingsPanel/ArrangerInput").GetComponent<TMP_InputField>();
            offsetInputField = transform.Find("SettingsPanel/OffsetInput").GetComponent<TMP_InputField>();
            baseBpmInputField = transform.Find("SettingsPanel/BaseBpmInput").GetComponent<TMP_InputField>();
            ratingInputField = transform.Find("SettingsPanel/RatingInput").GetComponent<TMP_InputField>();

            languageSelector = transform.Find("SettingsPanel/LanguageSelector").GetComponent<TMP_Dropdown>();
            languageDictionary = new Dictionary<int, string>() { {0, "zh_cn"}, {1, "en"} };
            keyVolumeSlider = transform.Find("SettingsPanel/KeyVolumeSlider").GetComponent<Slider>();
            keyVolumeValue = transform.Find("SettingsPanel/KeyVolumeValue").GetComponent<TMP_Text>();
            keyVolumeSlider.SetValueWithoutNotify(AudioKit.Settings.SoundVolume);
            keyVolumeValue.SetText(AudioKit.Settings.SoundVolume.ToString());
            AudioKit.Settings.SoundVolume.RegisterWithInitValue(v => keyVolumeSlider.value = v);
            keyVolumeSlider.onValueChanged.AddListener(value => {
                AudioKit.Settings.SoundVolume.Value = value;
                keyVolumeValue.SetText(value.ToString("F2"));
            });
            exportCsv = transform.Find("SettingsPanel/ExportCsv").GetComponent<Button>();
            exportCsv.onClick.AddListener(() =>
            {
                if (Global.IsFileSelected)
                {
                    ChartManager.ExportCsv();
                }
            });
            resetPlayerPrefs = transform.Find("SettingsPanel/ResetPlayerPrefs").GetComponent<Button>();

            saveSettings.onClick.AddListener(SaveSettings);
            cancelSettings.onClick.AddListener(() =>
            {
                StartCoroutine("closeSettingsPanelEnumerator");
                Global.IsDialoging = false;
            });
            settingsPanel.alpha = 0;
            settingsPanel.gameObject.SetActive(false);
            resetPlayerPrefs.onClick.AddListener(() =>
            {
                MsgBoxManager.ShowMessage(MsgType.Info, "Reset Preferences", Localization.GetString("ResetPrefsAttempt"), EditorManager.InitPlayerPrefs);
            });

            titleInputField.onValueChanged.AddListener(value =>
            {
                isInfoEdited = true;
                Global.IsEditing = true;
            });
            titleInputField.onEndEdit.AddListener(value => Global.IsEditing = false);
            composerInputField.onValueChanged.AddListener(value =>
            {
                isInfoEdited = true;
                Global.IsEditing = true;
            });
            composerInputField.onEndEdit.AddListener(value => Global.IsEditing = false);
            arrangerInputField.onValueChanged.AddListener(value =>
            {
                isInfoEdited = true;
                Global.IsEditing = true;
            });
            arrangerInputField.onEndEdit.AddListener(value => Global.IsEditing = false);
            offsetInputField.onValueChanged.AddListener(value =>
            {
                isInfoEdited = true;
                isOffsetEdited = true;
                Global.IsEditing = true;
            });
            offsetInputField.onEndEdit.AddListener(value => Global.IsEditing = false);
            baseBpmInputField.onValueChanged.AddListener(value =>
            {
                isInfoEdited = true;
                isBaseBpmEdited = true;
                Global.IsEditing = true;
            });
            baseBpmInputField.onEndEdit.AddListener(value => Global.IsEditing = false);
            ratingInputField.onValueChanged.AddListener(value =>
            {
                isInfoEdited = true;
                Global.IsEditing = true;
            });
            ratingInputField.onEndEdit.AddListener(value => Global.IsEditing = false);
            baseBpmInputField.onSelect.AddListener(ModifyBaseBpmAttempt);
            baseBpmInputField.onEndEdit.AddListener(value => Global.IsEditing = false);

            // UI under Left Toolbar
            createTapButton = transform.Find("LeftToolbar/CreateTap").GetComponent<Button>();
            createHoldButton = transform.Find("LeftToolbar/CreateHold").GetComponent<Button>();
            createFlickButton = transform.Find("LeftToolbar/CreateFlick").GetComponent<Button>();
            showSpeedPanelButton = transform.Find("LeftToolbar/OpenSpeedPanel").GetComponent<Button>();
            showGridButton = transform.Find("LeftToolbar/ShowGrid").GetComponent<Button>();
            enableAbsorptionButton = transform.Find("LeftToolbar/EnableAbsorption").GetComponent<Button>();
            openStepPanelButton = transform.Find("LeftToolbar/OpenStepPanel").GetComponent<Button>();
            eventTrackToggle = transform.Find("LeftToolbar/EventTrackToggle").GetComponent<Button>();
            eventTrackPanel = transform.Find("EventEditor").GetComponent<RectTransform>();
            eventTrack = transform.Find("EventEditor/Scroll View").GetComponent<ScrollRect>();

            createTapButton.onClick.AddListener((() => NoteManager.CreateNote(Type.Tap)));
            createHoldButton.onClick.AddListener((() => NoteManager.CreateNote(Type.Hold)));
            createFlickButton.onClick.AddListener((() => NoteManager.CreateNote(Type.Flick)));
            showSpeedPanelButton.onClick.AddListener(ToggleSpeedPanel);
            showGridButton.onClick.AddListener(SwitchGridStatus);
            enableAbsorptionButton.onClick.AddListener(SwitchAbsorptionStatus);
            openStepPanelButton.onClick.AddListener(ToggleStepPanel);

            eventTrackToggle.onClick.AddListener(ToggleEventTrackPanel);
            eventTrackPanel.GetComponent<CanvasGroup>().blocksRaycasts = false;
            Global.IsEventTrackOn = false;

            eventTrack.normalizedPosition = new Vector2(0, 1);

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
            stepInputField = transform.Find("StepPanel/StepInput").GetComponent<TMP_InputField>();
            speedSelector = transform.Find("StepPanel/SpeedSelector").GetComponent<TMP_Dropdown>();
            currentStepStatus = this.gameObject.transform.Find("StepPanel").Find("CurrentStepStatus")
                .GetComponent<TMP_Text>();
            currentBPMStatus = this.gameObject.transform.Find("StepPanel").Find("CurrentBPMStatus")
                .GetComponent<TMP_Text>();

            stepInputField.onSelect.AddListener(value => {previousStep = value;});
            stepInputField.onEndEdit.AddListener(value => {
                if (Int32.TryParse(value, out int result) && !Global.IsPlaying && result is >0 and <=160)
                {
                    RefreshStep(result);
                }
                else
                {
                    stepInputField.text = previousStep;
                }
            });
            speedSelector.onValueChanged.AddListener(value => RefreshSpeed(value));
            currentStepStatus.SetText($"1 Step = 1.000 Beat(s)");
            currentBPMStatus.SetText("Chart Unloaded");

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
            scaleInput = this.gameObject.transform.Find("NotePanel").Find("ScaleInput").GetComponent<TMP_InputField>();
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
            scaleInput.onSelect.AddListener((value) => Global.IsEditing = true);
            closeNotePanel.onClick.AddListener(() =>
            {
                selectedNote.GetComponentInChildren<SpriteRenderer>().material = materials[0];
                selectedNote = null;
                notePanel.gameObject.SetActive(false);
                Global.IsDialoging = false;
                Global.IsEditing = false;
            });

            // UI under Play Controller
            playSwitchButton = this.gameObject.transform.Find("PlayController").Find("PlaySwitch").GetComponent<Button>();
            stepBackwardButton = this.gameObject.transform.Find("PlayController").Find("StepBackward").GetComponent<Button>();
            stepForwardButton = this.gameObject.transform.Find("PlayController").Find("StepForward").GetComponent<Button>();
            backToBeginningButton = this.gameObject.transform.Find("PlayController").Find("BackToBeginning").GetComponent<Button>();
            adjustPointerButton = this.gameObject.transform.Find("PlayController").Find("AdjustPointer").GetComponent<Button>();
            beatInfo = this.gameObject.transform.Find("BeatInfo").Find("CurrentBeat").GetComponent<TMP_Text>();
            currentPos = this.gameObject.transform.Find("ProgressInfo").Find("CurrentPos").GetComponent<TMP_Text>();
            progressBar = this.gameObject.transform.Find("ProgressInfo").Find("Slider").GetComponent<Slider>();
            totalLength = this.gameObject.transform.Find("ProgressInfo").Find("TotalLength").GetComponent<TMP_Text>();

            playSwitchButton.onClick.AddListener(PlaySwitch);
            stepBackwardButton.onClick.AddListener(StepBackward);
            stepForwardButton.onClick.AddListener(StepForward);
            backToBeginningButton.onClick.AddListener(ResetPointer);
            adjustPointerButton.onClick.AddListener(AdjustPointer);
            progressBar.onValueChanged.AddListener(value => SetPointer(value));
        }

        private void Update()
        {
            if ((Input.GetKeyUp(KeyCode.RightArrow) || Input.GetAxis("Mouse ScrollWheel") > 0) && Global.IsAudioLoaded && !Global.IsPlaying && !Global.IsDialoging && !Global.IsEditing)
            {
                StepForward(Mathf.RoundToInt(960f / Int32.Parse(stepInputField.text)));
            }
            if ((Input.GetKeyUp(KeyCode.LeftArrow) || Input.GetAxis("Mouse ScrollWheel") < 0) && Global.IsAudioLoaded && !Global.IsPlaying && !Global.IsDialoging && !Global.IsEditing)
            {
                StepBackward(Mathf.RoundToInt(960f / Int32.Parse(stepInputField.text)));
            }

            if (Input.GetKeyUp(KeyCode.DownArrow) && Global.IsAudioLoaded && !Global.IsPlaying && !Global.IsDialoging && !Global.IsEditing)
            {
                StepForward();
            }
            if (Input.GetKeyUp(KeyCode.UpArrow) && Global.IsAudioLoaded && !Global.IsPlaying && !Global.IsDialoging && !Global.IsEditing)
            {
                StepBackward();
            }

            if (Input.GetKeyUp(KeyCode.S) && Global.IsAudioLoaded && !Global.IsDialoging && !Global.IsEditing)
            {
                PlaySwitch();
            }
            
            if (Input.GetMouseButtonUp(1) && !Global.IsEventTrackOn)
            {
                Collider2D col = Physics2D.OverlapPoint(Camera.main.ScreenToWorldPoint(Input.mousePosition), LayerMask.GetMask("Note"));

                if (col != null && col.enabled)
                {
                    Note note = col.gameObject.GetComponent<Note>();
                    if (selectedNote)
                    {
                        selectedNote.GetComponentInChildren<SpriteRenderer>().material = materials[0];
                    }
                    selectedNote = note;
                }

                if (selectedNote != null && col != null && col.enabled)
                {
                    selectedNote.GetComponentInChildren<SpriteRenderer>().material = materials[1];
                    typeSelector.value = (int)selectedNote.type;
                    timeInput.text = $"{selectedNote.time}";
                    posXInput.text = $"{selectedNote.position.x:N2}";
                    posYInput.text = $"{selectedNote.position.y:N2}";
                    scaleInput.text = $"{selectedNote.scale:N2}";

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
                        panelPos += new Vector2(0, 3f);
                    }
                    if (panelPos.x > 6)
                    {
                        panelPos -= new Vector2(3f, 0);
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
                    scaleInput.onEndEdit.RemoveAllListeners();
                    scaleInput.onEndEdit.AddListener(value =>
                    {
                        // if (Single.Parse(value) > 1f)
                        // {
                        //     value = "1";
                        //     scaleInput.SetTextWithoutNotify("1.00");
                        // }
                        // else if (Single.Parse(value) < 0.5f)
                        // {
                        //     value = "0.5";
                        //     scaleInput.SetTextWithoutNotify("0.50");
                        // }

                        if (Single.Parse(value) != selectedNote.scale)
                        {
                            selectedNote.UpdateScale(value);
                        }
                        Global.IsEditing = false;
                    });

                    deleteNote.onClick.RemoveAllListeners();
                    deleteNote.onClick.AddListener(() => {
                        OperationTracker.Record(new Operation(OperationType.Delete, new Line(selectedNote), null));
                        selectedNote.DeleteSelf();
                        notePanel.gameObject.SetActive(false);
                        Global.IsDialoging = false;
                    });

                    notePanel.position = panelPos;
                    notePanel.gameObject.SetActive(true);
                    Global.IsDialoging = true;
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                if (Physics2D.OverlapPoint(Camera.main.ScreenToWorldPoint(Input.mousePosition)) != null && !Global.IsEditing)
                {
                    if (notePanel.gameObject.activeSelf)
                    {
                        selectedNote.GetComponentInChildren<SpriteRenderer>().material = materials[0];
                        notePanel.gameObject.SetActive(false);
                        selectedNote = null;
                        Global.IsDialoging = false;
                        Global.IsEditing = false;
                    }
                }
            }
        }

        private void LateUpdate()
        {
            if (Global.IsPlaying)
            {
                RefreshUI();
            }
            else
            {
                if (Instance.playSwitchButton.gameObject.GetComponent<Image>().sprite == playAndPause[1])
                {
                    Instance.playSwitchButton.gameObject.GetComponent<Image>().sprite = playAndPause[0];
                    Global.IsPlaying = false;
                    
                    ResetPointer();
                    NoteManager.RefreshAllNotes();
                    Global.IsPrepared = false;
                }
            }
        }

        public static void RefreshUI()
        {
            Instance.audioTime.SetText(EditorManager.GetAudioPCMTime().ToString());
            Instance.currentPos.SetText(FormatTime(EditorManager.GetTimePointer()));
            if (Global.IsPlaying)
            {
                Instance.progressBar.SetValueWithoutNotify(EditorManager.GetTimePointer() / EditorManager.GetAudioLength());
            }

            int[] currentBeatTick = GetBeatTick(EditorManager.GetAudioPCMTime() - EditorManager.Instance.offset);
            Instance.beatInfo.SetText($"{currentBeatTick[0]}: {currentBeatTick[1].ToString().PadLeft(3, '0')}");
            int currentTick = (currentBeatTick[0] - 1) * 960 + currentBeatTick[1];
            // Instance.eventTrack.normalizedPosition = new Vector2(0, 1 - currentTick / (float) EventTrack.Instance.MaxTick);

            EventTrackController.LocateGroupByTick(currentTick);
            UpdateBpmUI();
        }

        // Manually set pointer
        private void SetPointer(float value)
        {
            if (!Global.IsPlaying && Global.IsAudioLoaded)
            {
                EditorManager.SetTime(Instance.progressBar.value * EditorManager.GetAudioLength());
                Instance.currentPos.SetText(FormatTime(Instance.progressBar.value * EditorManager.GetAudioLength()));
                NoteManager.RefreshAllNotes();

                int[] currentBeatTick = GetBeatTick(EditorManager.GetAudioPCMTime() - EditorManager.Instance.offset);
                EditorManager.Instance.beatTick = currentBeatTick;
            }
        }

        public static string FormatTime(float sec)
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

            return $"{formattedTime[0]} : {formattedTime[1]}";
        }

        private void NewProjectAttempt()
        {
            if (!Global.IsSaved)
            {
                MsgBoxManager.ShowMessage(MsgType.Warning, "Project Unsaved", Localization.GetString("UnsavedChart"),
                    delegate ()
                    {
                        EditorManager.SaveProject();
                        Global.IsSaved = true;
                        EditorManager.NewProject();
                    },
                    delegate ()
                    {
                        Global.IsSaved = true;
                        EditorManager.NewProject();
                    });
            }
            else
            {
                EditorManager.NewProject();
            }
        }

        private void ModifyBaseBpmAttempt(string value)
        {
            if (!PlayerPrefs.HasKey("IsModifyBaseBpmAllowed"))
            {
                PlayerPrefs.SetInt("IsModifyBaseBpmAllowed", 0);
                MsgBoxManager.ShowMessage(MsgType.Warning, "Warning", Localization.GetString("ModBaseBpmAttempt"),
                    delegate ()
                    {
                        PlayerPrefs.SetInt("IsModifyBaseBpmAllowed", 1);
                        baseBpmInputField.onSelect.RemoveAllListeners();
                        baseBpmInputField.onSelect.AddListener((value) => Global.IsEditing = true);
                    });
            }
        }

        private void UpdateTimeAttempt(string value)
        {
            if (!PlayerPrefs.HasKey("IsModifyNoteTimeAllowed"))
            {
                PlayerPrefs.SetInt("IsModifyNoteTimeAllowed", 0);
                MsgBoxManager.ShowMessage(MsgType.Warning, "Warning", Localization.GetString("ModNoteTimeAttempt"),
                    delegate ()
                    {
                        PlayerPrefs.SetInt("IsModifyNoteTimeAllowed", 1);
                        timeInput.onSelect.RemoveAllListeners();
                        timeInput.onSelect.AddListener((value) => Global.IsEditing = true);
                    });
            }
        }

        private void RefreshStep(int value)
        {
            float step = 4f / value;
            string res = $"1 Step = {step:N3} Beat(s)";
            Instance.currentStepStatus.SetText(res);

            EditorManager.ResetAudio();
            EventTrackController.RefreshPanel();
        }

        private void RefreshStep(bool value)
        {
            float step = 4f / Int32.Parse(stepInputField.text);
            string res = $"1 Step = {step:N3} Beat(s)";
            Instance.currentStepStatus.SetText(res);

            // EventTrack.RefreshPanel();
        }

        private void RefreshSpeed(int value)
        {
            switch (value)
            {
                case 0:
                    EditorManager.song.pitch = 0.5f;
                    break;
                case 1:
                    EditorManager.song.pitch = 0.75f;
                    break;
                case 2:
                    EditorManager.song.pitch = 1f;
                    break;
                case 3:
                    EditorManager.song.pitch = 1.25f;
                    break;
                case 4:
                    EditorManager.song.pitch = 1.5f;
                    break;
            }
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

                NoteManager.Instance.NotePreparation();
                EditorManager.Play();
            }
            else
            {
                playSwitchButton.gameObject.GetComponent<Image>().sprite = playAndPause[0];

                EditorManager.Pause();

                NoteManager.RefreshAllNotes();
                Global.IsPrepared = false;

                int[] currentBeatTick = GetBeatTick(EditorManager.GetAudioPCMTime() - EditorManager.Instance.offset);
                EditorManager.Instance.beatTick = currentBeatTick;
            }
        }

        private void SetTime(int targetTick)
        {
            int targetPcm = 0;
            foreach (var item in NoteManager.Instance.PcmDict)
            {
                if (item.Key.IsIn(targetTick / 960f + 1))
                {
                    targetPcm += Mathf.RoundToInt((targetTick / 960f + 1 - item.Key.start) * item.Value);
                    break;
                }
                else
                {
                    targetPcm += Mathf.RoundToInt(item.Key.range * item.Value);
                }
            }

            EditorManager.Instance.beatTick = new int[] {targetTick / 960 + 1, targetTick % 960};
            EditorManager.SetTime(targetPcm);

            NoteManager.RefreshAllNotes();
            Instance.progressBar.SetValueWithoutNotify(EditorManager.GetTimePointer() / EditorManager.GetAudioLength());
        }

        private void SetTime(int[] targetBeatTick)
        {
            int targetPcm = 0;
            foreach (var item in NoteManager.Instance.PcmDict)
            {
                if (item.Key.IsIn(targetBeatTick[0] + targetBeatTick[1] / 960f))
                {
                    targetPcm += Mathf.RoundToInt((targetBeatTick[0] + targetBeatTick[1] / 960f - item.Key.start) * item.Value);
                    break;
                }
                else
                {
                    targetPcm += Mathf.RoundToInt(item.Key.range * item.Value);
                }
            }

            EditorManager.Instance.beatTick = targetBeatTick;
            EditorManager.SetTime(targetPcm);

            NoteManager.RefreshAllNotes();
            Instance.progressBar.SetValueWithoutNotify(EditorManager.GetTimePointer() / EditorManager.GetAudioLength());
        }

        private static int[] GetBeatTick(int pcmTime)
        {
            int[] result = new int[] { 1, 0 };
            foreach (var item in NoteManager.Instance.PcmDict)
            {
                if (pcmTime - item.Key.range * item.Value <= 0)
                {
                    float res = item.Key.start + (float) pcmTime / Mathf.RoundToInt(item.Value);

                    result[0] = Mathf.FloorToInt(res);
                    result[1] = Mathf.RoundToInt(res * 1000 % 1000 * 0.96f);
                    break;
                }
                else
                {
                    pcmTime -= Mathf.RoundToInt(item.Key.range * item.Value);
                }
            }

            if (result[1] == 960) {
                result[0] += 1;
                result[1] = 0;
            }

            return result;
        }
        
        private void StepBackward()
        {
            if (!Global.IsAudioLoaded || Global.IsPlaying || Global.IsDialoging)
            {
                return;
            }

            // int deltaTick = Mathf.RoundToInt( 4  * 960 / Int32.Parse(stepInputField.text));
            int targetTick = (EditorManager.GetBeatTick()[0] - 1) * 960 + EditorManager.GetBeatTick()[1] - 960;
            if (targetTick < 0)
            {
                return;
            }

            int[] targetBeatTick = new int[] { targetTick / 960 + 1, targetTick % 960 };
            SetTime(targetBeatTick);
        }

        private void StepBackward(int ticks)
        {
            if (!Global.IsAudioLoaded || Global.IsPlaying || Global.IsDialoging)
            {
                return;
            }

            int targetTick = (EditorManager.GetBeatTick()[0] - 1) * 960 + EditorManager.GetBeatTick()[1] - ticks;
            if (targetTick < 0)
            {
                return;
            }

            int[] targetBeatTick = new int[] { targetTick / 960 + 1, targetTick % 960 };
            SetTime(targetBeatTick);
        }

        private void StepForward()
        {
            if (!Global.IsAudioLoaded || Global.IsPlaying || Global.IsDialoging)
            {
                return;
            }

            // int deltaTick = Mathf.RoundToInt( 4  * 960 / Int32.Parse(stepInputField.text));
            int targetTick = 960 + this.GetModel<EventTrackModel>().CurrentEventGroup.Tick;
            if (targetTick > EditorManager.GetMaxTicks())
            {
                return;
            }

            int[] targetBeatTick = new int[] { targetTick / 960 + 1, targetTick % 960 };
            SetTime(targetTick);
        }

        private void StepForward(int ticks)
        {
            if (!Global.IsAudioLoaded || Global.IsPlaying || Global.IsDialoging) return;

            int targetTick = ticks + this.GetModel<EventTrackModel>().CurrentEventGroup.Tick;
            if (targetTick > EditorManager.GetMaxTicks()) return;

            int[] targetBeatTick = new int[] { targetTick / 960 + 1, targetTick % 960 };
            SetTime(targetTick);
        }

        private void AdjustPointer()
        {
            if (Global.IsAudioLoaded && !Global.IsDialoging && !Global.IsPlaying)
            {
                int[] targetBeatTick = new int[] { EditorManager.GetBeatTick()[0], 0 };
                SetTime(targetBeatTick);
            }
        }

        private void ResetPointer()
        {
            if (Global.IsAudioLoaded && !Global.IsDialoging && !Global.IsPlaying)
            {
                EditorManager.ResetAudio();
                Instance.progressBar.value = 0;
                NoteManager.RefreshAllNotes();
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

        private void ToggleEventTrackPanel()
        {
            if (Global.IsEventTrackOn)
            {
                StopCoroutine("openEventTrackEnumerator");
                StartCoroutine("closeEventTrackEnumerator");
            }
            else
            {
                StopCoroutine("closeEventTrackEnumerator");
                StartCoroutine("openEventTrackEnumerator");
            }
        }

        IEnumerator openEventTrackEnumerator()
        {
            while (!Mathf.Approximately(eventTrackPanel.localPosition.x, 610f))
            {
                float x = Mathf.SmoothDamp(eventTrackPanel.localPosition.x, 610f, ref vel[2], 0.1f, 10000f);
                Vector3 updatePos = new Vector3(x, eventTrackPanel.localPosition.y, 0);
                eventTrackPanel.localPosition = updatePos;

                yield return new WaitForFixedUpdate();
            }

            Global.IsEventTrackOn = true;
            eventTrackPanel.GetComponent<CanvasGroup>().blocksRaycasts = true;
        }

        IEnumerator closeEventTrackEnumerator()
        {
            while (!Mathf.Approximately(eventTrackPanel.localPosition.x, 1210f))
            {
                float x = Mathf.SmoothDamp(eventTrackPanel.localPosition.x, 1210f, ref vel[2], 0.1f, 10000f);
                Vector3 updatePos = new Vector3(x, eventTrackPanel.localPosition.y, 0);
                eventTrackPanel.localPosition = updatePos;

                yield return new WaitForFixedUpdate();
            }

            Global.IsEventTrackOn = false;
            eventTrackPanel.GetComponent<CanvasGroup>().blocksRaycasts = false;
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

                settingsPanel.alpha = 0;
                settingsPanel.gameObject.SetActive(true);
                
                Global.IsEditing = false;
                isInfoEdited = false;
                StopCoroutine("closeSettingsPanelEnumerator");
                StartCoroutine("openSettingsPanelEnumerator");
            }
        }

        IEnumerator openSettingsPanelEnumerator()
        {
            while (settingsPanel.alpha < 0.98f)
            {
                settingsPanel.alpha = Mathf.Lerp(settingsPanel.alpha, 1f, 0.2f);
                yield return new WaitForFixedUpdate();
            }

            settingsPanel.alpha = 1;
            Global.IsDialoging = true;
        }

        IEnumerator closeSettingsPanelEnumerator()
        {
            while (settingsPanel.alpha > 0.02f)
            {
                settingsPanel.alpha = Mathf.Lerp(settingsPanel.alpha, 0f, 0.2f);
                yield return new WaitForFixedUpdate();
            }

            settingsPanel.alpha = 0;
            settingsPanel.gameObject.SetActive(false);
            Global.IsDialoging = false;
        }

        private void ToggleSpeedPanel()
        {
            if (speedPanel.gameObject.activeSelf)
            {
                StopCoroutine("openSpeedPanelEnumerator");
                StartCoroutine("closeSpeedPanelEnumerator");
            }
            else
            {
                speedPanel.gameObject.SetActive(true);
                StopCoroutine("closeSpeedPanelEnumerator");
                StartCoroutine("openSpeedPanelEnumerator");
            }
        }

        IEnumerator openSpeedPanelEnumerator()
        {
            speedPanel.alpha = Mathf.Lerp(speedPanel.alpha, 1f, 0.2f);

            if (speedPanel.alpha > 0.98f)
            {
                speedPanel.alpha = 1;
                Global.IsDialoging = true;
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
                Global.IsDialoging = false;
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
                MsgBoxManager.ShowMessage(MsgType.Warning, "Warning", Localization.GetString("UnsavedChart"), delegate() {
                        EditorManager.SaveProject();
                        EditorManager.SwitchDifficulty(diff);
                        difficultySelector.options[diff].text = Enum.GetName(typeof(Difficulties), diff);
                        difficultySelector.gameObject.transform.Find("Label").GetComponent<TMP_Text>().text = Enum.GetName(typeof(Difficulties), diff);
                    }, delegate()
                    {
                        Global.IsSaved = true;
                        EditorManager.SwitchDifficulty(diff);
                        difficultySelector.options[diff].text = Enum.GetName(typeof(Difficulties), diff);
                        difficultySelector.gameObject.transform.Find("Label").GetComponent<TMP_Text>().text = Enum.GetName(typeof(Difficulties), diff);
                    });
            }
            else
            {
                EditorManager.SwitchDifficulty(diff);
                difficultySelector.options[diff].text = Enum.GetName(typeof(Difficulties), diff);
                difficultySelector.gameObject.transform.Find("Label").GetComponent<TMP_Text>().text = Enum.GetName(typeof(Difficulties), diff);
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
                    op.text = Enum.GetName(typeof(Difficulties), i);
                    difficultySelector.options.Add(op);
                    if (!Global.IsFileSelected)
                    {
                        difficultySelector.GetComponentInChildren<TMP_Text>().text = Enum.GetName(typeof(Difficulties), i);
                        difficultySelector.value = i;
                        EditorManager.SwitchDifficulty(i);
                        Global.IsFileSelected = true;
                    }
                }
                else
                {
                    op.text = "Create " + Enum.GetName(typeof(Difficulties), i);
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

        public static void InitAudioState()
        {
            Instance.audioTime.SetText(Global.IsAudioLoaded ? EditorManager.Instance.offset.ToString() : "0");
            Instance.beatInfo.SetText(Global.IsAudioLoaded ? "1: 000" : "Audio Unloaded");
            Instance.currentPos.SetText("00 : 00");
            Instance.totalLength.SetText(Global.IsAudioLoaded ? FormatTime(EditorManager.GetAudioLength()) : "00 : 00");
            Instance.progressBar.SetValueWithoutNotify(0);
        }

        public static void InitBpmState()
        {
            Instance.currentBPMStatus.SetText(EditorManager.GetBPM().ToString());

            Instance.stepInputField.text = "4";
            
            Instance.RefreshStep(true);
        }

        public static void UpdateBpmUI()
        {
            foreach (var item in NoteManager.Instance.PcmDict)
            {
                if (item.Key.IsIn(Instance.GetModel<EventTrackModel>().CurrentEventGroup.Tick / 960f))
                {
                    Instance.currentBPMStatus.SetText( $"{44100 * 60f / item.Value:N2}" );
                }
            }
            // Instance.currentBPMStatus.SetText(EditorManager.GetBPM().ToString());
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
                if (isBaseBpmEdited)
                {
                    NoteManager.UpdateBaseBpm(Single.Parse(baseBpmInputField.text));
                    EditorManager.Instance.InitializeBPM(Single.Parse(baseBpmInputField.text));
                    isBaseBpmEdited = false;
                }

                if (isOffsetEdited)
                {
                    EditorManager.Instance.offset = Int32.Parse(offsetInputField.text);
                    Instance.ResetPointer();
                    Instance.audioTime.SetText(offsetInputField.text);
                    isOffsetEdited = false;
                }
                Instance.songName.text = titleInputField.text;
                Instance.artistName.text = $"Artist: {composerInputField.text}";

                Global.IsSaved = false;
                Global.IsEditing = false;
            }

            PlayerPrefs.SetString("Language", languageDictionary[languageSelector.value]);
        }

        public IArchitecture GetArchitecture()
        {
            return EventTrack.Interface;
        }

        private void OnDestroy()
        {
        }
    }
}