using ConvertCStoTS.Analyze.Methods;
using ConvertCStoTS.Analyze.Statements.BaseClass;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Text;
using static ConvertCStoTS.Common.AnalyzeUtility;

namespace ConvertCStoTS.Analyze.Statements
{
  public class LocalFunctionStatement : BaseStatement
  {
    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="method">メソッドインスタンス</param>
    /// <param name="statement">構文インスタンス</param>
    public LocalFunctionStatement(IMethod method, LocalFunctionStatementSyntax statement) : base(method, statement)
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
      var statement = Statement as LocalFunctionStatementSyntax;
      var classObject = ClassObject.GetInstance();

      var result = new StringBuilder();
      var spaceIndex = GetSpace(index);

      // 一時的なローカル変数とメソッドパラメータを作成
      var methodParams= new List<string>();
      var tempLocalDeclarationStatements = new List<string>();
      tempLocalDeclarationStatements.AddRange(localDeclarationStatements);
      foreach (var v in statement.ParameterList.Parameters)
      {
        tempLocalDeclarationStatements.Add(v.Identifier.ValueText);

        var methodParam = $"{v.Identifier.ValueText}: {classObject.GetTypeScriptType(v.Type)}";
        if(v.Default != null)
        {
          methodParam += " = " + ExpressionsInstance.GetExpression(v.Default.Value, tempLocalDeclarationStatements);
        }

        methodParams.Add(methodParam);
      }

      // 構文作成
      result.AppendLine($"{spaceIndex}let {statement.Identifier.Text} = ({string.Join(", ",methodParams)}): {classObject.GetTypeScriptType(statement.ReturnType)} => {{");
      if(statement.Body == null)
      {
        result.AppendLine($"{GetSpace(index + IndentSize)}return {ExpressionsInstance.GetExpression(statement.ExpressionBody.Expression, tempLocalDeclarationStatements)};");
      }
      else
      {
        result.Append(MethodInstance.GetMethodText(statement.Body as BlockSyntax, index + IndentSize, tempLocalDeclarationStatements));
      }
      result.AppendLine(spaceIndex + "}");

      return result.ToString();
    }
  }
}
