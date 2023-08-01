using UnityEngine;
using UnityEngine.UI;

public class MainTest : MonoBehaviour
{
    public Text savePath;
    private void Awake()
    {
        LogStoredManager.Instance.StartRecordLog();
        savePath.text = Application.persistentDataPath;
        Debug.Log("MainTest.Awake");
        Application.quitting += _Quit;
    }
    
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("MainTest.Start");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnDestroy()
    {
        Debug.Log("MainTest.OnDestroy");
        LogStoredManager.Instance.StopRecordLog();
    }

    private void OnApplicationQuit()
    {
        Debug.Log("MainTest.OnApplicationQuit");
        LogStoredManager.Instance.StopRecordLog();
    }

    public void NormalOnValueChanged(string value)
    {
        Debug.Log(value);
    }
    
    public void WarningOnValueChanged(string value)
    {
        Debug.LogWarning(value);
    }
    
    public void ErrorOnValueChanged(string value)
    {
        Debug.LogError(value);
    }

    public void _Quit()
    {
        Debug.Log("MainTest.Quit");
        LogStoredManager.Instance.StopRecordLog();
        Application.quitting -= _Quit;
    }
}
