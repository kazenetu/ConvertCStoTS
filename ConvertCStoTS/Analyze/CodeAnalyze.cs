using ConvertCStoTS.Analyze.Methods;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Text;
using static ConvertCStoTS.Common.AnalyzeUtility;

namespace ConvertCStoTS.Analyze
{
  /// <summary>
  /// C#解析・TypeScript変換クラス
  /// </summary>
  public class CodeAnalyze
  {
    /// <summary>
    /// メソッド出力フラグ
    /// </summary>
    private readonly bool IsOutputMethod;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="isOutputMethod">メソッド出力フラグ</param>
    public CodeAnalyze(bool isOutputMethod = true)
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
      var classObject = ClassObject.GetInstance();
      var analyzeResult = new AnalyzeResult();

      // クリア
      classObject.Clear();

      // C#解析
      var tree = CSharpSyntaxTree.ParseText(targetCode) as CSharpSyntaxTree;

      // 構文エラーチェック
      foreach (var item in tree.GetDiagnostics())
      {
        return new AnalyzeResult();
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
            classObject.RenameClasseNames.Add(cds.Identifier.ValueText, renameClassName);
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

      // 戻り値用クラスインスタンスを作成
      analyzeResult.SourceCode = result.ToString();
      classObject.CopyUnknownReferences(ref analyzeResult);
      analyzeResult.ClassNames.AddRange(classObject.ClassNames);
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
      var classObject = ClassObject.GetInstance();
      var result = new StringBuilder();

      // TSメソッド管理クラス生成
      var MethodDataManager = new Methods.MethodDataManager();

      var className = item.Identifier.ValueText;
      var superClass = string.Empty;
      if (item.BaseList != null)
      {
        superClass = $" extends {classObject.GetTypeScriptType(item.BaseList.Types[0].Type)}";
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
      if (!classObject.ClassNames.Contains(className))
      {
        classObject.ClassNames.Add(className);
      }

      // 子要素を設定
      foreach (var childItem in item.Members)
      {
        // インナークラスの場合は処理をしない
        if (childItem is ClassDeclarationSyntax)
        {
          continue;
        }

        try
        {
          switch (childItem)
          {
            case PropertyDeclarationSyntax prop:
              result.Append(GetPropertyText(prop, index + IndentSize));
              break;
            case BaseMethodDeclarationSyntax method:
              var methodInstance = new Method();
              var methodData = methodInstance.GetMethodText(method,IsOutputMethod, index + IndentSize);
              if(methodData != null)
              {
                MethodDataManager.AddMethodData(methodData);
              }
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
      var classObject = ClassObject.GetInstance();
      var result = new StringBuilder();

      result.Append(GetComments(item.GetLeadingTrivia().ToString()));
      result.Append($"{GetSpace(index)}{GetModifierText(item.Modifiers)} {item.Identifier.ValueText}: {classObject.GetTypeScriptType(item.Type)}");

      // 初期化処理を追加
      var createInitializeValue = classObject.GetCreateInitializeValue(item.Type, item.Initializer);
      result.Append(ReplaceMethodName(createInitializeValue));

      result.AppendLine(";");

      return result.ToString();
    }

  }
}
