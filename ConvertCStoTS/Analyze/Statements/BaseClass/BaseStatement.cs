using ConvertCStoTS.Analyze.Expressions;
using ConvertCStoTS.Analyze.Methods;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace ConvertCStoTS.Analyze.Statements.BaseClass
{
  /// <summary>
  /// 構文のスーパークラス
  /// </summary>
  public abstract class BaseStatement
  {
    /// <summary>
    /// メソッドインスタンス
    /// </summary>
    protected IMethod MethodInstance;

    /// <summary>
    /// 式変換クラスインスタンス
    /// </summary>
    protected ConvertExpressions ExpressionsInstance = new ConvertExpressions();

    /// <summary>
    /// 構文インスタンス
    /// </summary>
    protected StatementSyntax Statement;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="method">メソッドインスタンス</param>
    /// <param name="statement">構文インスタンス</param>
    public BaseStatement(IMethod method, StatementSyntax statement)
    {
      MethodInstance = method;
      Statement = statement;
    }

    /// <summary>
    /// 構文のTypeScript変換
    /// </summary>
    /// <param name="index">インデックス数</param>
    /// <param name="localDeclarationStatements">宣言済ローカル変数のリスト</param>
    /// <returns>TypeScriptに変換した文字列</returns>
    public abstract string ConvertStatement(int index, List<string> localDeclarationStatements);
  }
}
