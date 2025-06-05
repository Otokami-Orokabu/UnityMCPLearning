namespace UnityMCP.Editor
{
    /// <summary>
    /// 変更検知のインターフェース
    /// </summary>
    public interface IChangeDetector
    {
        /// <summary>
        /// データが変更されたかどうかを判定
        /// </summary>
        /// <returns>変更があった場合はtrue</returns>
        bool HasChanged();

        /// <summary>
        /// 現在の状態を更新済みとしてマーク
        /// </summary>
        void MarkAsUpdated();
    }
}