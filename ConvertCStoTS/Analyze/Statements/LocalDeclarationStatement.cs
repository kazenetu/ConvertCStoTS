using ConvertCStoTS.Analyze.Methods;
using ConvertCStoTS.Analyze.Statements.BaseClass;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Text;
using static ConvertCStoTS.Common.AnalyzeUtility;

namespace ConvertCStoTS.Analyze.Statements
{
  /// <summary>
  /// ローカル変数宣言のTypeScript変換
  /// </summary>
  public class LocalDeclarationStatement : BaseStatement
  {
    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="method">メソッドインスタンス</param>
    /// <param name="statement">構文インスタンス</param>
    public LocalDeclarationStatement(IMethod method, LocalDeclarationStatementSyntax statement) : base(method, statement)
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
      var statement = Statement as LocalDeclarationStatementSyntax;

      var result = new StringBuilder();
      var spaceIndex = GetSpace(index);

      if (statement.Declaration.Type.IsVar)
      {
        foreach (var v in statement.Declaration.Variables)
        {
          localDeclarationStatements.Add(v.Identifier.ValueText);
        }

        var varStatement = statement.Declaration.Variables.First();
        var intializeValue = ExpressionsInstance.GetExpression(varStatement.Initializer.Value, localDeclarationStatements);

        result.Append($"{spaceIndex}let {varStatement.Identifier.ValueText}");
        if (!intializeValue.Contains("="))
        {
          result.Append(" = ");
        }
        result.AppendLine($"{intializeValue};");
      }
      else
      {
        var tsType = ClassObject.GetInstance().GetTypeScriptType(statement.Declaration.Type);
        foreach (var v in statement.Declaration.Variables)
        {
          result.AppendLine($"{spaceIndex}let {v.Identifier}: {tsType} {v.Initializer};");
          localDeclarationStatements.Add(v.Identifier.ValueText);
        }
      }

      return result.ToString();
    }
  }
}
