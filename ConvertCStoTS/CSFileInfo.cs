namespace ConvertCStoTS
{
  /// <summary>
  /// C#ファイルデータ
  /// </summary>
  public class CSFileInfo
  {
    /// <summary>
    /// 対象ファイルのディレクトリパス
    /// </summary>
    public string ImportPath { set; get; }

    /// <summary>
    /// ソースファイル
    /// </summary>
    public string SourceCode { set; get; }
  }
}
