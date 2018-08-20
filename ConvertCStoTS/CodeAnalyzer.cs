﻿using Microsoft.CodeAnalysis;
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
    /// C#とTypeScriptの変換リスト
    /// </summary>
    private readonly Dictionary<string, string> ConvertMethodNames = new Dictionary<string, string>()
    {
      {".ToString(",".toString(" },
      {".Length",".length" },
      {"int.Parse(","parseInt(" }
    };

    /// <summary>
    /// スコープキーワード
    /// </summary>
    private readonly List<string> ScopeKeywords = new List<string>()
    {
      "public","private","protected"
    };


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
        returnValue, superMethodArgCount);

      MethodDataManager.AddMethodData(methodName, methodData);

      return string.Empty;
    }

    /// <summary>
    /// スコープ取得
    /// </summary>
    /// <param name="modifiers">スコープキーワード</param>
    /// <returns>public/private/protectedのキーワード</returns>
    public string GetModifierText(SyntaxTokenList modifiers)
    {
      var scopeKeyword = modifiers.Where(modifier => ScopeKeywords.Contains(modifier.ValueText));

      return string.Join(' ', scopeKeyword);
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
            foreach (var v in lds.Declaration.Variables)
            {
              localDeclarationStatements.Add(v.Identifier.ValueText);
            }

            var varStatement = lds.Declaration.Variables.First();
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
            var tsType = GetTypeScriptType(lds.Declaration.Type, Result.UnknownReferences, RenameClasseNames);
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
        if(statement is ReturnStatementSyntax rss)
        {
          result.AppendLine($"{spaceIndex}return {GetExpression(rss.Expression, localDeclarationStatements)};");
          continue;
        }

        var a = 123;
      }

      return result.ToString();
    }

    #region 式の変換結果を取得

    /// <summary>
    /// 式の変換結果を取得
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
        case ObjectCreationExpressionSyntax oces:
          var equalsValueClauseSyntax = oces.Parent as EqualsValueClauseSyntax;
          if(equalsValueClauseSyntax == null)
          {
            return oces.ToString();
          }
          return GetCreateInitializeValue(oces.Type, equalsValueClauseSyntax);

        case BinaryExpressionSyntax bss:
          left = GetExpression(bss.Left, localDeclarationStatements);

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
        case InvocationExpressionSyntax ies:
        case MemberAccessExpressionSyntax mes:
          var invocationExpressionResult = condition.ToString();

          MemberAccessExpressionSyntax maes = null;
          if(condition is InvocationExpressionSyntax)
          {
            var localIes = condition as InvocationExpressionSyntax;
            maes = localIes.Expression as MemberAccessExpressionSyntax;

            if (localIes.Expression is MemberAccessExpressionSyntax || localIes.Expression is InvocationExpressionSyntax)
            {
              if(maes.Expression is ThisExpressionSyntax)
              {
                invocationExpressionResult = $"{GetExpression(maes.Name, localDeclarationStatements)}(";
              }
              else
              {
                invocationExpressionResult = $"{GetExpression(maes, localDeclarationStatements)}(";
              }
            }
            else
            {
              invocationExpressionResult = $"{localIes.Expression.ToString()}(";
            }

            // パラメータ設定
            var argsText = localIes.ArgumentList.Arguments.Select(arg => GetExpression(arg.Expression, localDeclarationStatements));
            invocationExpressionResult += string.Join(", ", argsText);

            invocationExpressionResult += ")";
          }
          if (condition is MemberAccessExpressionSyntax)
          {
            maes = condition as MemberAccessExpressionSyntax;

            if(maes.Expression is IdentifierNameSyntax == false)
            {
              invocationExpressionResult = $"{GetExpression(maes.Expression, localDeclarationStatements)}{maes.OperatorToken}";
              invocationExpressionResult += maes.Name;
            }
          }

          if (maes != null)
          {
            foreach(var convertMethodName in ConvertMethodNames.Keys)
            {
              if (invocationExpressionResult.Contains(convertMethodName))
              {
                invocationExpressionResult = invocationExpressionResult.Replace(convertMethodName, ConvertMethodNames[convertMethodName], StringComparison.CurrentCulture);
              }
            }
          }

          if (!IsLocalDeclarationStatement(condition, localDeclarationStatements))
          {
            if (!invocationExpressionResult.StartsWith("this.",StringComparison.CurrentCulture))
            {
              return "this." + invocationExpressionResult;
            }
          }
          return invocationExpressionResult;

        case IdentifierNameSyntax ins:
        case LiteralExpressionSyntax les:
          if (!IsLocalDeclarationStatement(condition, localDeclarationStatements))
          {
            return "this." + condition.ToString();
          }
          return condition.ToString();
        case PredefinedTypeSyntax pts:
        case ThisExpressionSyntax tes:
          return condition.ToString();
        case BaseExpressionSyntax bes:
          return "super";
        default:
          return string.Empty;
      }

      return $"{left} {keyword} {right}";
    }

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
