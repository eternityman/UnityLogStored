using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class LogStoredManager
{
    private struct _LogFileData
    {
        public string timeToken;
        public string condition;
        public string stackTrace;
        public LogType logType;

        public _LogFileData(string timeToken,string condition,string stackTrace,LogType logType)
        {
            this.timeToken = timeToken;
            this.condition = condition;
            this.stackTrace = stackTrace;
            this.logType = logType;
        }

        public string GetLogInfo()
        {
            string resultInfo = $"[{timeToken}] [{logType.ToString()}] : {condition}\n{stackTrace}\n\n";
            return resultInfo;
        }
    }
    
    #region 事件区

    public Func<long> GetSurplusStoredStorageFunc;

    #endregion

    #region 属性区

    /// <summary>
    /// 单例实例
    /// </summary>
    public static LogStoredManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new();
            }

            return _instance;
        }
    }

    #endregion

    #region 变量区

    /// <summary>
    /// 单例实例
    /// </summary>
    private static LogStoredManager _instance;

    /// <summary>
    /// 保存路径
    /// </summary>
    private string _savePath;

    /// <summary>
    /// log线程
    /// </summary>
    private Task _logThread;

    /// <summary>
    /// 输出数据队列
    /// </summary>
    private Queue<_LogFileData> _logFileDataQue;

    /// <summary>
    /// 取消标记
    /// </summary>
    private CancellationTokenSource tokenSource;
    private CancellationToken cancellationToken;

    /// <summary>
    /// 现在的时间
    /// </summary>
    private DateTime _lastDateTime;

    /// <summary>
    /// 日志配置类
    /// </summary>
    private LogStoreConfigClass _config;

    /// <summary>
    /// 内存列表
    /// </summary>
    private Queue<LogDirectoryStorageInfo> _storedDirectoryQue;

    /// <summary>
    /// 总共使用的内存
    /// </summary>
    private long _totalUsedMemory;
    
    /// <summary>
    /// 文件流
    /// </summary>
    private FileStream _fileStream;

    #endregion

    #region 函数区

    //--------------------Public 公有函数--------------------//

    /// <summary>
    /// 开始存储log
    /// </summary>
    public void StartRecordLog()
    {
        if (_logThread != null)
        {
            return;
        }

        // _InitMemory();
        _logThread = new Task(_WaitingRecord,cancellationToken);
        _logThread.Start();
        
        Application.logMessageReceived -= _ReceiveUnityLogMessage;
        Application.logMessageReceived += _ReceiveUnityLogMessage;

    }

    /// <summary>
    /// 停止存储log
    /// </summary>
    public void StopRecordLog()
    {
        try
        {
            Application.logMessageReceived -= _ReceiveUnityLogMessage;
            tokenSource.Cancel();

            if (_fileStream != null)
            {
                _WriteAllLog();
                _fileStream.Close();
                _fileStream.Dispose();
                _fileStream = null;
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.ToString());
            if (_logThread != null)
            {
                _logThread.Dispose();
            }
        }
    }

    /// <summary>
    /// 获取对应日期的日志文件
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public byte[] GetLogFileByDate(DateTime dateTime)
    {
        string strCurDate = dateTime.ToString("yyyyMMdd", DateTimeFormatInfo.InvariantInfo);
        //
        // if (_IsSameTime(dateTime,_lastDateTime))
        // {
        //     if (_fileStream == null)
        //     {
        //         _InitWriteStream();
        //     }
        //     
        //     byte[] bytes = new byte[_fileStream.Length];
        //     _fileStream.Read(bytes, 0, bytes.Length);
        //     return bytes;
        // }
        
        string stringLogFileDir = Path.Combine(_savePath, strCurDate);
        string strDir = Path.GetFullPath(stringLogFileDir);
        if (!Directory.Exists(strDir))
        {
            return null;
        }

        string strFinalFilePath = $"{strDir}/logFile-{strCurDate}.log";
        if (!File.Exists(strFinalFilePath))
        {
            return null;
        }

        FileStream fileStream = File.OpenRead(strFinalFilePath);
        byte[] resultByteArr = new byte[fileStream.Length];
        fileStream.Read(resultByteArr, 0, resultByteArr.Length);
        return resultByteArr;
    }

    //--------------------Protect 保护函数--------------------//
    
    
    
    //--------------------Private 私有函数--------------------//

    private LogStoredManager()
    {
        TextAsset jsonData = Resources.Load<TextAsset>("LogStoredConfigJsonData");
        string jsonText = "";
        if (jsonData != null)
        {
            jsonText = jsonData.text;
            _config = JsonUtility.FromJson<LogStoreConfigClass>(jsonText);
        }

        if (_config == null)
        {
            _config = new LogStoreConfigClass();
        }

        _storedDirectoryQue = new();
        _logFileDataQue = new();
        tokenSource = new();
        cancellationToken = tokenSource.Token;
        _savePath = Path.Combine(Path.GetDirectoryName(Application.persistentDataPath) ?? "",_config.logDirectoryName);
    }
    
    /// <summary>
    /// 初始化内存
    /// </summary>
    private void _InitMemory()
    {
        _storedDirectoryQue.Clear();
        _totalUsedMemory = 0;

        if (!Directory.Exists(_savePath))
        {
            return;
        }
        
        DirectoryInfo directory = new DirectoryInfo(_savePath);
        DirectoryInfo[] directoryInfoArr = directory.GetDirectories();

        for (int i = 0; i < directoryInfoArr.Length; i++)
        {
            DirectoryInfo directoryInfo = directoryInfoArr[i];
            if (directoryInfo != null)
            {
                long storageLength = _GetDirectoryMemory(directoryInfo);
                _totalUsedMemory += storageLength;
                LogDirectoryStorageInfo storageInfo = new LogDirectoryStorageInfo(directoryInfo.FullName, storageLength);
                _storedDirectoryQue.Enqueue(storageInfo);
            }
        }
    }

    /// <summary>
    /// 初始话写入流
    /// </summary>
    private void _InitWriteStream()
    {
        try
        {
            DateTime now = DateTime.Now;
            if (!_IsSameTime(now, _lastDateTime))
            {
                _lastDateTime = now;
                string strCurDate = now.ToString("yyyyMMdd", DateTimeFormatInfo.InvariantInfo);
                string stringLogFileDir = Path.Combine(_savePath, strCurDate);
                string strDir = Path.GetFullPath(stringLogFileDir);
                if (!Directory.Exists(strDir))
                {
                    Directory.CreateDirectory(strDir);
                }

                string strFinalFilePath = $"{strDir}/logFile-{strCurDate}.log";

                _InitFileStream(strFinalFilePath);
                _InitMemory();
            }
        }
        catch (Exception ex)
        {
            _ReceiveUnityLogMessage("[LogStoredManager.Instance._InitWriteStream.catch]",ex.ToString(),LogType.Exception);
#if UNITY_EDITOR
            Debug.LogError(ex.ToString());
#endif
        }
    }

    /// <summary>
    /// 初始化文件流
    /// </summary>
    /// <param name="path"></param>
    private void _InitFileStream(string path)
    {
        if (_fileStream != null)
        {
            _fileStream.Close();
            _fileStream.Dispose();
        }
        
        _fileStream = File.Open(path, FileMode.Append);
    }
    
    /// <summary>
    /// 等待存储
    /// </summary>
    private void _WaitingRecord()
    {
        while (true)
        {
            if (tokenSource.IsCancellationRequested)
            {
                break;
            }
            
            if (_logFileDataQue.Count > 0)
            {
                _InitWriteStream();
                _LogFileData logFileData = _logFileDataQue.Dequeue();
                byte[] arr = Encoding.UTF8.GetBytes(logFileData.GetLogInfo());
                if (!_CheckStorageSurplusIsEnough(arr))
                {
                    if (_storedDirectoryQue.Count > 1)
                    {
                        if (!_DestroyHistoryFile(arr))
                        {
                            continue;
                        }
                    }
                    else
                    {
                        continue;
                    }
                }

                _WriteSomeSth(arr);
            }
        }

        _logThread = null;
    }

    /// <summary>
    /// 接受unity的log
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="stackTrace"></param>
    /// <param name="type"></param>
    private void _ReceiveUnityLogMessage(string condition, string stackTrace, LogType type)
    {
        if (_IgnoreType(type))
        {
            return;
        }
        
        string timeToken = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:ms");
        _LogFileData logFileData = new _LogFileData(timeToken, condition, stackTrace, type);
        _logFileDataQue.Enqueue(logFileData);
    }

    /// <summary>
    /// 写入所有剩余log
    /// </summary>
    private void _WriteAllLog()
    {
        int count = _logFileDataQue.Count;
        for (int i = 0; i < count; i++)
        {
            _LogFileData logFileData = _logFileDataQue.Dequeue();
            byte[] arr = Encoding.UTF8.GetBytes(logFileData.GetLogInfo());
            
            if (!_CheckStorageSurplusIsEnough(arr))
            {
                if (_storedDirectoryQue.Count > 1)
                {
                    if (!_DestroyHistoryFile(arr))
                    {
                        return;
                    }
                }
                else
                {
                    return;
                }
            }

            _WriteSomeSth(arr);
        }
    }

    private void _WriteSomeSth(byte[] bytes)
    {
        if (bytes == null)
        {
            return;
        }

        _totalUsedMemory += bytes.Length;
        try
        {
            _fileStream.Write(bytes);
        }
        catch (Exception e)
        {
            _ReceiveUnityLogMessage("[LogStoredManager.Instance._WriteSomeSth.catch]", e.ToString(),
                LogType.Exception);
#if UNITY_EDITOR
            Debug.LogError(e.ToString());
#endif
            _totalUsedMemory -= bytes.Length;
        }
        
        _fileStream.Flush();
    }

    /// <summary>
    /// 是否是同一天
    /// </summary>
    /// <param name="now"></param>
    /// <param name="histroy"></param>
    /// <returns></returns>
    private bool _IsSameTime(DateTime now, DateTime histroy)
    {
        return now.Year == histroy.Year && now.Month == histroy.Month && now.Day == histroy.Day;
    }

    /// <summary>
    /// 是否是忽略类型
    /// </summary>
    /// <param name="logType"></param>
    /// <returns></returns>
    private bool _IgnoreType(LogType logType)
    {
        if (_config == null)
        {
            return false;
        }

        return _config.IsIgnore(logType);
    }

    /// <summary>
    /// 获取文件夹占用的内存
    /// </summary>
    /// <param name="directory"></param>
    /// <returns></returns>
    private long _GetDirectoryMemory(DirectoryInfo directory)
    {
        if (directory == null)
        {
            return 0;
        }
        
        long memoryValue = 0;
        
        FileInfo[] fileInfoArr = directory.GetFiles();
        for (int i = 0; i < fileInfoArr.Length; i++)
        {
            memoryValue += fileInfoArr[i].Length;
        }
        
        DirectoryInfo[] directoryInfoArr = directory.GetDirectories();
        for (int i = 0; i < directoryInfoArr.Length; i++)
        {
            memoryValue += _GetDirectoryMemory(directoryInfoArr[i]);
        }

        return memoryValue;
    }

    /// <summary>
    /// 检测存储剩余是否足够
    /// </summary>
    /// <returns></returns>
    private bool _CheckStorageSurplusIsEnough(byte[] byteArr)
    {
        long maxMemory = GetSurplusStoredStorageFunc == null
            ? _config.MaxMemoryK
            : Math.Max(GetSurplusStoredStorageFunc.Invoke(), _config.MaxMemoryK);
        long curOccupyStorage = _totalUsedMemory + (byteArr?.Length ?? 0);
        if (curOccupyStorage >= maxMemory)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 删除历史文件
    /// </summary>
    /// <returns>返回删除结束后空间是否充足</returns>
    private bool _DestroyHistoryFile(byte[] byteArr)
    {
        int count = _storedDirectoryQue.Count - 1;
        for (int i = 0; i < count; i++)
        {
            LogDirectoryStorageInfo dirInfo = _storedDirectoryQue.Peek();

            if (Directory.Exists(dirInfo.DirFullName))
            {
                try
                {
                    Directory.Delete(dirInfo.DirFullName, true);
                    _storedDirectoryQue.Dequeue();
                }
                catch (Exception e)
                {
                    _ReceiveUnityLogMessage("[LogStoredManager.Instance._DestroyHistoryFile.catch]", e.ToString(),
                        LogType.Exception);
#if UNITY_EDITOR
                    Debug.LogError(e.ToString());
#endif
                }
            }

            _totalUsedMemory -= dirInfo.StorageLength;
            if (_CheckStorageSurplusIsEnough(byteArr))
            {
                return true;
            }
        }

        return false;
    }
    
    #endregion
}
