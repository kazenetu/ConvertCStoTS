using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace ConvertCStoTS.Analyze.Methods
{
  public interface IMethod
  {
    /// <summary>
    /// メソッド内処理を取得：エントリメソッド
    /// </summary>
    /// <param name="logic">BlockSyntaxインスタンス</param>
    /// <param name="index">インデックス数(半角スペース数)</param>
    /// <param name="parentLocalDeclarationStatements">親GetMethodTextのローカル変数リスト</param>
    string GetMethodText(BlockSyntax logic, int index = 0, List<string> parentLocalDeclarationStatements = null);

    /// <summary>
    /// メソッド内処理を取得
    /// </summary>
    /// <param name="statements">BlockSyntaxインスタンス</param>
    /// <param name="index">インデックス数(半角スペース数)</param>
    /// <param name="parentLocalDeclarationStatements">親GetMethodTextのローカル変数リスト</param>
    /// <returns>TypeScriptに変換した文字列</returns>
    string GetMethodText(SyntaxList<StatementSyntax> statements, int index = 0, List<string> parentLocalDeclarationStatements = null);
  }
}
