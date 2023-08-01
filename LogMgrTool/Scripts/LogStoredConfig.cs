using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LogStoredConfigAsset", menuName = "Create LogStoredConfig Asset")]
[Serializable]
public class LogStoredConfig : ScriptableObject
{
    #region 事件区



    #endregion

    #region 属性区

    #endregion

    #region 变量区

    [Header("存储log的文件夹名称")]
    public string logDirectoryName = "daily_log";

    /// <summary>
    /// 忽略的输出类型
    /// </summary>
    [Header("忽略的打印类型")]
    public List<LogType> ignoreLogTypeList = new();

    /// <summary>
    /// 最大占用空间
    /// </summary>
    [Header("最大占用空间[单位m]")]
    public int maxMemory = 10;

    #endregion

    #region 函数区

    //--------------------Public 公有函数--------------------//
    

    //--------------------Protect 保护函数--------------------//


    //--------------------Private 私有函数--------------------//


    #endregion
}
