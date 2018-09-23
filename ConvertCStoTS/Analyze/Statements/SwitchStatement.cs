using ConvertCStoTS.Analyze.Methods;
using ConvertCStoTS.Analyze.Statements.BaseClass;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Text;
using static ConvertCStoTS.Common.AnalyzeUtility;

namespace ConvertCStoTS.Analyze.Statements
{
  /// <summary>
  /// switch構文のTypeScript変換
  /// </summary>
  public class SwitchStatement : BaseStatement
  {
    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="method">メソッドインスタンス</param>
    /// <param name="statement">構文インスタンス</param>
    public SwitchStatement(IMethod method, SwitchStatementSyntax statement) : base(method, statement)
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
      var statement = Statement as SwitchStatementSyntax;

      var result = new StringBuilder();
      var spaceIndex = GetSpace(index);

      result.Append(spaceIndex);
      result.AppendLine($"switch ({ExpressionsInstance.GetExpression(statement.Expression, localDeclarationStatements)})" + " {");
      foreach (var section in statement.Sections)
      {
        foreach (var label in section.Labels)
        {
          result.AppendLine($"{GetSpace(index + IndentSize)}{label}");
        }
        result.Append(MethodInstance.GetMethodText(section.Statements, index + 4, localDeclarationStatements));
      }

      result.AppendLine(spaceIndex + "}");

      return result.ToString();
    }
  }
}
