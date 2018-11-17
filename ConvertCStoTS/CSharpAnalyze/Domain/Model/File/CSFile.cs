using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CSharpAnalyze.Domain.Model.File
{
  public class CSFile
  {
    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="relativePath">相対パス</param>
    /// <param name="sourceCode">ソースコード</param>
    public CSFile(string relativePath, string sourceCode)
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
    /// C#ファイルリストを取得
    /// </summary>
    /// <param name="rootPath">ルートパス</param>
    /// <returns></returns>
    /// <returns>C#ファイルデータのリスト</returns>
    public static List<CSFile> GetCSFileList(string rootPath, ICSFileRepository fileRepository)
    {
      // 除外フォルダ
      var exclusionKeywords = new List<string>() {
        $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"
      };

      return fileRepository.GetCSFileList(rootPath, exclusionKeywords).
              Select(fileData => new CSFile(fileData.relativePath, fileData.source)).
              ToList();
    }

    #endregion
  }
}
