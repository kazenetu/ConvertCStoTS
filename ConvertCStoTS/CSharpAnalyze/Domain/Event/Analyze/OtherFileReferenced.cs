namespace CSharpAnalyze.Domain.Event.Analyze
{
  /// <summary>
  /// 外部ファイル参照イベント
  /// </summary>
  public class OtherFileReferenced : IEvent
  {
    /// <summary>
    /// 外部ファイルパス
    /// </summary>
    public string FilePath { get; }
    
    /// <summary>
    /// 参照クラス名
    /// </summary>
    public string ClassName { get; }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="filePath">外部ファイルパス</param>
    /// <param name="className">参照クラス名</param>
    public OtherFileReferenced(string filePath,string className)
    {
      FilePath = filePath;
      ClassName = className;
    }
  }
}
