using System;
using System.Collections.Generic;
using System.Text;

namespace CSharpAnalyze.DomainModel
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
  }
}
