using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using static ConvertCStoTS.MethodData;
using static ConvertCStoTS.Common.AnalyzeUtility;

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
    /// メソッド管理クラスインスタンス
    /// </summary>
    private MethodDataManager MethodDataManager = new MethodDataManager();

    /// <summary>
    /// メソッド出力フラグ
    /// </summary>
    private readonly bool IsOutputMethod;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="isOutputMethod">メソッド出力フラグ</param>
    public CodeAnalyzer(bool isOutputMethod = true)
    {
      IsOutputMethod = isOutputMethod;
    }

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
      MethodDataManager.Clear();

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
        if (item is ClassDeclarationSyntax cds)
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

      // TSメソッド管理クラスのクリア
      MethodDataManager.Clear();

      var className = item.Identifier.ValueText;
      var superClass = string.Empty;
      if (item.BaseList != null)
      {
        superClass = $" extends {GetTypeScriptType(item.BaseList.Types[0].Type,Result.UnknownReferences,RenameClasseNames)}";
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
      if (!Result.ClassNames.Contains(className))
      {
        Result.ClassNames.Add(className);
      }

      // 子要素を設定
      foreach (var childItem in item.Members)
      {
        // インナークラスの場合は処理をしない
        if(childItem is ClassDeclarationSyntax)
        {
          continue;
        }

        try
        {
          result.Append(GetChildText((dynamic)childItem, index + 2));
        }
        catch (Exception ex)
        {
          Console.WriteLine($"[{ex.Message}]");
          Console.WriteLine(childItem.ToString());
        }
      }

      // メソッドを出力
      result.Append(MethodDataManager.GetMethodText());

      result.AppendLine($"{GetSpace(index)}{item.CloseBraceToken.ValueText}");
      return result.ToString();
    }

    /// <summary>
    /// メソッドの取得
    /// </summary>
    /// <param name="item">C#ソースを解析したインスタンス</param>
    /// <param name="index">インデックス数(半角スペース数)</param>
    /// <returns>TypeScriptのコンストラクタに変換した文字列</returns>
    private string GetChildText(BaseMethodDeclarationSyntax item, int index = 0)
    {
      // メソッド出力しない場合はそのまま終了
      if (!IsOutputMethod)
      {
        return string.Empty;
      }

      var returnValue = string.Empty;
      var methodName = "constructor";
      if (item is MethodDeclarationSyntax mi)
      {
        methodName = mi.Identifier.Text;
        returnValue = GetTypeScriptType(mi.ReturnType, Result.UnknownReferences, RenameClasseNames);
      }

      var parameterDataList = new List<ParameterData>();
      foreach (var param in item.ParameterList.Parameters)
      {
        parameterDataList.Add(new ParameterData(param.Identifier.ValueText, GetTypeScriptType(param.Type, Result.UnknownReferences, RenameClasseNames)));
      }

      // スーパークラスのコンストラクタのパラメータ数を取得
      var superMethodArgCount = -1;
      if (item is ConstructorDeclarationSyntax cds && cds.Initializer != null && cds.Initializer.ArgumentList != null)
      {
        superMethodArgCount = cds.Initializer.ArgumentList.Arguments.Count;
      }

      // TSメソッド管理クラスにメソッド情報を追加
      var methodData = new MethodData(index, GetModifierText(item.Modifiers), parameterDataList,
        GetMethodText(item.Body, index + 2, parameterDataList.Select(p => p.Name).ToList()),
        returnValue, superMethodArgCount, GetComments(item.GetLeadingTrivia().ToString()));

      MethodDataManager.AddMethodData(methodName, methodData);

      return string.Empty;
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
        catch(Exception ex)
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
      result.Append(GetMethodText(statement.Statement as BlockSyntax, index + 2, localDeclarationStatements));
      result.AppendLine($"{spaceIndex}" + "}");

      // ElseStatement
      if (statement.Else != null)
      {
        result.AppendLine($"{spaceIndex}" + "else {");
        result.Append(GetMethodText(statement.Else.Statement as BlockSyntax, index + 2));
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
          result.AppendLine($"{GetSpace(index + 2)}{label}");
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
      result.Append(GetMethodText(statement.Statement as BlockSyntax, index + 2, tempLocalDeclarationStatements));
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
      result.Append(GetMethodText(statement.Statement as BlockSyntax, index + 2, tempLocalDeclarationStatements));
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
      result.Append(GetMethodText(statement.Statement as BlockSyntax, index + 2, localDeclarationStatements));
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
    public string ConvertExpression(ObjectCreationExpressionSyntax condition, List<string> localDeclarationStatements)
    {
      var equalsValueClauseSyntax = condition.Parent as EqualsValueClauseSyntax;
      if (equalsValueClauseSyntax == null)
      {
        return condition.ToString();
      }
      return GetCreateInitializeValue(condition.Type, equalsValueClauseSyntax);
    }

    /// <summary>
    /// 2項演算子のTypeScript変換
    /// </summary>
    /// <param name="condition">ExpressionSyntaxインスタンス</param>
    /// <param name="localDeclarationStatements">ローカル変数リスト</param>
    /// <returns>TypeScriptに変換した文字列</returns>
    public string ConvertExpression(BinaryExpressionSyntax condition, List<string> localDeclarationStatements)
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
    public string ConvertExpression(AssignmentExpressionSyntax condition, List<string> localDeclarationStatements)
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
    public string ConvertExpression(InvocationExpressionSyntax condition, List<string> localDeclarationStatements)
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
    public string ConvertExpression(MemberAccessExpressionSyntax condition, List<string> localDeclarationStatements)
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
      if(condition.Expression is ElementAccessExpressionSyntax eaes)
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
    public string ConvertExpression(IdentifierNameSyntax condition, List<string> localDeclarationStatements)
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
    public string ConvertExpression(LiteralExpressionSyntax condition, List<string> localDeclarationStatements)
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
    public string ConvertExpression(PostfixUnaryExpressionSyntax condition, List<string> localDeclarationStatements)
    {
      return condition.ToString();
    }

    /// <summary>
    /// 要素アクセス構文のTypeScript変換
    /// </summary>
    /// <param name="condition">ExpressionSyntaxインスタンス</param>
    /// <param name="localDeclarationStatements">ローカル変数リスト</param>
    /// <returns>TypeScriptに変換した文字列</returns>
    public string ConvertExpression(ElementAccessExpressionSyntax condition, List<string> localDeclarationStatements)
    {
      return $"{condition.Expression.ToString()}.get({condition.ArgumentList.Arguments.ToString()})";
    }

    /// <summary>
    /// 定義済みタイプの構文のTypeScript変換
    /// </summary>
    /// <param name="condition">ExpressionSyntaxインスタンス</param>
    /// <param name="localDeclarationStatements">ローカル変数リスト</param>
    /// <returns>TypeScriptに変換した文字列</returns>
    public string ConvertExpression(PredefinedTypeSyntax condition, List<string> localDeclarationStatements)
    {
      return condition.ToString();
    }

    /// <summary>
    /// thisのTypeScript変換
    /// </summary>
    /// <param name="condition">ExpressionSyntaxインスタンス</param>
    /// <param name="localDeclarationStatements">ローカル変数リスト</param>
    /// <returns>TypeScriptに変換した文字列</returns>
    public string ConvertExpression(ThisExpressionSyntax condition, List<string> localDeclarationStatements)
    {
      return condition.ToString();
    }

    /// <summary>
    /// baseのTypeScript変換
    /// </summary>
    /// <param name="condition">ExpressionSyntaxインスタンス</param>
    /// <param name="localDeclarationStatements">ローカル変数リスト</param>
    /// <returns>TypeScriptに変換した文字列</returns>
    public string ConvertExpression(BaseExpressionSyntax condition, List<string> localDeclarationStatements)
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
    
    #endregion

    #endregion

    /// <summary>
    /// プロパティ取得
    /// </summary>
    /// <param name="item">C#ソースを解析したインスタンス</param>
    /// <param name="index">インデックス数(半角スペース数)</param>
    /// <returns>TypeScriptのクラスフィールドに変換した文字列</returns>
    private string GetChildText(PropertyDeclarationSyntax item, int index = 0)
    {
      var result = new StringBuilder();

      result.Append(GetComments(item.GetLeadingTrivia().ToString()));
      result.Append($"{GetSpace(index)}{GetModifierText(item.Modifiers)} {item.Identifier.ValueText}: {GetTypeScriptType(item.Type, Result.UnknownReferences, RenameClasseNames)}");

      // 初期化処理を追加
      result.Append(GetCreateInitializeValue(item.Type, item.Initializer));

      result.AppendLine(";");

      return result.ToString();
    }

    /// <summary>
    /// フィールド宣言時の初期化を設定する
    /// </summary>
    /// <param name="type">フィールドの型</param>
    /// <param name="initializer">初期化情報</param>
    /// <returns>TypeScriptの初期化文字列</returns>
    private string GetCreateInitializeValue(TypeSyntax type, EqualsValueClauseSyntax initializer)
    {
      if (initializer != null)
      {
        return $" {GetEqualsValue(initializer, Result.UnknownReferences, RenameClasseNames)}";
      }
      else
      {
        switch (type)
        {
          case NullableTypeSyntax nts:
            return " = null";
          case GenericNameSyntax gts:
            return $" = new {GetTypeScriptType(type, Result.UnknownReferences, RenameClasseNames)}()";
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
