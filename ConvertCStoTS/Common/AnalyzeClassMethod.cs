using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static ConvertCStoTS.Common.AnalyzeUtility;

namespace ConvertCStoTS.Common
{
  /// <summary>
  /// クラスメソッド出力クラス
  /// </summary>
  public class AnalyzeClassMethod
  {
    #region インスタンスフィールド
    
    /// <summary>
    /// 解析結果
    /// </summary>
    private AnalyzeResult Result;

    /// <summary>
    /// 内部クラスの名称変更情報
    /// </summary>
    /// <remarks>「親クラス_内部クラス」で表現する</remarks>
    private readonly Dictionary<string, string> RenameClasseNames;

    /// <summary>
    /// メソッド出力フラグ
    /// </summary>
    private readonly bool IsOutputMethod;

    #endregion

    #region コンストラクタ

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="result">解析結果インスタンス</param>
    /// <param name="renameClasseNames">内部クラスコレクション</param>
    /// <param name="methodDataManager">メソッド管理クラスインスタンス</param>
    public AnalyzeClassMethod(AnalyzeResult result, Dictionary<string, string> renameClasseNames, bool isOutputMethod)
    {
      Result = result;
      RenameClasseNames = renameClasseNames;
      IsOutputMethod = isOutputMethod;
    }

    #endregion

    #region エンドポイントメソッド

    /// <summary>
    /// メソッドの取得
    /// </summary>
    /// <param name="item">C#ソースを解析したインスタンス</param>
    /// <param name="isOutputMethod">メソッド出力フラグ</param>
    /// <param name="index">インデックス数(半角スペース数)</param>
    /// <returns>TypeScriptのコンストラクタに変換した文字列</returns>
    public void GetMethodText(BaseMethodDeclarationSyntax item, ref MethodDataManager methodDataManager, int index = 0)
    {
      // メソッド出力しない場合はそのまま終了
      if (!IsOutputMethod)
      {
        return;
      }

      var returnValue = string.Empty;
      var methodName = "constructor";
      if (item is MethodDeclarationSyntax mi)
      {
        methodName = mi.Identifier.Text;
        returnValue = GetTypeScriptType(mi.ReturnType, Result.UnknownReferences, RenameClasseNames);
      }

      var parameterDataList = new List<MethodData.ParameterData>();
      foreach (var param in item.ParameterList.Parameters)
      {
        parameterDataList.Add(new MethodData.ParameterData(param.Identifier.ValueText, GetTypeScriptType(param.Type, Result.UnknownReferences, RenameClasseNames)));
      }

      // スーパークラスのコンストラクタのパラメータ数を取得
      var superMethodArgCount = -1;
      if (item is ConstructorDeclarationSyntax cds && cds.Initializer != null && cds.Initializer.ArgumentList != null)
      {
        superMethodArgCount = cds.Initializer.ArgumentList.Arguments.Count;
      }

      // TSメソッド管理クラスにメソッド情報を追加
      var methodData = new MethodData(index, GetModifierText(item.Modifiers), parameterDataList,
        GetMethodText(item.Body, index + IndentSize, parameterDataList.Select(p => p.Name).ToList()),
        returnValue, superMethodArgCount, GetComments(item.GetLeadingTrivia().ToString()));

      methodDataManager.AddMethodData(methodName, methodData);

    }
    #endregion

    #region メソッド内処理の取得

    /// <summary>
    /// メソッド内処理を取得：エントリメソッド
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
      return GetMethodText(logic.Statements, index, parentLocalDeclarationStatements);
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
      if (parentLocalDeclarationStatements != null)
      {
        localDeclarationStatements.AddRange(parentLocalDeclarationStatements);
      }

      // ローカル変数に予約語を追加する
      localDeclarationStatements.Add(nameof(String));
      localDeclarationStatements.Add(nameof(Decimal));
      localDeclarationStatements.Add(nameof(Boolean));
      localDeclarationStatements.Add(nameof(Int16));
      localDeclarationStatements.Add(nameof(Int32));
      localDeclarationStatements.Add(nameof(Int64));
      localDeclarationStatements.Add(nameof(Single));
      localDeclarationStatements.Add(nameof(Double));
      localDeclarationStatements.Add(nameof(DateTime));

      // 処理単位の文字列を取得
      var spaceIndex = GetSpace(index);
      var result = new StringBuilder();
      foreach (var statement in statements)
      {
        try
        {
          // コメント出力
          var comments = statement.GetLeadingTrivia().Where(trivia => trivia.Kind() != SyntaxKind.WhitespaceTrivia);
          foreach (var comment in comments)
          {
            if (comment.IsKind(SyntaxKind.SingleLineCommentTrivia))
            {
              result.Append(spaceIndex);
            }
            result.Append($"{comment}");
          }

          // TypeScript変換
          result.Append(ConvertStatement((dynamic)statement, index, localDeclarationStatements));
        }
        catch (Exception ex)
        {
          Console.WriteLine($"[{ex.Message}]");
          Console.WriteLine(statement.ToString());
        }
      }

