using System.Collections.Generic;

namespace ConvertCStoTS
{
  /// <summary>
  /// 解析結果
  /// </summary>
  public class AnalyzeResult
  {
    /// <summary>
    /// ソースコード
    /// </summary>
    public string SourceCode { set; get; }

    /// <summary>
    /// 未解決の参照
    /// </summary>
    public Dictionary<string, string> UnknownReferences { get; } = new Dictionary<string, string>();

    /// <summary>
    /// 対象ファイルのディレクトリパス
    /// </summary>
    public string ImportPath { set; get; }

    /// <summary>
    /// クラス名のリスト
    /// </summary>
    public List<string> ClassNames { get; } = new List<string>();

    /// <summary>
    /// データクリア
    /// </summary>
    public void Clear()
    {
      SourceCode = string.Empty;
      UnknownReferences.Clear();
      ImportPath = string.Empty;
      ClassNames.Clear();
    }

    /// <summary>
    /// 未解決の参照のコピー
    /// </summary>
    /// <param name="desc">コピー対象</param>
    public void CopyUnknownReferences(ref AnalyzeResult desc)
    {
      foreach (var reference in UnknownReferences.Keys)
      {
        if (!ClassNames.Contains(reference))
        {
          desc.UnknownReferences.Add(reference, null);
        }
      }
    }

  }
}
