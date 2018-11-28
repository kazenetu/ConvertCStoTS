using Microsoft.CodeAnalysis;
using System.Linq;

namespace ConvertCStoTS.CSharpAnalyze.Domain.Model.Analyze.Items
{
  /// <summary>
  /// Item系クラスのスーパークラス
  /// </summary>
  public abstract class AbstractItem
  {
    /// <summary>
    /// シンボルインターフェースの型の名前を返す
    /// </summary>
    /// <param name="target">対象シンボルインターフェース</param>
    /// <returns>型名・存在しない場合はstring.Empty</returns>
    protected string GetSymbolTypeName(ISymbol target)
    {
      var symbol = target as INamedTypeSymbol;
      if (symbol == null)
      {
        return string.Empty;
      }

      if (symbol.IsGenericType)
      {
        return "GenericClass";
      }

      if (symbol.SpecialType != SpecialType.None)
      {
        return symbol.Name;
      }

      return symbol.TypeKind.ToString();
    }

    /// <summary>
    /// 外部ファイル参照イベント発行
    /// </summary>
    /// <param name="targetNode">対象Node</param>
    /// <param name="targetSymbol">比較対象のSymbol</param>
    protected void RaiseOtherFileReferenced(SyntaxNode targetNode,ISymbol targetSymbol)
    {
      if (!targetSymbol.DeclaringSyntaxReferences.Any())
      {
        // TODO ファイルパスなしでイベント送信
      }

      var targetNodeFilePath = targetNode.SyntaxTree.FilePath;
      var referenceFilePaths = targetSymbol.DeclaringSyntaxReferences.
                                Where(item => item.SyntaxTree.FilePath != targetNodeFilePath).
                                Select(item => item.SyntaxTree.FilePath);
      foreach (var referenceFilePath in referenceFilePaths)
      {
        // TODO ファイルパスありでイベント送信
      }
    }
  }
}
