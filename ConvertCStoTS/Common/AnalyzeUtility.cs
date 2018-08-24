using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace ConvertCStoTS.Common
{
  /// <summary>
  /// 解析ユーティリティ
  /// </summary>
  public static class AnalyzeUtility
  {
    /// <summary>
    /// スコープキーワード
    /// </summary>
    private static readonly List<string> ScopeKeywords = new List<string>()
    {
      "public","private","protected"
    };

    /// <summary>
    /// C#とTypeScriptの変換リスト
    /// </summary>
    private static readonly Dictionary<string, string> ConvertMethodNames = new Dictionary<string, string>()
    {
      {".ToString(",".toString(" },
      {".Length",".length" },
      {"int.Parse(","parseInt(" }
    };

    /// <summary>
    /// 置き換え対象のコメントタグと置き換え文字
    /// </summary>
    private static Dictionary<string, string> CommentTagList = new Dictionary<string, string>()
    {
      {"<summary>{0}<\\/summary>","{0}" },
      {"<param name=\"{0}\">{0}<\\/param>","@param {0} {1}" },
      {"<remarks>{0}<\\/remarks>","@return {0}" },
    };

    /// <summary>
    /// メソッドをTypeScript用に置換え
    /// </summary>
    /// <param name="src"></param>
    /// <returns></returns>
    public static string ReplaceMethodName(string src)
    {
      var result = src;

      foreach (var convertMethodName in ConvertMethodNames.Keys)
      {
        if (result.Contains(convertMethodName))
        {
          result = result.Replace(convertMethodName, ConvertMethodNames[convertMethodName], StringComparison.CurrentCulture);
        }
      }

      return result;
    }

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

    /// <summary>
    /// スコープ取得
    /// </summary>
    /// <param name="modifiers">スコープキーワード</param>
    /// <returns>public/private/protectedのキーワード</returns>
    public static string GetModifierText(SyntaxTokenList modifiers)
    {
      var scopeKeyword = modifiers.Where(modifier => ScopeKeywords.Contains(modifier.ValueText));

      return string.Join(' ', scopeKeyword);
    }

    /// <summary>
    /// コメント取得処理
    /// </summary>
    /// <param name="src">置き換え対象</param>
    /// <returns>TypeScriptコメント</returns>
    public static string GetComments(string src)
    {
      // 文字列が存在しない場合は改行だけを返す
      if (string.IsNullOrEmpty(src.Trim()))
      {
        return Environment.NewLine;
      }

      var comments = src.Replace(Environment.NewLine, string.Empty, StringComparison.CurrentCulture);

      foreach (var commentTag in CommentTagList)
      {
        comments = GetComment(comments, commentTag.Key, commentTag.Value);
      }

      string[] delimiter = { "///" };
      var results = comments.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);
      if (results.Count() > 1)
      {
        var commentDetails = results.Where(item => !string.IsNullOrEmpty(item.Trim())).Select(item => "   * " + item.Trim()).ToList();
        comments = $"  /** {Environment.NewLine}{string.Join(Environment.NewLine, commentDetails)}{Environment.NewLine}   */";
      }
      else
      {
        comments = $"  /** {results.ToString().Trim()} */";
      }

      return Environment.NewLine + comments + Environment.NewLine;

      // コメント取得
      string GetComment(string commentsText, string regexText, string replaceText)
      {

        var maches = Regex.Matches(commentsText, $"{string.Format(CultureInfo.CurrentCulture, regexText, "(.+?)")}");
        if (maches.Count > 0)
        {
          var args = new List<string>();
          foreach (Match match in maches)
          {
            if (match.Groups.Count < 2)
            {
              break;
            }
            for (var i = 1; i < match.Groups.Count; i++)
            {
              // カンマ区切りでパラメータリスト作成
              args.Add(match.Groups[i].Value.Trim());
            }
            //commentsText = Regex.Replace(commentsText, string.Format(CultureInfo.CurrentCulture, regexText, ".*"), string.Format(CultureInfo.CurrentCulture, replaceText, args.ToArray()));
            commentsText = Regex.Replace(commentsText, match.Groups[0].Value, string.Format(CultureInfo.CurrentCulture, replaceText, args.ToArray()));
          }
        }

        return commentsText;
      }
    }

  }
}
