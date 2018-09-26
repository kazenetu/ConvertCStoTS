using ConvertCStoTS.Common;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ConvertCStoTS.Analyze.Expressions
{
  /// <summary>
  /// 式変換クラス
  /// </summary>
  public class ConvertExpressions
  {
    /// <summary>
    /// 式の変換結果を取得
    /// </summary>
    /// <param name="condition">ExpressionSyntaxインスタンス</param>
    /// <param name="localDeclarationStatements">ローカル変数リスト</param>
    /// <returns>TypeScriptに変換した文字列</returns>
    public string GetExpression(ExpressionSyntax condition, List<string> localDeclarationStatements)
    {
      var result = string.Empty;
      try
      {
        result = ConvertExpression((dynamic)condition, localDeclarationStatements);
      }
      catch (Exception ex)
      {
        Console.WriteLine($"[{ex.Message}]");
        Console.WriteLine(condition.ToString());
        Console.WriteLine($".............Type[{condition.GetType().Name}]");
        Console.WriteLine("------------------------------------------------");
      }
      return result;
    }

    #region Expressionごとの変換処理

    /// <summary>
    /// オブジェクト生成処理のTypeScript変換
    /// </summary>
    /// <param name="condition">ExpressionSyntaxインスタンス</param>
    /// <param name="localDeclarationStatements">ローカル変数リスト</param>
    /// <returns>TypeScriptに変換した文字列</returns>
    private string ConvertExpression(ObjectCreationExpressionSyntax condition, List<string> localDeclarationStatements)
    {
      var equalsValueClauseSyntax = condition.Parent as EqualsValueClauseSyntax;
      if (equalsValueClauseSyntax == null)
      {
        return condition.ToString();
      }
      return ClassObject.GetInstance().GetCreateInitializeValue(condition.Type, equalsValueClauseSyntax);
    }

    /// <summary>
    /// 2項演算子のTypeScript変換
    /// </summary>
    /// <param name="condition">ExpressionSyntaxインスタンス</param>
    /// <param name="localDeclarationStatements">ローカル変数リスト</param>
    /// <returns>TypeScriptに変換した文字列</returns>
    private string ConvertExpression(BinaryExpressionSyntax condition, List<string> localDeclarationStatements)
    {
      var left = GetExpression(condition.Left, localDeclarationStatements);
      var keyword = condition.OperatorToken.ToString();
      var right = GetExpression(condition.Right, localDeclarationStatements);

      return $"{left} {keyword} {right}";
    }

    /// <summary>
    /// 代入式のTypeScript変換
    /// </summary>
    /// <param name="condition">ExpressionSyntaxインスタンス</param>
    /// <param name="localDeclarationStatements">ローカル変数リスト</param>
    /// <returns>TypeScriptに変換した文字列</returns>
    private string ConvertExpression(AssignmentExpressionSyntax condition, List<string> localDeclarationStatements)
    {
      var left = GetExpression(condition.Left, localDeclarationStatements);
      var keyword = condition.OperatorToken.ToString();
      var right = GetExpression(condition.Right, localDeclarationStatements);

      return $"{left} {keyword} {right}";
    }

    /// <summary>
    /// 呼び出し式構文のTypeScript変換
    /// </summary>
    /// <param name="condition">ExpressionSyntaxインスタンス</param>
    /// <param name="localDeclarationStatements">ローカル変数リスト</param>
    /// <returns>TypeScriptに変換した文字列</returns>
    private string ConvertExpression(InvocationExpressionSyntax condition, List<string> localDeclarationStatements)
    {
      var result = condition.ToString();
      var memberAccessExpression = condition.Expression as MemberAccessExpressionSyntax;

      if (condition.Expression is MemberAccessExpressionSyntax || condition.Expression is InvocationExpressionSyntax)
      {
        if (memberAccessExpression.Expression is ThisExpressionSyntax)
        {
          result = $"{GetExpression(memberAccessExpression.Name, localDeclarationStatements)}(";
        }
        else
        {
          result = $"{GetExpression(memberAccessExpression, localDeclarationStatements)}(";
        }
      }
      else
      {
        result = $"{condition.Expression.ToString()}(";
      }

      // パラメータ設定
      var argsText = condition.ArgumentList.Arguments.Select(arg => GetExpression(arg.Expression, localDeclarationStatements));
      result += string.Join(", ", argsText);

      result += ")";

      // メソッドをTypeScript用に置換え
      result = AnalyzeUtility.ReplaceMethodName(result);

      // thisをつけるかの判定
      if (!IsLocalDeclarationStatement(condition, localDeclarationStatements))
      {
        if (!result.StartsWith("this.", StringComparison.CurrentCulture))
        {
          return "this." + result;
        }
      }
      return result;
    }

    /// <summary>
    /// メンバーアクセス式構文のTypeScript変換
    /// </summary>
    /// <param name="condition">ExpressionSyntaxインスタンス</param>
    /// <param name="localDeclarationStatements">ローカル変数リスト</param>
    /// <returns>TypeScriptに変換した文字列</returns>
    private string ConvertExpression(MemberAccessExpressionSyntax condition, List<string> localDeclarationStatements)
    {
      var result = condition.ToString();

      if (condition.Expression is IdentifierNameSyntax == false)
      {
        result = $"{GetExpression(condition.Expression, localDeclarationStatements)}{condition.OperatorToken}";
        result += condition.Name;
      }

      // メソッドをTypeScript用に置換え
      result = AnalyzeUtility.ReplaceMethodName(result);

      // thisをつけるかの判定
      var targetCondition = condition as ExpressionSyntax;
      if (condition.Expression is ElementAccessExpressionSyntax eaes)
      {
        targetCondition = eaes.Expression;
      }
      if (!IsLocalDeclarationStatement(targetCondition, localDeclarationStatements))
      {
        if (!result.StartsWith("this.", StringComparison.CurrentCulture))
        {
          return "this." + result;
        }
      }
      return result;
    }

    /// <summary>
    /// 識別子名構文のTypeScript変換
    /// </summary>
    /// <param name="condition">ExpressionSyntaxインスタンス</param>
    /// <param name="localDeclarationStatements">ローカル変数リスト</param>
    /// <returns>TypeScriptに変換した文字列</returns>
    private string ConvertExpression(IdentifierNameSyntax condition, List<string> localDeclarationStatements)
    {
      if (!IsLocalDeclarationStatement(condition, localDeclarationStatements))
      {
        return "this." + condition.ToString();
      }
      return condition.ToString();
    }

    /// <summary>
    /// リテラル式構文のTypeScript変換
    /// </summary>
    /// <param name="condition">ExpressionSyntaxインスタンス</param>
    /// <param name="localDeclarationStatements">ローカル変数リスト</param>
    /// <returns>TypeScriptに変換した文字列</returns>
    private string ConvertExpression(LiteralExpressionSyntax condition, List<string> localDeclarationStatements)
    {
      if (!IsLocalDeclarationStatement(condition, localDeclarationStatements))
      {
        return "this." + condition.ToString();
      }
      return condition.ToString();
    }

    /// <summary>
    /// 単項式構文のPostfixUnaryExpressionSyntax
    /// </summary>
    /// <param name="condition">ExpressionSyntaxインスタンス</param>
    /// <param name="localDeclarationStatements">ローカル変数リスト</param>
    /// <returns>TypeScriptに変換した文字列</returns>
    private string ConvertExpression(PostfixUnaryExpressionSyntax condition, List<string> localDeclarationStatements)
    {
      return condition.ToString();
    }

    /// <summary>
    /// 要素アクセス構文のTypeScript変換
    /// </summary>
    /// <param name="condition">ExpressionSyntaxインスタンス</param>
    /// <param name="localDeclarationStatements">ローカル変数リスト</param>
    /// <returns>TypeScriptに変換した文字列</returns>
    private string ConvertExpression(ElementAccessExpressionSyntax condition, List<string> localDeclarationStatements)
    {
      return $"{condition.Expression.ToString()}.get({condition.ArgumentList.Arguments.ToString()})";
    }

    /// <summary>
    /// 定義済みタイプの構文のTypeScript変換
    /// </summary>
    /// <param name="condition">ExpressionSyntaxインスタンス</param>
    /// <param name="localDeclarationStatements">ローカル変数リスト</param>
    /// <returns>TypeScriptに変換した文字列</returns>
    private string ConvertExpression(PredefinedTypeSyntax condition, List<string> localDeclarationStatements)
    {
      return condition.ToString();
    }

    /// <summary>
    /// thisのTypeScript変換
    /// </summary>
    /// <param name="condition">ExpressionSyntaxインスタンス</param>
    /// <param name="localDeclarationStatements">ローカル変数リスト</param>
    /// <returns>TypeScriptに変換した文字列</returns>
    private string ConvertExpression(ThisExpressionSyntax condition, List<string> localDeclarationStatements)
    {
      return condition.ToString();
    }

    /// <summary>
    /// baseのTypeScript変換
    /// </summary>
    /// <param name="condition">ExpressionSyntaxインスタンス</param>
    /// <param name="localDeclarationStatements">ローカル変数リスト</param>
    /// <returns>TypeScriptに変換した文字列</returns>
    private string ConvertExpression(BaseExpressionSyntax condition, List<string> localDeclarationStatements)
    {
      return "super";
    }

    #endregion

    #region ローカル変数判定

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
        var localMaes = ies.Expression as MemberAccessExpressionSyntax;
        ExpressionSyntax ExpressionSyntaxResult = null;
        if (localMaes?.Expression is ThisExpressionSyntax)
        {
          ExpressionSyntaxResult = GetExpressionSyntax(localMaes);
        }
        else
        {
          ExpressionSyntaxResult = GetExpressionSyntax(ies);
        }

        if (ExpressionSyntaxResult is PredefinedTypeSyntax ||
            ExpressionSyntaxResult is BaseExpressionSyntax)
        {
          return true;
        }
        localDeclarationStatement = ExpressionSyntaxResult.ToString();
      }
      if (es is MemberAccessExpressionSyntax maes)
      {
        var ExpressionSyntaxResult = GetExpressionSyntax(maes);
        if (ExpressionSyntaxResult is PredefinedTypeSyntax ||
            ExpressionSyntaxResult is BaseExpressionSyntax)
        {
          return true;
        }
        localDeclarationStatement = ExpressionSyntaxResult.ToString();
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

    /// <summary>
    /// ローカル変数判定：InvocationExpressionSyntax
    /// </summary>
    /// <param name="item">InvocationExpressionSyntaxインスタンス</param>
    /// <returns>item.Expressionの値</returns>
    private ExpressionSyntax GetExpressionSyntax(InvocationExpressionSyntax item)
    {
      if (item.Expression is IdentifierNameSyntax)
      {
        return item.Expression;
      }
      if (item.Expression is PredefinedTypeSyntax)
      {
        return item.Expression;
      }
      if (item.Expression is MemberAccessExpressionSyntax maes)
      {
        return GetExpressionSyntax(maes);
      }
      return item;
    }

    /// <summary>
    /// ローカル変数判定：MemberAccessExpressionSyntax
    /// </summary>
    /// <param name="item">MemberAccessExpressionSyntaxインスタンス</param>
    /// <returns>item.Expressionの値</returns>
    private ExpressionSyntax GetExpressionSyntax(MemberAccessExpressionSyntax item)
    {
      if (item.Expression is ThisExpressionSyntax)
      {
        return item.Name;
      }
      if (item.Expression is BaseExpressionSyntax)
      {
        return item.Expression;
      }
      if (item.Expression is IdentifierNameSyntax)
      {
        return item.Expression;
      }
      if (item.Expression is PredefinedTypeSyntax)
      {
        return item.Expression;
      }
      if (item.Expression is InvocationExpressionSyntax ies)
      {
        return GetExpressionSyntax(ies);
      }
      if (item.Expression is MemberAccessExpressionSyntax maes)
      {
        return GetExpressionSyntax(maes);
      }
      if (item.Expression is ElementAccessExpressionSyntax eaes)
      {
        return eaes.Expression;
      }
      return item;
    }

    #endregion

  }
}
