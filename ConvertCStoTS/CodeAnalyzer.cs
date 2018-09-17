using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using static ConvertCStoTS.MethodData;
using static ConvertCStoTS.Common.AnalyzeUtility;
using ConvertCStoTS.Common;

namespace ConvertCStoTS
{
  public class CodeAnalyzer
  {
    /// <summary>
    /// 解析結果
    /// </summary>
    public AnalyzeResult Result { get; } = new AnalyzeResult();

    /// <summary>
    /// 内部クラスの名称変更情報
    /// </summary>
    /// <remarks>「親クラス_内部クラス」で表現する</remarks>
    private Dictionary<string, string> RenameClasseNames = new Dictionary<string, string>();

    /// <summary>
    /// メソッド管理クラスインスタンス
    /// </summary>
    private MethodDataManager MethodDataManager = new MethodDataManager();

    /// <summary>
    /// メソッド出力フラグ
    /// </summary>
    private readonly bool IsOutputMethod;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="isOutputMethod">メソッド出力フラグ</param>
    public CodeAnalyzer(bool isOutputMethod = true)
    {
      IsOutputMethod = isOutputMethod;
    }

    /// <summary>
    /// 解析処理
    /// </summary>
    /// <param name="targetCode">C#ソース</param>
    /// <returns>TypeScript情報</returns>
    public AnalyzeResult Analyze(string targetCode)
    {
      // クリア
      Result.Clear();
      RenameClasseNames.Clear();
      MethodDataManager.Clear();

      // C#解析
      var tree = CSharpSyntaxTree.ParseText(targetCode) as CSharpSyntaxTree;

      // 構文エラーチェック
      foreach (var item in tree.GetDiagnostics())
      {
        return Result;
      }

      // ルート取得
      var root = tree.GetRoot();

      // クラス名取得
      foreach (CSharpSyntaxNode item in root.DescendantNodes())
      {
        if (item is ClassDeclarationSyntax cds)
        {
          if (cds.Parent is ClassDeclarationSyntax parentClass)
          {
            var renameClassName = parentClass.Identifier + "_" + cds.Identifier.ValueText;
            RenameClasseNames.Add(cds.Identifier.ValueText, renameClassName);
          }
        }
      }

      // ソース解析
      var result = new StringBuilder();
      foreach (CSharpSyntaxNode item in root.DescendantNodes())
      {
        switch (item.Kind())
        {
          case SyntaxKind.ClassDeclaration:
            result.Append(GetItemText(item as ClassDeclarationSyntax));
            break;
        }
      }

      var analyzeResult = new AnalyzeResult();
      analyzeResult.SourceCode = result.ToString();
      Result.CopyUnknownReferences(ref analyzeResult);
      analyzeResult.ClassNames.AddRange(Result.ClassNames);
      return analyzeResult;
    }

    /// <summary>
    /// クラス取得
    /// </summary>
    /// <param name="item">C#ソースを解析したインスタンス</param>
    /// <param name="index">インデックス数(半角スペース数)</param>
    /// <returns>TypeScriptのクラスに変換した文字列</returns>
    private string GetItemText(ClassDeclarationSyntax item, int index = 0)
    {
      var result = new StringBuilder();

      // TSメソッド管理クラスのクリア
      MethodDataManager.Clear();

      var className = item.Identifier.ValueText;
      var superClass = string.Empty;
      if (item.BaseList != null)
      {
        superClass = $" extends {GetTypeScriptType(item.BaseList.Types[0].Type,Result.UnknownReferences,RenameClasseNames)}";
      }

      // 親クラスがある場合はクラス名に付加する
      if (item.Parent is ClassDeclarationSyntax parentClass)
      {
        className = parentClass.Identifier + "_" + className;
      }

      // クラスコメント追加
      result.Append(GetComments(item.GetLeadingTrivia().ToString(), string.Empty));

      // クラス定義追加
      result.AppendLine($"{GetSpace(index)}export class {className}{superClass} {item.OpenBraceToken.ValueText}");

      // クラス名を追加
      if (!Result.ClassNames.Contains(className))
      {
        Result.ClassNames.Add(className);
      }

      // 子要素を設定
      foreach (var childItem in item.Members)
      {
        // インナークラスの場合は処理をしない
        if(childItem is ClassDeclarationSyntax)
        {
          continue;
        }

        try
        {
          switch (childItem)
          {
            case PropertyDeclarationSyntax prop:
              result.Append(GetPropertyText(prop, index + 2));
              break;
            case BaseMethodDeclarationSyntax method:
              var methodInstance = new AnalyzeClassMethod(Result, RenameClasseNames, IsOutputMethod);
              methodInstance.GetMethodText(method, ref MethodDataManager, index + 2);
              break;
          }
        }
        catch (Exception ex)
        {
          Console.WriteLine($"[{ex.Message}]");
          Console.WriteLine(childItem.ToString());
        }
      }

      // メソッドを出力
      result.Append(MethodDataManager.GetMethodText());

      result.AppendLine($"{GetSpace(index)}{item.CloseBraceToken.ValueText}");
      return result.ToString();
    }

    /// <summary>
    /// プロパティ取得
    /// </summary>
    /// <param name="item">C#ソースを解析したインスタンス</param>
    /// <param name="index">インデックス数(半角スペース数)</param>
    /// <returns>TypeScriptのクラスフィールドに変換した文字列</returns>
    private string GetPropertyText(PropertyDeclarationSyntax item, int index = 0)
    {
      var result = new StringBuilder();

      result.Append(GetComments(item.GetLeadingTrivia().ToString()));
      result.Append($"{GetSpace(index)}{GetModifierText(item.Modifiers)} {item.Identifier.ValueText}: {GetTypeScriptType(item.Type, Result.UnknownReferences, RenameClasseNames)}");

      // 初期化処理を追加
      result.Append(GetCreateInitializeValue(item.Type, item.Initializer, Result.UnknownReferences, RenameClasseNames));

      result.AppendLine(";");

      return result.ToString();
    }

  }
}
