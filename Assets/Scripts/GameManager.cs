using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    [SerializeField]
    public AudioSource song;
    private float length;
    private float step;
    public float timePointer;

    public static bool isAudioPlaying;

    private void Awake()
    {
        Instance = this;
        song = this.gameObject.GetComponent<AudioSource>();
        timePointer = 0f;

        isAudioPlaying = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && Global.IsAudioLoaded)
        {
            song.time = timePointer;
            isAudioPlaying = true;
            song.Play();
        }
        if (Input.GetKeyUp(KeyCode.Space) && Global.IsAudioLoaded)
        {
            //timePointer = song.time;
            isAudioPlaying = false;
            song.Stop();
        }
        if (Input.GetKeyUp(KeyCode.RightArrow))
        {
            timePointer = timePointer + step > song.clip.length ? song.clip.length : timePointer + step;
        }
    }
    public static void InitAudio()
    {
        Instance.song.clip = Global.song;
        Instance.length = Global.song.length;
        Instance.step = Global.song.length / Global.Chart.bpm;  // TODO: Consider BPM changeable
    }

    /// <summary>
    /// Set the song into initial status
    /// </summary>
    public static void ResetAudio()
    {
        Instance.song.time = 0;
        Instance.timePointer = 0;
    }
}
