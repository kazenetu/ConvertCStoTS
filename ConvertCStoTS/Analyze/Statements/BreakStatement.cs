using ConvertCStoTS.Analyze.Methods;
using ConvertCStoTS.Analyze.Statements.BaseClass;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Text;
using static ConvertCStoTS.Common.AnalyzeUtility;

namespace ConvertCStoTS.Analyze.Statements
{
  /// <summary>
  /// breakのTypeScript変換
  /// </summary>
  public class BreakStatement : BaseStatement
  {
    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="method">メソッドインスタンス</param>
    /// <param name="statement">構文インスタンス</param>
    public BreakStatement(IMethod method, BreakStatementSyntax statement) : base(method, statement)
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
      var result = new StringBuilder();
      var spaceIndex = GetSpace(index);

      result.AppendLine($"{spaceIndex}break;");

      return result.ToString();
    }
  }
}
