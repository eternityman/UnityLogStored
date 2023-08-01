using System.IO;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LogStoredConfig))]
public class LogStoreConfigCustomEditor : Editor
{
    #region 事件区



    #endregion

    #region 属性区



    #endregion

    #region 变量区



    #endregion

    #region 函数区

    //--------------------Public 公有函数--------------------//

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        GUI.backgroundColor = Color.green;
        
        if (GUILayout.Button("保存日志配置数据"))
        {
            _SaveLogJsonData();
        }
        
        GUI.backgroundColor = Color.black;
    }

    //--------------------Protect 保护函数--------------------//


    //--------------------Private 私有函数--------------------//
    
    private void _SaveLogJsonData()
    {
        string[] paths = UnityEditor.AssetDatabase.FindAssets("LogStoredConfigAsset");
        if (paths.Length > 1)
        {
            Debug.LogError("有同名文件 LogStoredConfig 获取路径失败");
            return;
        }

        string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(paths[0]);
        //将字符串中得脚本名字和后缀统统去除掉
        string path = assetPath.Replace((@"/LogStoredConfigAsset.asset"), "");
        string dirPath = Path.Combine(path, "Resources");
        
        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }

        string filePath = Path.Combine(dirPath, "LogStoredConfigJsonData.json");

        LogStoredConfig config = UnityEditor.AssetDatabase.LoadAssetAtPath<LogStoredConfig>(assetPath);
        if (config == null)
        {
            Debug.LogError("配置资源错误：无配置资源！");
            return;
        }

        StreamWriter streamWriter = new StreamWriter(filePath);
        string dataJson = JsonUtility.ToJson(config);
        streamWriter.Write(dataJson);
        
        streamWriter.Flush();
        streamWriter.Close();
        streamWriter.Dispose();
        
        UnityEditor.AssetDatabase.Refresh();
        UnityEditor.AssetDatabase.SaveAssets();
    }

    #endregion
}
