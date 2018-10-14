using ConvertCStoTS.Analyze.Methods;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
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
    /// 解析前処理
    /// </summary>
    /// <param name="csInfos">C#ファイル情報リスト</param>
    /// <remarks>クラス名変換とクラスメンバの格納</remarks>
    public void PreAnalyze(List<CSFileInfo> csInfos)
    {
      // ClassObjectインスタンス取得
      var classObject = ClassObject.GetInstance();

      // モードを前処理に設定
      classObject.IsPreAnalyze = true;

      // 解析によってクラス名変更とクラスメンバの格納を行う
      foreach(var csInfo in csInfos)
      {
        Analyze(csInfo.SourceCode);
      }
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

      // 前処理の場合はクラス名変更確認と格納
      if (classObject.IsPreAnalyze)
      {
        foreach (CSharpSyntaxNode item in root.DescendantNodes())
        {
          if (item is ClassDeclarationSyntax cds)
          {
            if (cds.Parent is ClassDeclarationSyntax parentClass)
            {
              var renameClassName = parentClass.Identifier + "_" + cds.Identifier.ValueText;
              var csClassName = parentClass.Identifier + "." + cds.Identifier.ValueText;
              classObject.RenameClasseNames.Add(csClassName, renameClassName);
            }
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
      var MethodDataManager = new MethodDataManager();

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
      classObject.ProcessClassName = className;

      // インスタンス要素を格納
      foreach (var childItem in item.Members)
      {
        // インナークラスの場合は処理をしない
        if (childItem is ClassDeclarationSyntax)
        {
          continue;
        }

        SyntaxTokenList? tokenList = null;
        switch (childItem)
        {
          case FieldDeclarationSyntax fieldDeclaration:
            tokenList = fieldDeclaration.Modifiers;
            break;
          case PropertyDeclarationSyntax prop:
            tokenList = prop.Modifiers;
            break;
          case BaseMethodDeclarationSyntax method:
            if (!method.IsKind(SyntaxKind.ConstructorDeclaration))
            {
              tokenList = method.Modifiers;
            }
            break;
        }

        // インスタンス要素の場合は要素名を格納
        if (tokenList.HasValue)
        {
          var targetName = childItem.DescendantTokens().Where(token => token.IsKind(SyntaxKind.IdentifierToken)).FirstOrDefault();
          if (targetName != null && !tokenList.Value.Any(token => token.ValueText == "static"))
          {
            if (!classObject.InstanceMembers.Contains(targetName.ValueText))
            {
              classObject.InstanceMembers.Add(targetName.ValueText);
            }
          }
        }
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
            case FieldDeclarationSyntax fieldDeclaration:
              result.Append(GetFieldDeclarationText(fieldDeclaration, index + IndentSize));
              break;
            case PropertyDeclarationSyntax prop:
              result.Append(GetPropertyText(prop, index + IndentSize));
              break;
            case BaseMethodDeclarationSyntax method:
              var methodInstance = new Method();
              var methodData = methodInstance.GetMethodText(method, IsOutputMethod, index + IndentSize);
              if (methodData != null)
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

      // 前処理の場合は終了
      if (classObject.IsPreAnalyze)
      {
        return string.Empty;
      }

      // メソッドを出力
      result.Append(MethodDataManager.GetMethodText());

      result.AppendLine($"{GetSpace(index)}{item.CloseBraceToken.ValueText}");
      return result.ToString();
    }

    /// <summary>
    /// フィールド類取得
    /// </summary>
    /// <param name="item">C#ソースを解析したインスタンス</param>
    /// <param name="index">インデックス数(半角スペース数)</param>
    /// <returns>TypeScriptのクラスフィールドに変換した文字列</returns>
    private string GetFieldDeclarationText(FieldDeclarationSyntax item, int index = 0)
    {
      var classObject = ClassObject.GetInstance();
      var variable = item.Declaration.Variables[0];

      // 前処理の場合はメソッドチェックと格納のみ
      if (classObject.IsPreAnalyze)
      {
        if(item.Modifiers.Any(modifier => modifier.Kind() == SyntaxKind.StaticKeyword))
        {
          classObject.AddStaticMember(variable.Identifier.ValueText);
        }
        return string.Empty;
      }

      var result = new StringBuilder();

      //初期化処理を取得
      var defineAssignmentAssertion = string.Empty;
      var createInitializeValue = classObject.GetCreateInitializeValue(item.Declaration.Type, variable.Initializer);
      if (string.IsNullOrEmpty(createInitializeValue))
      {
        // 初期化情報がない場合は限定代入アサーションを設定
        defineAssignmentAssertion = "!";
      }

      // キーワード設定
      var modifiers = new List<string>();
      modifiers.Add(GetModifierText(item.Modifiers));
      foreach(var modifer in item.Modifiers)
      {
        switch (modifer.Text)
        {
          case "const":
            modifiers.Add("readonly");
            break;
          case "static":
            modifiers.Add("static");
            break;
        }
      }

      result.Append(GetComments(item.GetLeadingTrivia().ToString()));
      result.Append($"{GetSpace(index)}{string.Join(' ',modifiers)} {variable.Identifier.ValueText}{defineAssignmentAssertion}: {classObject.GetTypeScriptType(item.Declaration.Type)}");

      // 初期化処理を設定
      result.Append(ReplaceMethodName(createInitializeValue));

      result.AppendLine(";");

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

      // 前処理の場合はメソッドチェックと格納のみ
      if (classObject.IsPreAnalyze)
      {
        if (item.Modifiers.Any(modifier => modifier.Kind() == SyntaxKind.StaticKeyword))
        {
          classObject.AddStaticMember(item.Identifier.ValueText);
        }
        return string.Empty;
      }

      var result = new StringBuilder();

      // 初期化処理を取得
      var defineAssignmentAssertion = string.Empty;
      var createInitializeValue = classObject.GetCreateInitializeValue(item.Type, item.Initializer);
      if (string.IsNullOrEmpty(createInitializeValue))
      {
        // 初期化情報がない場合は限定代入アサーションを設定
        defineAssignmentAssertion = "!";
      }

      result.Append(GetComments(item.GetLeadingTrivia().ToString()));
      result.Append($"{GetSpace(index)}{GetModifierText(item.Modifiers)} {item.Identifier.ValueText}{defineAssignmentAssertion}: {classObject.GetTypeScriptType(item.Type)}");

      // 初期化処理を設定
      result.Append(ReplaceMethodName(createInitializeValue));

      result.AppendLine(";");

      return result.ToString();
    }

  }
}
