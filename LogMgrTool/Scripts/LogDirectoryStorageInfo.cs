public struct LogDirectoryStorageInfo
{
    public string DirFullName => _dirFullName;

    public long StorageLength => _storageLength;
    
    private string _dirFullName;
    private long _storageLength;

    public LogDirectoryStorageInfo(string dirFullName,long storageLength)
    {
        this._dirFullName = dirFullName;
        this._storageLength = storageLength;
    }
}
