using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

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
    /// 解析処理
    /// </summary>
    /// <param name="targetCode">C#ソース</param>
    /// <returns>TypeScript情報</returns>
    public AnalyzeResult Analyze(string targetCode)
    {
      // クリア
      Result.Clear();
      RenameClasseNames.Clear();

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
        if(item is ClassDeclarationSyntax cds)
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
        if (childItem is ConstructorDeclarationSyntax ci)
        {
          result.Append(GetItemText(ci, index + 2));
        }
      }

      result.AppendLine($"{GetSpace(index)}{item.CloseBraceToken.ValueText}");
      return result.ToString();
    }

    /// <summary>
    /// コンストラクタメソッドの取得
    /// </summary>
    /// <param name="item">C#ソースを解析したインスタンス</param>
    /// <param name="index">インデックス数(半角スペース数)</param>
    /// <returns>TypeScriptのコンストラクタに変換した文字列</returns>
    private string GetItemText(ConstructorDeclarationSyntax item, int index = 0)
    {
      var result = new StringBuilder();

      var spaceIndex = GetSpace(index);

      // コンストラクタ宣言
      result.Append($"{spaceIndex}{item.Modifiers.ToString()} constructor(");
      result.Append(GetParameterList(item.ParameterList, true));
      result.Append(")");
      result.AppendLine(" {");

      // メソッド内処理を変換
      result.Append(GetMethodText(item.Body, index + 2));

      result.AppendLine(spaceIndex + "}");

      return result.ToString();
    }

    /// <summary>
    /// パラメータの文字列を設定
    /// </summary>
    /// <param name="list">解析結果パラメータリスト</param>
    /// <param name="settingType">型宣言するか否か</param>
    /// <returns>C#パラメータをTypeScriptに変換した文字列</returns>
    private string GetParameterList(ParameterListSyntax list, bool settingType)
    {
      var result = new StringBuilder();

      var isFirst = true;
      foreach (var param in list.Parameters)
      {
        if (!isFirst)
        {
          result.Append(", ");
        }

        result.Append($"{param.Identifier.ValueText}");
        if (settingType)
        {
          result.Append($":{GetTypeScriptType(param.Type)}");
        }

        isFirst = false;
      }

      return result.ToString();
    }

    #region メソッド内処理の取得

    /// <summary>
    /// メソッド内処理を取得
    /// </summary>
    /// <param name="logic">BlockSyntaxインスタンス</param>
    /// <param name="index">インデックス数(半角スペース数)</param>
    /// <param name="parentLocalDeclarationStatements">親GetMethodTextのローカル変数リスト</param>
    private string GetMethodText(BlockSyntax logic, int index = 0, List<string> parentLocalDeclarationStatements = null)
    {
      if (logic == null)
      {
        return string.Empty;
      }
      return GetMethodText(logic.Statements,index,parentLocalDeclarationStatements);
    }

    /// <summary>
    /// メソッド内処理を取得
    /// </summary>
    /// <param name="statements">BlockSyntaxインスタンス</param>
    /// <param name="index">インデックス数(半角スペース数)</param>
    /// <param name="parentLocalDeclarationStatements">親GetMethodTextのローカル変数リスト</param>
    /// <returns>TypeScriptに変換した文字列</returns>
    private string GetMethodText(SyntaxList<StatementSyntax> statements, int index = 0, List<string> parentLocalDeclarationStatements = null)
    {
      // 宣言されたローカル変数
      var localDeclarationStatements = new List<string>();
      if(parentLocalDeclarationStatements != null)
      {
        localDeclarationStatements.AddRange(parentLocalDeclarationStatements);
      }

      var result = new StringBuilder();

      var spaceIndex = GetSpace(index);

      foreach(var statement in statements)
      {
        if (statement is LocalDeclarationStatementSyntax lds)
        {
          if (lds.Declaration.Type.IsVar)
          {
            result.AppendLine($"{spaceIndex}let {lds.Declaration.Variables};");
            foreach (var v in lds.Declaration.Variables)
            {
              localDeclarationStatements.Add(v.Identifier.ValueText);
            }
          }
          else
          {
            var tsType = GetTypeScriptType(lds.Declaration.Type);
            foreach (var v in lds.Declaration.Variables)
            {
              result.AppendLine($"{spaceIndex}let {v.Identifier}:{tsType} {v.Initializer};");
              localDeclarationStatements.Add(v.Identifier.ValueText);
            }
          }
          continue;
        }

        if (statement is IfStatementSyntax ifss)
        {
          result.AppendLine($"{spaceIndex}if({GetExpression(ifss.Condition, localDeclarationStatements)})" + " {");
          result.Append(GetMethodText(ifss.Statement as BlockSyntax, index + 2, localDeclarationStatements));
          result.AppendLine($"{spaceIndex}" + "}");

          // ElseStatement
          if(ifss.Else != null)
          {
            result.AppendLine($"{spaceIndex}" + "else {");
            result.Append(GetMethodText(ifss.Else.Statement as BlockSyntax, index + 2));
            result.AppendLine($"{spaceIndex}" + "}");
          }
          continue;
        }
        if (statement is ExpressionStatementSyntax ess)
        {
          result.AppendLine($"{spaceIndex}{GetExpression(ess.Expression, localDeclarationStatements)};");
          continue;
        }
        if(statement is SwitchStatementSyntax sss)
        {
          result.Append(spaceIndex);
          result.AppendLine($"switch({GetExpression(sss.Expression, localDeclarationStatements)})" + " {");
          foreach(var section in sss.Sections)
          {
            foreach(var label in section.Labels)
            {
              result.AppendLine($"{GetSpace(index + 2)}{label}");
            }
            result.Append(GetMethodText(section.Statements, index + 4,localDeclarationStatements));
          }

          result.AppendLine(spaceIndex + "}");
          continue;
        }
        if (statement is BreakStatementSyntax bss)
        {
          result.AppendLine($"{spaceIndex}break;");
          continue;
        }
        if (statement is ForStatementSyntax fss)
        {
          foreach (var v in fss.Declaration.Variables)
          {
            localDeclarationStatements.Add(v.Identifier.ValueText);
          }

          result.AppendLine($"{spaceIndex}for(let {fss.Declaration.Variables}; {GetExpression(fss.Condition, localDeclarationStatements)}; {fss.Incrementors})" + " {");
          result.Append(GetMethodText(fss.Statement as BlockSyntax, index + 2, localDeclarationStatements));
          result.AppendLine(spaceIndex+"}");

          continue;
        }

        var a = 123;
      }

      return result.ToString();
    }

    #region 条件式を取得

    /// <summary>
    /// 条件を取得
    /// </summary>
    /// <param name="condition">ExpressionSyntaxインスタンス</param>
    /// <param name="localDeclarationStatements">ローカル変数リスト</param>
    /// <returns>TypeScriptに変換した文字列</returns>
    private string GetExpression(ExpressionSyntax condition, List<string> localDeclarationStatements)
    {
      var left = string.Empty;
      var keyword = string.Empty;
      var right = string.Empty;
      switch (condition)
      {
        case BinaryExpressionSyntax bss:
          left = bss.Left.ToString();
          if (!IsLocalDeclarationStatement(bss.Left, localDeclarationStatements))
          {
            left = "this." + left;
          }

          keyword = bss.OperatorToken.ToString();

          right = GetExpression(bss.Right, localDeclarationStatements);

          break;
        case AssignmentExpressionSyntax ass:
          left = ass.Left.ToString();
          if (!IsLocalDeclarationStatement(ass.Left, localDeclarationStatements))
          {
            left = "this." + left;
          }

          keyword = ass.OperatorToken.ToString();

          right = GetExpression(ass.Right, localDeclarationStatements);

          break;
        case IdentifierNameSyntax ins:
        case InvocationExpressionSyntax ies:
        case LiteralExpressionSyntax les:
          if (!IsLocalDeclarationStatement(condition, localDeclarationStatements))
          {
            return "this." + condition.ToString();
          }
          return condition.ToString();
        default:
          return string.Empty;
      }

      return $"{left} {keyword} {right}";
    }

    /// <summary>
    /// ローカル変数か判定
    /// </summary>
    /// <param name="es">対象インスタンス</param>
    /// <param name="localDeclarationStatements">ローカル変数リスト</param>
    /// <returns>ローカル変数か否か</returns>
    private bool IsLocalDeclarationStatement(ExpressionSyntax es, List<string> localDeclarationStatements)
    {
      var localDeclarationStatement = string.Empty;
      if (es is InvocationExpressionSyntax ies)
      {
        var memberAccessExpressionSyntax = ies.Expression as MemberAccessExpressionSyntax;
        if (memberAccessExpressionSyntax != null)
        {
          localDeclarationStatement = memberAccessExpressionSyntax.Expression.ToString();
        }
      }
      if (es is IdentifierNameSyntax ins)
      {
        localDeclarationStatement = ins.ToString();
      }

      if (string.IsNullOrEmpty(localDeclarationStatement))
      {
        return true;
      }
      
      return localDeclarationStatements.Contains(localDeclarationStatement);
    }

    #endregion

    #endregion

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
              if (RenameClasseNames.ContainsKey(result))
              {
                return RenameClasseNames[result];
              }

              result = result.Replace(".", "_", StringComparison.CurrentCulture);
              if (!Result.UnknownReferences.ContainsKey(ins.ToString()))
              {
                Result.UnknownReferences.Add(ins.ToString(), null);
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
