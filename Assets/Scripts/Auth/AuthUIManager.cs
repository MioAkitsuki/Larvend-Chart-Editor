using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Auth
{
    public string username;
    public string password;

    public Auth(string username, string password)
    {
        this.username = username;
        this.password = password;
    }
}

public class AuthUIManager : MonoBehaviour
{
    private TMP_InputField username;
    private TMP_InputField password;
    private Button submit;
    void Start()
    {
        username = this.gameObject.transform.Find("Username").GetComponent<TMP_InputField>();
        password = this.gameObject.transform.Find("Password").GetComponent<TMP_InputField>();
        submit = this.gameObject.transform.Find("Submit").GetComponent<Button>();

        submit.onClick.AddListener(LoginAttempt);
    }

    private void LoginAttempt()
    {
        string url = "https://usr.pub/api/chart_editor";
        string json = JsonUtility.ToJson(new Auth(username.text, password.text));
        Debug.Log(json);

        StartCoroutine(RequestByJsonBodyPost(url, json));
    }

    private IEnumerator RequestByJsonBodyPost(string url, string json)
    {
        UnityWebRequest www = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
        DownloadHandler downloadHandler = new DownloadHandlerBuffer();
        www.downloadHandler = downloadHandler;
        www.SetRequestHeader("Content-Type", "application/json;charset=utf-8");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        yield return www.SendWebRequest();

        if (www.downloadHandler.text == "true")
        {
            SceneManager.LoadScene("MainScene");
        }
    }
}
