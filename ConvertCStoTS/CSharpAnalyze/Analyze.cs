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
      var models = CreateModels(rootPath);
      if (!models.Any())
      {
        // TODO エラー
      }

      // TODO 解析処理
      foreach(var model in models)
      {
        if (model.SyntaxTree.FilePath.Contains("TestLogic"))
        {
          var sma = new SemanticModelAnlayze(model);
          break;
        }
      }
    }
    #endregion

    /// <summary>
    /// セマンティックモデルを作成する
    /// </summary>
    /// <param name="rootPath">ルートパス</param>
    /// <returns>作成結果</returns>
    private List<SemanticModel> CreateModels(string rootPath)
    {
      // C#ファイル情報リストを取得
      var csFileInfos = CSFileInfo.GetCSFileInfoList(rootPath);

      // 編集クラスを作成
      var mscorlib = MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location);
      var treeList = csFileInfos.Select(info => CSharpSyntaxTree.ParseText(info.SourceCode, null, info.RelativePath)).ToList();
      var compilation = CSharpCompilation.Create("", treeList, new[] { mscorlib });

      // セマンティックモデルのリストを取得する
      var models = new List<SemanticModel>();
      foreach (var model in treeList)
      {
        models.Add(compilation.GetSemanticModel(model));
      }
      return models;
    }

  }
}
