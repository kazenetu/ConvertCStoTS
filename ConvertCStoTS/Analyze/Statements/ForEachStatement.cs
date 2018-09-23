using ConvertCStoTS.Analyze.Methods;
using ConvertCStoTS.Analyze.Statements.BaseClass;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Text;
using static ConvertCStoTS.Common.AnalyzeUtility;

namespace ConvertCStoTS.Analyze.Statements
{
  /// <summary>
  /// foreach構文のTypeScript変換
  /// </summary>
  public class ForEachStatement : BaseStatement
  {
    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="method">メソッドインスタンス</param>
    /// <param name="statement">構文インスタンス</param>
    public ForEachStatement(IMethod method, ForEachStatementSyntax statement) : base(method, statement)
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
      var statement = Statement as ForEachStatementSyntax;

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
      result.AppendLine($"{spaceIndex}for (let {statement.Identifier} in {ExpressionsInstance.GetExpression(statement.Expression, tempLocalDeclarationStatements)})" + " {");
      result.Append(MethodInstance.GetMethodText(statement.Statement as BlockSyntax, index + IndentSize, tempLocalDeclarationStatements));
      result.AppendLine(spaceIndex + "}");

      return result.ToString();
    }
  }
}
