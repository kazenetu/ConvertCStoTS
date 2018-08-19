using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ConvertCStoTS.Common
{
  /// <summary>
  /// 解析ユーティリティ
  /// </summary>
  public static class AnalyzeUtility
  {
    /// <summary>
    /// C#の型をTypeScriptの型に変換する
    /// </summary>
    /// <param name="CSSyntax">C#の型情報</param>
    /// <param name="unknownReferences">未解決の参照データ</param>
    /// <param name="renameClasseNames">内部クラスの名称変更データ</param>
    /// <returns>TypeScriptの型に変換した文字列</returns>
    public static string GetTypeScriptType(TypeSyntax CSSyntax, Dictionary<string, string> unknownReferences, Dictionary<string, string> renameClasseNames)
    {
      var result = CSSyntax.ToString();
      switch (CSSyntax)
      {
        case NullableTypeSyntax ns:
          result = $"{GetTypeScriptType(ns.ElementType,unknownReferences,renameClasseNames)} | null";
          break;
        case GenericNameSyntax gs:
          // パラメータ設定
          var argsText = gs.TypeArgumentList.Arguments.Select(arg => GetTypeScriptType(arg, unknownReferences, renameClasseNames));
          result = $"{GetGenericClass(gs.Identifier, unknownReferences)}<{string.Join(", ", argsText)}>";
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
              if (renameClasseNames.ContainsKey(result))
              {
                return renameClasseNames[result];
              }

              result = result.Replace(".", "_", StringComparison.CurrentCulture);
              if (!unknownReferences.ContainsKey(ins.ToString()))
              {
                unknownReferences.Add(ins.ToString(), null);
              }
              break;
          }
          break;
        default:
          if (renameClasseNames.ContainsKey(result))
          {
            return renameClasseNames[result];
          }

          result = result.Replace(".", "_", StringComparison.CurrentCulture);
          if (!unknownReferences.ContainsKey(result))
          {
            unknownReferences.Add(result, null);
          }
          break;
      }
      return result;
    }

    /// <summary>
    /// ジェネリッククラスの変換
    /// </summary>
    /// <param name="token">対象</param>
    /// <param name="unknownReferences">未解決の参照データ</param>
    /// <returns>変換結果</returns>
    private static string GetGenericClass(SyntaxToken token, Dictionary<string, string> unknownReferences)
    {
      switch (token.ValueText)
      {
        case "List":
          return "Array";
        case "Dictionary":
          return "Map";
        default:
          if (!unknownReferences.ContainsKey(token.ValueText))
          {
            unknownReferences.Add(token.ValueText, null);
          }
          break;
      }

      return token.ValueText;
    }

    /// <summary>
    /// 代入の右辺をTypeScriptの文字列に変換
    /// </summary>
    /// <param name="CSSyntax">C#の代入情報</param>
    /// <param name="unknownReferences">未解決の参照データ</param>
    /// <param name="renameClasseNames">内部クラスの名称変更データ</param>
    /// <returns>TypeScriptの代入文字列</returns>
    public static string GetEqualsValue(EqualsValueClauseSyntax CSSyntax, Dictionary<string, string> unknownReferences, Dictionary<string, string> renameClasseNames)
    {
      switch (CSSyntax.Value)
      {
        case ObjectCreationExpressionSyntax ocs:
          return $" = new {GetTypeScriptType(ocs.Type, unknownReferences, renameClasseNames)}()";
      }

      return CSSyntax.ToString();
    }

    /// <summary>
    /// インデックススペースを取得
    /// </summary>
    /// <param name="index">インデックス数</param>
    /// <returns>index数分の半角スペース</returns>
    public static string GetSpace(int index)
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
