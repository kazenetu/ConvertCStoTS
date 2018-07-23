using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Text;

namespace ConvertCStoTS
{
  public class CodeAnalyzer
  {
    /// <summary>
    /// 解析処理
    /// </summary>
    /// <param name="targetCode">C#ソース</param>
    /// <returns>TypeScript情報</returns>
    public string Analyze(string targetCode)
    {
      var tree = CSharpSyntaxTree.ParseText(targetCode) as CSharpSyntaxTree;

      // 構文エラーチェック
      foreach (var item in tree.GetDiagnostics())
      {
        return string.Empty;
      }

      var root = tree.GetRoot();

      // ソース変換
      var result = new StringBuilder();
      foreach (CSharpSyntaxNode item in root.DescendantNodes())
      {
        switch (item.Kind())
        {
          case SyntaxKind.ClassDeclaration:
            result.Append(GetItemText(item as ClassDeclarationSyntax));
            break;

          case SyntaxKind.NamespaceDeclaration:
            var nsItem = item as NamespaceDeclarationSyntax;
            Console.Write("namespace ");
            Console.Write(nsItem.Name.ToString());
            Console.WriteLine();
            break;
        }
      }
      return result.ToString();
    }

    /// <summary>
    /// クラス取得
    /// </summary>
    /// <param name="item">C#ソースを解析したインスタンス</param>
    /// <param name="index">インデックス数(半角スペース数)</param>
    /// <returns>TypeScriptのクラスに変換した文字列</returns>
    private string GetItemText(ClassDeclarationSyntax item,int index = 0)
    {
      var result = new StringBuilder();

      var className = item.Identifier.ValueText;
      if (item.BaseList != null)
      {
#pragma warning disable CA1307 // Specify StringComparison
        className += $" extends {item.BaseList.Types.ToString().Replace(".","_")}";
#pragma warning restore CA1307 // Specify StringComparison
      }

      // 親クラスがある場合はクラス名に付加する
      if(item.Parent is ClassDeclarationSyntax parentClass)
      {
        className = parentClass.Identifier.ValueText + "_" + className;
      }
      result.AppendLine($"{GetSpace(index)}export class {className} {item.OpenBraceToken.ValueText}");

      // 子要素を設定
      foreach(var childItem in item.Members)
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
          result = $"{gs.Identifier.ToString() }<{args.Remove(args.Length - 2, 2)}>";
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
          }
          break;
      }
      return result;
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
