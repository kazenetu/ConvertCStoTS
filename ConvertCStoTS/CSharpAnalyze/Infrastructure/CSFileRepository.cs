using CSharpAnalyze.Domain.Model.File;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CSharpAnalyze.Infrastructure
{
  /// <summary>
  /// C#ファイルデータ読み込み
  /// </summary>
  public class CSFileRepository: ICSFileRepository
  {
    /// <summary>
    /// C#ファイルデータリストを取得する
    /// </summary>
    /// <param name="rootPath">ルートパス</param>
    /// <param name="exclusionKeywords">除外フォルダリスト</param>
    /// <returns>C#ファイルデータのリスト</returns>
    public List<(string relativePath,string source)> GetCSFileList(string rootPath, List<string> exclusionKeywords)
    {
      var result = new List<(string relativePath, string source)>();

      // ディレクトリ内のファイルリストを作成
      var csFilePaths = Directory.GetFiles(rootPath, "*.cs", SearchOption.AllDirectories);

      // 対象ファイルリストを作成
      var targetFilePaths = csFilePaths.Where(filePath => !exclusionKeywords.Any(keyword => filePath.Contains(keyword))).ToList();

      // ファイル単位でソース解析
      foreach (var filePath in targetFilePaths)
      {
        // 相対パスを作成
        var relativePath = Path.GetRelativePath(rootPath, filePath);
        relativePath = relativePath.Replace(Path.DirectorySeparatorChar, '/');

        // C＃ファイル読み込み
        using (var sr = new StreamReader(filePath))
        {
          result.Add((relativePath, sr.ReadToEnd()));
        }
      }

      return result;
    }
  }
}
