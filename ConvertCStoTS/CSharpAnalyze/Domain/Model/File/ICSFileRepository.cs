using System.Collections.Generic;

namespace CSharpAnalyze.Domain.Model.File
{
  /// <summary>
  /// C#ファイルデータ読み込み
  /// </summary>
  public interface ICSFileRepository
  {
    /// <summary>
    /// C#ファイルデータリストを取得する
    /// </summary>
    /// <param name="rootPath">ルートパス</param>
    /// <param name="exclusionKeywords">除外フォルダリスト</param>
    /// <returns>C#ファイルデータのリスト</returns>
    List<(string relativePath, string source)> GetCSFileList(string rootPath, List<string> exclusionKeywords);
  }
}
