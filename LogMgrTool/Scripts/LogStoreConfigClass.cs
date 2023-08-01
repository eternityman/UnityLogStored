using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LogStoreConfigClass
{
    #region 事件区

    

    #endregion

    #region 属性区

    public int MaxMemoryK
    {
        get
        {
            if (maxMemoryK == 0)
            {
                maxMemoryK = maxMemory * 1024 * 1024;
            }

            return maxMemoryK;
        }
    }

    #endregion

    #region 变量区

    public string logDirectoryName;
    public List<LogType> ignoreLogTypeList;
    public int maxMemory;

    private HashSet<LogType> _ignoreTypeSet;
    private int maxMemoryK;

    #endregion

    #region 函数区

    //--------------------Public 公有函数--------------------//

    public LogStoreConfigClass()
    {
        ignoreLogTypeList = new();
        logDirectoryName = "log_file";
        maxMemory = 10;
    }

    /// <summary>
    /// 是否是忽略类型
    /// </summary>
    /// <param name="logType"></param>
    /// <returns></returns>
    public bool IsIgnore(LogType logType)
    {
        if (_ignoreTypeSet == null)
        {
            _ignoreTypeSet = new();
            for (int i = 0; i < ignoreLogTypeList.Count; i++)
            {
                _ignoreTypeSet.Add(ignoreLogTypeList[i]);
            }
        }

        return _ignoreTypeSet.Contains(logType);
    }

    //--------------------Protect 保护函数--------------------//


    //--------------------Private 私有函数--------------------//


    #endregion
}
