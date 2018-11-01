using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CSharpAnalyze.Repositories
{
  /// <summary>
  /// C#ファイルデータ
  /// </summary>
  public class CSFileInfo
  {
    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="relativePath">相対パス</param>
    /// <param name="sourceCode">ソースコード</param>
    public CSFileInfo(string relativePath,string sourceCode)
    {
      RelativePath = relativePath;
      SourceCode = sourceCode;
    }

    #region インスタンスプロパティ

    /// <summary>
    /// 対象ファイルの相対ディレクトリパス
    /// </summary>
    public string RelativePath { get; }

    /// <summary>
    /// ソースファイル
    /// </summary>
    public string SourceCode { get; }

    #endregion

    #region クラスメソッド

    /// <summary>
    /// C#ファイルデータリストを取得する
    /// </summary>
    /// <param name="srcPath">ルートパス</param>
    /// <returns>C#ファイルデータのリスト</returns>
    public static List<CSFileInfo> GetCSFileInfoList(string srcPath)
    {
      // 除外フォルダ
      var exclusionKeywords = new List<string>() {
        $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"
      };

      var result = new List<CSFileInfo>();

      // ディレクトリ内のファイルリストを作成
      var csFilePaths = Directory.GetFiles(srcPath, "*.cs", SearchOption.AllDirectories);

      // 対象ファイルリストを作成
      var targetFilePaths = csFilePaths.Where(filePath => !exclusionKeywords.Any(keyword => filePath.Contains(keyword))).ToList();

      // ファイル単位でソース解析
      foreach (var filePath in targetFilePaths)
      {
        // 相対パスを作成
        var relativePath = Path.GetRelativePath(srcPath, filePath);
        relativePath = relativePath.Replace(Path.DirectorySeparatorChar, '/');

        // C＃ファイル読み込み
        using (var sr = new StreamReader(filePath))
        {
          result.Add(new CSFileInfo(relativePath, sr.ReadToEnd()));
        }
      }

      return result;
    }

    #endregion
  }
}
