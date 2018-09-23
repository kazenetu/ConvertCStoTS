using ConvertCStoTS.Analyze.Statement;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static ConvertCStoTS.Common.AnalyzeUtility;

namespace ConvertCStoTS.Analyze.Methods
{
  public class Method : IMethod
  {
    /// <summary>
    /// メソッドの取得
    /// </summary>
    /// <param name="item">C#ソースを解析したインスタンス</param>
    /// <param name="isOutputMethod">メソッド出力フラグ</param>
    /// <param name="index">インデックス数(半角スペース数)</param>
    /// <returns>TypeScriptのコンストラクタに変換した文字列</returns>
    public MethodData GetMethodText(BaseMethodDeclarationSyntax item, bool isOutputMethod, int index = 0)
    {
      // メソッド出力しない場合はそのまま終了
      if (!isOutputMethod)
      {
        return null;
      }

      var returnValue = string.Empty;
      var methodName = "constructor";
      if (item is MethodDeclarationSyntax mi)
      {
        methodName = mi.Identifier.Text;
        returnValue = ClassObject.GetInstance().GetTypeScriptType(mi.ReturnType);
      }

      var parameterDataList = new List<MethodData.ParameterData>();
      foreach (var param in item.ParameterList.Parameters)
      {
        parameterDataList.Add(new MethodData.ParameterData(param.Identifier.ValueText, ClassObject.GetInstance().GetTypeScriptType(param.Type)));
      }

      // スーパークラスのコンストラクタのパラメータ数を取得
      var superMethodArgCount = -1;
      if (item is ConstructorDeclarationSyntax cds && cds.Initializer != null && cds.Initializer.ArgumentList != null)
      {
        superMethodArgCount = cds.Initializer.ArgumentList.Arguments.Count;
      }

      // メソッド情報を作成
      var methodData = new MethodData(index, methodName, GetModifierText(item.Modifiers), parameterDataList,
        GetMethodText(item.Body, index + IndentSize, parameterDataList.Select(p => p.Name).ToList()),
        returnValue, superMethodArgCount, GetComments(item.GetLeadingTrivia().ToString()));

      // メソッド情報を返す
      return methodData;
    }

    /// <summary>
    /// メソッド内処理を取得：エントリメソッド
    /// </summary>
    /// <param name="logic">BlockSyntaxインスタンス</param>
    /// <param name="index">インデックス数(半角スペース数)</param>
    /// <param name="parentLocalDeclarationStatements">親GetMethodTextのローカル変数リスト</param>
    public string GetMethodText(BlockSyntax logic, int index = 0, List<string> parentLocalDeclarationStatements = null)
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
    public string GetMethodText(SyntaxList<StatementSyntax> statements, int index = 0, List<string> parentLocalDeclarationStatements = null)
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
          var statementInstance = StatementFactory.GetStatement(this, (dynamic)statement);
          result.Append(statementInstance.ConvertStatement(index, localDeclarationStatements));
        }
        catch (Exception ex)
        {
          Console.WriteLine($"[{ex.Message}]");
          Console.WriteLine(statement.ToString());
        }
      }

      return result.ToString();
    }
  }
}
