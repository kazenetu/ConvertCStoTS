using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Text;
using System.Linq;

namespace ConvertCStoTS
{
  public class CodeAnalyzer
  {
    /// <summary>
    /// 解析結果
    /// </summary>
    public AnalyzeResult Result { get; } = new AnalyzeResult();

    /// <summary>
    /// 解析処理
    /// </summary>
    /// <param name="targetCode">C#ソース</param>
    /// <returns>TypeScript情報</returns>
    public string Analyze(string targetCode)
    {
      // クリア
      Result.Clear();

      // C#解析
      var tree = CSharpSyntaxTree.ParseText(targetCode) as CSharpSyntaxTree;

      // 構文エラーチェック
      foreach (var item in tree.GetDiagnostics())
      {
        return string.Empty;
      }

      // ルート取得
      var root = tree.GetRoot();

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

      // 暫定で出力結果に「未知の参照」を設定
      result.AppendLine("-- 未知の参照 --");
      Result.UnknownReferences.Keys.ToList().ForEach(item => result.AppendLine(item));

      return result.ToString();
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

      var className = item.Identifier.ValueText;
      var superClass = string.Empty;
      if (item.BaseList != null)
      {
        superClass = $" extends {GetTypeScriptType(item.BaseList.Types[0].Type)}";
      }

      // 親クラスがある場合はクラス名に付加する
      if (item.Parent is ClassDeclarationSyntax parentClass)
      {
        className = parentClass.Identifier + "_" + className;
      }
      result.AppendLine($"{GetSpace(index)}export class {className}{superClass} {item.OpenBraceToken.ValueText}");

      // クラス名を追加
      if (!Result.ClassNames.Contains(className))
      {
        Result.ClassNames.Add(className);
      }

      // 子要素を設定
      foreach (var childItem in item.Members)
      {
        if (childItem is PropertyDeclarationSyntax pi)
        {
          result.Append(GetItemText(pi, index + 2));
        }
      }

      result.AppendLine($"{GetSpace(index)}{item.CloseBraceToken.ValueText}");
      return result.ToString();
    }

    /// <summary>
    /// プロパティ取得
    /// </summary>
    /// <param name="item">C#ソースを解析したインスタンス</param>
    /// <param name="index">インデックス数(半角スペース数)</param>
    /// <returns>TypeScriptのクラスフィールドに変換した文字列</returns>
    private string GetItemText(PropertyDeclarationSyntax item, int index = 0)
    {
      var result = new StringBuilder();

      result.Append($"{GetSpace(index)}{item.Modifiers.ToString()} {item.Identifier.ValueText}: {GetTypeScriptType(item.Type)}");

      // 初期化処理を追加
      if (item.Initializer != null)
      {
        result.Append($" {GetEqualsValue(item.Initializer)}");
      }
      else
      {
        switch (item.Type)
        {
          case NullableTypeSyntax nts:
            result.Append(" = null");
            break;
          case GenericNameSyntax gts:
            result.Append($" = new {GetTypeScriptType(item.Type)}()");
            break;
          case PredefinedTypeSyntax ps:
            switch (ps.ToString())
            {
              case "int":
              case "float":
              case "double":
              case "decimal":
                result.Append(" = 0");
                break;
              case "bool":
                result.Append(" = false");
                break;
            }
            break;
          case IdentifierNameSyntax ins:
            switch (ins.ToString())
            {
              case "DateTime":
                result.Append(" = new Date(0)");
                break;
            }
            break;
        }
      }

      result.AppendLine(";");

      return result.ToString();
    }

    /// <summary>
    /// C#の型をTypeScriptの型に変換する
    /// </summary>
    /// <param name="CSSyntax">C#の型情報</param>
    /// <returns>TypeScriptの型に変換した文字列</returns>
    private string GetTypeScriptType(TypeSyntax CSSyntax)
    {
      var result = CSSyntax.ToString();
      switch (CSSyntax)
      {
        case NullableTypeSyntax ns:
          result = $"{GetTypeScriptType(ns.ElementType)} | null";
          break;
        case GenericNameSyntax gs:
          var arguments = new StringBuilder();
          foreach (var arg in gs.TypeArgumentList.Arguments)
          {
            arguments.Append(GetTypeScriptType(arg) + ", ");
          }
          var args = arguments.ToString();
          result = $"{GetGenericClass(gs.Identifier)}<{args.Remove(args.Length - 2, 2)}>";
          break;
        case PredefinedTypeSyntax ps:
          switch (ps.ToString())
          {
            case "int":
            case "float":
            case "double":
            case "decimal":
              result = "number";
              break;
            case "bool":
              result = "boolean";
              break;
          }
          break;
        case IdentifierNameSyntax ins:
          switch (ins.ToString())
          {
            case "DateTime":
              result = "Date";
              break;
            default:
              result = result.Replace(".", "_", StringComparison.CurrentCulture);
              if (!Result.UnknownReferences.ContainsKey(ins.ToString()))
              {
                Result.UnknownReferences.Add(ins.ToString(), null);
              }
              break;
          }
          break;
        default:
          result = result.Replace(".", "_", StringComparison.CurrentCulture);
          if (!Result.UnknownReferences.ContainsKey(result))
          {
            Result.UnknownReferences.Add(result, null);
          }
          break;
      }
      return result;
    }

    /// <summary>
    /// ジェネリッククラスの変換
    /// </summary>
    /// <param name="token">対象</param>
    /// <returns>変換結果</returns>
    private string GetGenericClass(SyntaxToken token)
    {
      switch (token.ValueText)
      {
        case "List":
          return "Array";
        case "Dictionary":
          return "Map";
        default:
          if (!Result.UnknownReferences.ContainsKey(token.ValueText))
          {
            Result.UnknownReferences.Add(token.ValueText, null);
          }
          break;
      }

      return token.ValueText;
    }

    /// <summary>
    /// 代入の右辺をTypeScriptの文字列に変換
    /// </summary>
    /// <param name="CSSyntax">C#の代入情報</param>
    /// <returns>TypeScriptのの代入文字列</returns>
    private string GetEqualsValue(EqualsValueClauseSyntax CSSyntax)
    {
      switch (CSSyntax.Value)
      {
        case ObjectCreationExpressionSyntax ocs:
          return $" = new {GetTypeScriptType(ocs.Type)}()";
      }

      return CSSyntax.ToString();
    }

    /// <summary>
    /// インデックススペースを取得
    /// </summary>
    /// <param name="index">インデックス数</param>
    /// <returns>index数分の半角スペース</returns>
    private string GetSpace(int index)
    {
      var result = string.Empty;
      var spaceCount = index;
      while (spaceCount > 0)
      {
        result += " ";
        spaceCount--;
      }

      return result;
    }

  }
}
