namespace RemoteVisionConsole.Module
{
    public enum DataSourceType
    {
        DataEvent,
        ZeroMQ,
        DataFile
    }

    public enum ImageSaveSchema
    {
        SplitOkNg,
        OkNgInOneFolder
    }

    public enum ImageSaveFilter
    {
        ErrorOnly,
        ErrorAndNg,
        All
    }
}