      return result.ToString();
    }

    #region statementごとの変換処理

    /// <summary>
    /// ローカル変数宣言のTypeScript変換
    /// </summary>
    /// <param name="statement">対象行</param>
    /// <param name="index">インデックス数</param>
    /// <param name="localDeclarationStatements">宣言済ローカル変数のリスト</param>
    /// <returns>TypeScriptに変換した文字列</returns>
    private string ConvertStatement(LocalDeclarationStatementSyntax statement, int index, List<string> localDeclarationStatements)
    {
      var result = new StringBuilder();
      var spaceIndex = GetSpace(index);

      if (statement.Declaration.Type.IsVar)
      {
        foreach (var v in statement.Declaration.Variables)
        {
          localDeclarationStatements.Add(v.Identifier.ValueText);
        }

        var varStatement = statement.Declaration.Variables.First();
        var intializeValue = GetExpression(varStatement.Initializer.Value, localDeclarationStatements);

        result.Append($"{spaceIndex}let {varStatement.Identifier.ValueText}");
        if (!intializeValue.Contains("="))
        {
          result.Append(" = ");
        }
        result.AppendLine($"{intializeValue};");
      }
      else
      {
        var tsType = GetTypeScriptType(statement.Declaration.Type, Result.UnknownReferences, RenameClasseNames);
        foreach (var v in statement.Declaration.Variables)
        {
          result.AppendLine($"{spaceIndex}let {v.Identifier}: {tsType} {v.Initializer};");
          localDeclarationStatements.Add(v.Identifier.ValueText);
        }
      }

      return result.ToString();
    }

    /// <summary>
    /// if構文のTypeScript変換
    /// </summary>
    /// <param name="statement">対象行</param>
    /// <param name="index">インデックス数</param>
    /// <param name="localDeclarationStatements">宣言済ローカル変数のリスト</param>
    /// <returns>TypeScriptに変換した文字列</returns>
    private string ConvertStatement(IfStatementSyntax statement, int index, List<string> localDeclarationStatements)
    {
      var result = new StringBuilder();
      var spaceIndex = GetSpace(index);

      result.AppendLine($"{spaceIndex}if ({GetExpression(statement.Condition, localDeclarationStatements)})" + " {");
      result.Append(GetMethodText(statement.Statement as BlockSyntax, index + IndentSize, localDeclarationStatements));
      result.AppendLine($"{spaceIndex}" + "}");

      // ElseStatement
      if (statement.Else != null)
      {
        result.AppendLine($"{spaceIndex}" + "else {");
        result.Append(GetMethodText(statement.Else.Statement as BlockSyntax, index + IndentSize));
        result.AppendLine($"{spaceIndex}" + "}");
      }

      return result.ToString();
    }

    /// <summary>
    /// 式のTypeScript変換
    /// </summary>
    /// <param name="statement">対象行</param>
    /// <param name="index">インデックス数</param>
    /// <param name="localDeclarationStatements">宣言済ローカル変数のリスト</param>
    /// <returns>TypeScriptに変換した文字列</returns>
    private string ConvertStatement(ExpressionStatementSyntax statement, int index, List<string> localDeclarationStatements)
    {
      var result = new StringBuilder();
      var spaceIndex = GetSpace(index);

      result.AppendLine($"{spaceIndex}{GetExpression(statement.Expression, localDeclarationStatements)};");

      return result.ToString();
    }

    /// <summary>
    /// switch構文のTypeScript変換
    /// </summary>
    /// <param name="statement">対象行</param>
    /// <param name="index">インデックス数</param>
    /// <param name="localDeclarationStatements">宣言済ローカル変数のリスト</param>
    /// <returns>TypeScriptに変換した文字列</returns>
    private string ConvertStatement(SwitchStatementSyntax statement, int index, List<string> localDeclarationStatements)
    {
      var result = new StringBuilder();
      var spaceIndex = GetSpace(index);

      result.Append(spaceIndex);
      result.AppendLine($"switch ({GetExpression(statement.Expression, localDeclarationStatements)})" + " {");
      foreach (var section in statement.Sections)
      {
        foreach (var label in section.Labels)
        {
          result.AppendLine($"{GetSpace(index + IndentSize)}{label}");
        }
        result.Append(GetMethodText(section.Statements, index + 4, localDeclarationStatements));
      }

      result.AppendLine(spaceIndex + "}");

      return result.ToString();
    }

    /// <summary>
    /// breakのTypeScript変換
    /// </summary>
    /// <param name="statement">対象行</param>
    /// <param name="index">インデックス数</param>
    /// <param name="localDeclarationStatements">宣言済ローカル変数のリスト</param>
    /// <returns>TypeScriptに変換した文字列</returns>
    private string ConvertStatement(BreakStatementSyntax statement, int index, List<string> localDeclarationStatements)
    {
      var result = new StringBuilder();
      var spaceIndex = GetSpace(index);

      result.AppendLine($"{spaceIndex}break;");

      return result.ToString();
    }

