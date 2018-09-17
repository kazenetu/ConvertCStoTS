using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
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
      {@"\.ToString\(",".toString(" },
      {@"\.Length",".length" },
      {@"int\.Parse\(","parseInt(" },
      {@"[s|S]tring\.Empty","''" },
    };

    /// <summary>
    /// 置き換え対象のコメントタグと置き換え文字
    /// </summary>
    private static Dictionary<string, string> CommentTagList = new Dictionary<string, string>()
    {
      {"<summary>{0}<\\/summary>","{0}" },
      {"<param name=\"{0}\">{0}<\\/param>","@param {0} {1}" },
      {"<returns>{0}</returns>","@return {0}" },
    };

    /// <summary>
    /// メソッドをTypeScript用に置換え
    /// </summary>
    /// <param name="src">ソース文字列</param>
    /// <returns>置き換え後文字列</returns>
    public static string ReplaceMethodName(string src)
    {
      var result = src;

      foreach (var convertMethodName in ConvertMethodNames.Keys)
      {
        if (Regex.IsMatch(result, convertMethodName))
        {
          result = ReplaceKeyword(result, convertMethodName, ConvertMethodNames[convertMethodName]);
        }
      }

      return result;
    }

    /// <summary>
    /// メソッドの置き換え
    /// </summary>
    /// <param name="srcText">ソース文字列</param>
    /// <param name="regexText">正規表現</param>
    /// <param name="replaceText">置き換え後の対象</param>
    /// <returns>置き換え後文字列</returns>
    private static string ReplaceKeyword(string srcText, string regexText, string replaceText)
    {
      var result = srcText;

      var replaceCount = Regex.Matches(replaceText, @"\{[0-9]}").Count;
      if (replaceCount <= 0)
      {
        // 置換文字列にパラメータが設定されていない場合は単純な置換え
        result = Regex.Replace(srcText, regexText, replaceText);
      }
      else
      {
        // パラメータの取得
        var args = new List<string>();
        foreach (Match match in Regex.Matches(srcText, $"^{regexText}\\((.+?)\\)$"))
        {
          if (match.Groups.Count < 2)
          {
            break;
          }
          // カンマ区切りでパラメータリスト作成
          args.AddRange(Regex.Split(match.Groups[1].Value, @"\s*,\s*(?=(?:[^""]*""[^""]*"")*[^""]*$)").Select(arg => arg.Trim()).ToList());
        }

        // 置換文字列のパラメータ数より作成したパラメータ数が少ない場合は元の文字列を返す
        if (replaceCount > args.Count)
        {
          return srcText;
        }

        try
        {
          // 置換を実施
          result = Regex.Replace(srcText, $"{regexText}\\(.*\\)$", string.Format(CultureInfo.InvariantCulture, replaceText, args.ToArray()));
        }
        catch
        {
          throw;
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
      if (!unknownReferences.ContainsKey(token.ValueText))
      {
        unknownReferences.Add(token.ValueText, null);
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
          return $"= new {GetTypeScriptType(ocs.Type, unknownReferences, renameClasseNames)}()";
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
    public static string GetComments(string src, string indentSpace = "  ")
    {
      // 文字列が存在しない場合は改行だけを返す
      if (string.IsNullOrEmpty(src.Trim()))
      {
        return Environment.NewLine;
      }

      // 改行を削除
      var comments = src.Replace(Environment.NewLine, string.Empty, StringComparison.CurrentCulture);

      // コメントタグをTypeScript用コメントに変換
      foreach (var commentTag in CommentTagList)
      {
        comments = ConvertComment(comments, commentTag.Key, commentTag.Value);
      }

      // C#用ヘッダコメントキーワード(///)で文字列配列を作成
      string[] delimiter = { "///" };
      var results = comments.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);

      // TypeScript用ヘッダコメント用に生成( * コメント)
      var commentDetails = results.Where(item => !string.IsNullOrEmpty(item.Trim())).Select(item => $"{indentSpace} * " + item.Trim()).ToList();

      var result = new StringBuilder();
      result.AppendLine();
      result.AppendLine($"{indentSpace}/**");
      result.AppendLine($"{string.Join(Environment.NewLine, commentDetails)}");
      result.AppendLine($"{indentSpace} */");

      // 適度に改行を追加した文字列を返す
      return result.ToString();

      // コメントタグをTypeScript用コメントに変換
      string ConvertComment(string commentsText, string regexText, string replaceText)
      {
        var maches = Regex.Matches(commentsText, $"{string.Format(CultureInfo.CurrentCulture, regexText, "(.+?)")}");
        if (maches.Count > 0)
        {
          foreach (Match match in maches)
          {
            var args = new List<string>();
            if (match.Groups.Count < 2)
            {
              break;
            }
            for (var i = 1; i < match.Groups.Count; i++)
            {
              // カンマ区切りでパラメータリスト作成
              args.Add(match.Groups[i].Value.Trim());
            }

            // 置き換え
            commentsText = Regex.Replace(commentsText, match.Groups[0].Value, string.Format(CultureInfo.CurrentCulture, replaceText, args.ToArray()));
          }
        }

        return commentsText;
      }
    }

    /// <summary>
    /// フィールド宣言時の初期化を設定する
    /// </summary>
    /// <param name="type">フィールドの型</param>
    /// <param name="initializer">初期化情報</param>
    /// <param name="unknownReferences">未解決の参照データ</param>
    /// <param name="renameClasseNames">内部クラスの名称変更データ</param>
    /// <returns>TypeScriptの初期化文字列</returns>
    public static string GetCreateInitializeValue(TypeSyntax type, EqualsValueClauseSyntax initializer, Dictionary<string, string> unknownReferences, Dictionary<string, string> renameClasseNames)
    {
      if (initializer != null)
      {
        return $" {GetEqualsValue(initializer, unknownReferences, renameClasseNames)}";
      }
      else
      {
        switch (type)
        {
          case NullableTypeSyntax nts:
            return " = null";
          case GenericNameSyntax gts:
            return $" = new {GetTypeScriptType(type, unknownReferences, renameClasseNames)}()";
          case PredefinedTypeSyntax ps:
            switch (ps.ToString())
            {
              case "int":
              case "float":
              case "double":
              case "decimal":
                return " = 0";
              case "bool":
                return " = false";
            }
            break;
          case IdentifierNameSyntax ins:
            switch (ins.ToString())
            {
              case "DateTime":
                return " = new Date(0)";
            }
            break;
        }
      }
      return string.Empty;
    }

  }
}
