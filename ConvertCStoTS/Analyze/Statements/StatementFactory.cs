using ConvertCStoTS.Analyze.Methods;
using ConvertCStoTS.Analyze.Statements;
using ConvertCStoTS.Analyze.Statements.BaseClass;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ConvertCStoTS.Analyze.Statement
{
  /// <summary>
  /// 構文クラスファクトリクラス
  /// </summary>
  public static class StatementFactory
  {
    /// <summary>
    /// ローカル変数宣言のTypeScript変換クラス取得
    /// </summary>
    /// <param name="method">メソッドインスタンス</param>
    /// <param name="statement">構文インスタンス</param>
    /// <returns>TypeScript変換クラス</returns>
    public static BaseStatement GetStatement(IMethod method, LocalDeclarationStatementSyntax statement)
    {
      return new LocalDeclarationStatement(method,statement);
    }

    /// <summary>
    /// if構文のTypeScript変換
    /// </summary>
    /// <param name="method">メソッドインスタンス</param>
    /// <param name="statement">構文インスタンス</param>
    /// <returns>TypeScript変換クラス</returns>
    public static BaseStatement GetStatement(IMethod method, IfStatementSyntax statement)
    {
      return new IfStatement(method, statement);
    }

    /// <summary>
    /// 式のTypeScript変換
    /// </summary>
    /// <param name="method">メソッドインスタンス</param>
    /// <param name="statement">構文インスタンス</param>
    /// <returns>TypeScript変換クラス</returns>
    public static BaseStatement GetStatement(IMethod method, ExpressionStatementSyntax statement)
    {
      return new ExpressionStatement(method, statement);
    }

    /// <summary>
    /// switch構文のTypeScript変換
    /// </summary>
    /// <param name="method">メソッドインスタンス</param>
    /// <param name="statement">構文インスタンス</param>
    /// <returns>TypeScript変換クラス</returns>
    public static BaseStatement GetStatement(IMethod method, SwitchStatementSyntax statement)
    {
      return new SwitchStatement(method, statement);
    }

    /// <summary>
    /// breakのTypeScript変換
    /// </summary>
    /// <param name="method">メソッドインスタンス</param>
    /// <param name="statement">構文インスタンス</param>
    /// <returns>TypeScript変換クラス</returns>
    public static BaseStatement GetStatement(IMethod method, BreakStatementSyntax statement)
    {
      return new BreakStatement(method, statement);
    }

    /// <summary>
    /// for構文のTypeScript変換
    /// </summary>
    /// <param name="method">メソッドインスタンス</param>
    /// <param name="statement">構文インスタンス</param>
    /// <returns>TypeScript変換クラス</returns>
    public static BaseStatement GetStatement(IMethod method, ForStatementSyntax statement)
    {
      return new ForStatement(method, statement);
    }

    /// <summary>
    /// foreach構文のTypeScript変換
    /// </summary>
    /// <param name="method">メソッドインスタンス</param>
    /// <param name="statement">構文インスタンス</param>
    /// <returns>TypeScript変換クラス</returns>
    public static BaseStatement GetStatement(IMethod method, ForEachStatementSyntax statement)
    {
      return new ForEachStatement(method, statement);
    }

    /// <summary>
    /// while構文のTypeScript変換
    /// </summary>
    /// <param name="method">メソッドインスタンス</param>
    /// <param name="statement">構文インスタンス</param>
    /// <returns>TypeScript変換クラス</returns>
    public static BaseStatement GetStatement(IMethod method, WhileStatementSyntax statement)
    {
      return new WhileStatement(method, statement);
    }

    /// <summary>
    /// returnのTypeScript変換
    /// </summary>
    /// <param name="method">メソッドインスタンス</param>
    /// <param name="statement">構文インスタンス</param>
    /// <returns>TypeScript変換クラス</returns>
    public static BaseStatement GetStatement(IMethod method, ReturnStatementSyntax statement)
    {
      return new ReturnStatement(method, statement);
    }

  }
}