    /// <summary>
    /// for構文のTypeScript変換
    /// </summary>
    /// <param name="statement">対象行</param>
    /// <param name="index">インデックス数</param>
    /// <param name="localDeclarationStatements">宣言済ローカル変数のリスト</param>
    /// <returns>TypeScriptに変換した文字列</returns>
    private string ConvertStatement(ForStatementSyntax statement, int index, List<string> localDeclarationStatements)
    {
      var result = new StringBuilder();
      var spaceIndex = GetSpace(index);

      // 一時的なローカル変数を作成
      var tempLocalDeclarationStatements = new List<string>();
      tempLocalDeclarationStatements.AddRange(localDeclarationStatements);
      foreach (var v in statement.Declaration.Variables)
      {
        tempLocalDeclarationStatements.Add(v.Identifier.ValueText);
      }

      // 構文作成
      result.AppendLine($"{spaceIndex}for (let {statement.Declaration.Variables}; {GetExpression(statement.Condition, tempLocalDeclarationStatements)}; {statement.Incrementors})" + " {");
      result.Append(GetMethodText(statement.Statement as BlockSyntax, index + IndentSize, tempLocalDeclarationStatements));
      result.AppendLine(spaceIndex + "}");

      return result.ToString();
    }

    /// <summary>
    /// foreach構文のTypeScript変換
    /// </summary>
    /// <param name="statement">対象行</param>
    /// <param name="index">インデックス数</param>
    /// <param name="localDeclarationStatements">宣言済ローカル変数のリスト</param>
    /// <returns>TypeScriptに変換した文字列</returns>
    private string ConvertStatement(ForEachStatementSyntax statement, int index, List<string> localDeclarationStatements)
    {
      var result = new StringBuilder();
      var spaceIndex = GetSpace(index);

      // 一時的なローカル変数を作成
      var tempLocalDeclarationStatements = new List<string>();
      tempLocalDeclarationStatements.AddRange(localDeclarationStatements);
      if (!tempLocalDeclarationStatements.Contains(statement.Identifier.ToString()))
      {
        tempLocalDeclarationStatements.Add(statement.Identifier.ToString());
      }

      // 構文作成
      result.AppendLine($"{spaceIndex}for (let {statement.Identifier} in {GetExpression(statement.Expression, tempLocalDeclarationStatements)})" + " {");
      result.Append(GetMethodText(statement.Statement as BlockSyntax, index + IndentSize, tempLocalDeclarationStatements));
      result.AppendLine(spaceIndex + "}");

      return result.ToString();
    }

    /// <summary>
    /// while構文のTypeScript変換
    /// </summary>
    /// <param name="statement">対象行</param>
    /// <param name="index">インデックス数</param>
    /// <param name="localDeclarationStatements">宣言済ローカル変数のリスト</param>
    /// <returns>TypeScriptに変換した文字列</returns>
    private string ConvertStatement(WhileStatementSyntax statement, int index, List<string> localDeclarationStatements)
    {
      var result = new StringBuilder();
      var spaceIndex = GetSpace(index);

      result.AppendLine($"{spaceIndex}while ({GetExpression(statement.Condition, localDeclarationStatements)})" + " {");
      result.Append(GetMethodText(statement.Statement as BlockSyntax, index + IndentSize, localDeclarationStatements));
      result.AppendLine(spaceIndex + "}");

      return result.ToString();
    }

    /// <summary>
    /// returnのTypeScript変換
    /// </summary>
    /// <param name="statement">対象行</param>
    /// <param name="index">インデックス数</param>
    /// <param name="localDeclarationStatements">宣言済ローカル変数のリスト</param>
    /// <returns>TypeScriptに変換した文字列</returns>
    private string ConvertStatement(ReturnStatementSyntax statement, int index, List<string> localDeclarationStatements)
    {
      var result = new StringBuilder();
      var spaceIndex = GetSpace(index);

      result.AppendLine($"{spaceIndex}return {GetExpression(statement.Expression, localDeclarationStatements)};");

      return result.ToString();
    }


    #endregion


    #region 式の変換結果を取得

    /// <summary>
    /// 式の変換結果を取得
    /// </summary>
    /// <param name="condition">ExpressionSyntaxインスタンス</param>
    /// <param name="localDeclarationStatements">ローカル変数リスト</param>
    /// <returns>TypeScriptに変換した文字列</returns>
    private string GetExpression(ExpressionSyntax condition, List<string> localDeclarationStatements)
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
      return GetCreateInitializeValue(condition.Type, equalsValueClauseSyntax,Result.UnknownReferences,RenameClasseNames);
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
      result = ReplaceMethodName(result);

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
      result = ReplaceMethodName(result);

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

    #endregion

    #endregion

  }
}
