using CSharpAnalyze.Repositories;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CSharpAnalyze
{
  /// <summary>
  /// C#解析クラス
  /// </summary>
  public class Analyze
  {
    #region コンストラクタ
    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="rootPath">ルートパス</param>
    public Analyze(string rootPath)
    {
      // セマンティックモデルのリストを作成する
      if (!CreateModels(rootPath))
      {
        // TODO エラー
      }

      // TODO 解析処理
    }
    #endregion

    #region インスタンスフィールド
    /// <summary>
    /// セマンティックモデルリスト
    /// </summary>
    private List<SemanticModel> Models = new List<SemanticModel>();
    #endregion

    /// <summary>
    /// セマンティックモデルを作成する
    /// </summary>
    /// <param name="rootPath">ルートパス</param>
    /// <returns>作成結果</returns>
    private bool CreateModels(string rootPath)
    {
      // C#ファイル情報リストを取得
      var csFileInfos = CSFileInfo.GetCSFileInfoList(rootPath);

      // 編集クラスを作成
      var mscorlib = MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location);
      var treeList = csFileInfos.Select(info => CSharpSyntaxTree.ParseText(info.SourceCode)).ToList();
      var compilation = CSharpCompilation.Create("", treeList, new[] { mscorlib });

      // セマンティックモデルのリストを取得する
      foreach (var model in treeList)
      {
        Models.Add(compilation.GetSemanticModel(model));
      }

      

      return false;
    }

  }
}
