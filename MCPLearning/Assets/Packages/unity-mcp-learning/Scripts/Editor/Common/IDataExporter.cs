namespace UnityMCP.Editor
{
    /// <summary>
    /// データエクスポーターのインターフェース
    /// </summary>
    public interface IDataExporter
    {
        /// <summary>
        /// 出力ファイル名
        /// </summary>
        string FileName { get; }

        /// <summary>
        /// データをエクスポート
        /// </summary>
        void Export();
    }
}