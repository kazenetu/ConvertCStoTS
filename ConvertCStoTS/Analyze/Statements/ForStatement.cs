using ConvertCStoTS.Analyze.Methods;
using ConvertCStoTS.Analyze.Statements.BaseClass;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Text;
using static ConvertCStoTS.Common.AnalyzeUtility;

namespace ConvertCStoTS.Analyze.Statements
{
  public class ForStatement : BaseStatement
  {
    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="method">メソッドインスタンス</param>
    /// <param name="statement">構文インスタンス</param>
    public ForStatement(IMethod method, ForStatementSyntax statement) : base(method, statement)
    {
    }

    /// <summary>
    /// 構文のTypeScript変換
    /// </summary>
    /// <param name="index">インデックス数</param>
    /// <param name="localDeclarationStatements">宣言済ローカル変数のリスト</param>
    /// <returns>TypeScriptに変換した文字列</returns>
    public override string ConvertStatement(int index, List<string> localDeclarationStatements)
    {
      var statement = Statement as ForStatementSyntax;

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
      result.AppendLine($"{spaceIndex}for (let {statement.Declaration.Variables}; {ExpressionsInstance.GetExpression(statement.Condition, tempLocalDeclarationStatements)}; {statement.Incrementors})" + " {");
      result.Append(MethodInstance.GetMethodText(statement.Statement as BlockSyntax, index + IndentSize, tempLocalDeclarationStatements));
      result.AppendLine(spaceIndex + "}");

      return result.ToString();
    }
  }
}
