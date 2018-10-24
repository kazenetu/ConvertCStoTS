using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ConvertCStoTS.Analyze
{
  /// <summary>
  /// クラス用オブジェクト
  /// </summary>
  /// <remarks>
  /// シングルトンクラス
  /// </remarks>
  public class ClassObject
  {
    #region プロパティ

    /// <summary>
    /// 内部クラスの名称変更情報
    /// </summary>
    /// <remarks>「親クラス_内部クラス」で表現する</remarks>
    public Dictionary<string, string> RenameClasseNames { get; } = new Dictionary<string, string>();

    /// <summary>
    /// 未解決の参照
    /// </summary>
    public Dictionary<string, string> UnknownReferences { get; } = new Dictionary<string, string>();

    /// <summary>
    /// クラス名のリスト
    /// </summary>
    public List<string> ClassNames { get; } = new List<string>();

    /// <summary>
    /// 処理中のTypeScriptのクラス名
    /// </summary>
    public string ProcessClassName { set; get; } = string.Empty;

    /// <summary>
    /// 前処理モード
    /// </summary>
    /// <remarks>
    /// クラス名変換とクラスメンバの格納のみ実施
    /// </remarks>
    public bool IsPreAnalyze { set; get; } = true;

    /// <summary>
    /// インスタンスフィールド・プロパティ・メソッドのリスト
    /// </summary>
    public List<string> InstanceMembers { set; get; } = new List<string>();

        /// <summary>
    /// クラス単位のクラスメンバ
    /// </summary>
    public Dictionary<string, List<string>> StaticMembers = new Dictionary<string, List<string>>();

    /// <summary>
    /// メソッド出力フラグ
    /// </summary>
    public readonly bool IsOutputMethod;

    #endregion

    #region インスタンスフィールド

    /// <summary>
    /// インスタンス
    /// </summary>
    private static readonly ClassObject instance = new ClassObject();

    #endregion

    #region インスタンス取得

    /// <summary>
    /// インスタンス取得
    /// </summary>
    /// <returns>インスタンス</returns>
    public static ClassObject GetInstance()
    {
      return instance;
    }
    #endregion

    #region コンストラクタ

    /// <summary>
    /// プライベートコンストラクタ
    /// </summary>
    private ClassObject()
    {
    }
    #endregion

    #region メソッド

    /// <summary>
    /// インスタンスフィールドのクリア
    /// </summary>
    public void Clear()
    {
      InstanceMembers.Clear();
      UnknownReferences.Clear();
      ClassNames.Clear();
      ProcessClassName = string.Empty;
    }

    /// <summary>
    /// C#の型をTypeScriptの型に変換する
    /// </summary>
    /// <param name="CSSyntax">C#の型情報</param>
    /// <returns>TypeScriptの型に変換した文字列</returns>
    public string GetCreateInitializeValue(TypeSyntax type, EqualsValueClauseSyntax initializer)
    {
      if (initializer is EqualsValueClauseSyntax CSSyntax)
      {
        switch (CSSyntax.Value)
        {
          case ObjectCreationExpressionSyntax ocs:
            return $" = new {GetTypeScriptType(ocs.Type)}()";
        }

        return $" = {CSSyntax.Value}";
      }
      else
      {
        switch (type)
        {
          case NullableTypeSyntax nts:
            return " = null";
          case GenericNameSyntax gts:
            return $" = new {GetTypeScriptType(type)}()";
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

    /// <summary>
    /// C#の型をTypeScriptの型に変換する
    /// </summary>
    /// <param name="CSSyntax">C#の型情報</param>
    /// <returns>TypeScriptの型に変換した文字列</returns>
    public string GetTypeScriptType(TypeSyntax CSSyntax)
    {
      var result = CSSyntax.ToString();
      switch (CSSyntax)
      {
        case NullableTypeSyntax ns:
          result = $"{GetTypeScriptType(ns.ElementType)} | null";
          break;
        case GenericNameSyntax gs:
          // パラメータ設定
          var argsText = gs.TypeArgumentList.Arguments.Select(arg => GetTypeScriptType(arg));
          result = $"{GetGenericClass(gs.Identifier)}<{string.Join(", ", argsText)}>";
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
              // 置換えクラス名の確認
              var renameClassName = result;
              var renameClassNames = new List<string>()
              {
                result,
                $"{ProcessClassName}.{result}"
              };
              foreach(var className in renameClassNames)
              {
                if (RenameClasseNames.ContainsKey(className))
                {
                  return RenameClasseNames[className];
                }
              }

              result = result.Replace(".", "_", StringComparison.CurrentCulture);
              if (!UnknownReferences.ContainsKey(ins.ToString()))
              {
                UnknownReferences.Add(ins.ToString(), null);
              }
              break;
          }
          break;
        default:
          if (RenameClasseNames.ContainsKey(result))
          {
            return RenameClasseNames[result];
          }

          result = result.Replace(".", "_", StringComparison.CurrentCulture);
          if (!UnknownReferences.ContainsKey(result))
          {
            UnknownReferences.Add(result, null);
          }
          break;
      }
      return result;
    }

    /// <summary>
    /// クラスメンバの格納
    /// </summary>
    /// <param name="className">クラス名</param>
    /// <param name="memberName">メンバ名</param>
    public void AddStaticMember(string memberName)
    {
      // クラス名が存在しない場合は要素追加
      if (!StaticMembers.ContainsKey(ProcessClassName))
      {
        StaticMembers.Add(ProcessClassName, new List<string>());
      }

      // メンバ名が存在しない場合は追加
      if (!StaticMembers[ProcessClassName].Contains(memberName))
      {
        StaticMembers[ProcessClassName].Add(memberName);
      }
    }

    /// <summary>
    /// ジェネリッククラスの変換
    /// </summary>
    /// <param name="token">対象</param>
    /// <returns>変換結果</returns>
    private string GetGenericClass(SyntaxToken token)
    {
      if (!UnknownReferences.ContainsKey(token.ValueText))
      {
        UnknownReferences.Add(token.ValueText, null);
      }

      return token.ValueText;
    }

    #endregion

    /// <summary>
    /// 未解決の参照のコピー
    /// </summary>
    /// <param name="desc">コピー対象</param>
    public void CopyUnknownReferences(ref AnalyzeResult desc)
    {
      foreach (var reference in UnknownReferences.Keys)
      {
        if (!ClassNames.Contains(reference))
        {
          desc.UnknownReferences.Add(reference, null);
        }
      }
    }


  }
}
