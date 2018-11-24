using System.Collections.Generic;

namespace ConvertCStoTS.CSharpAnalyze.Domain.Model.Analyze.Items
{
  /// <summary>
  /// C#ファイル解析結果 詳細情報
  /// </summary>
  public interface ISemanticModelAnalyzeItem
  {
    /// <summary>
    /// 子メンバ
    /// </summary>
    List<ISemanticModelAnalyzeItem> Members { get; }

    /// <summary>
    /// アイテム種別
    /// </summary>
    ItemTypes ItemType { get; }

    /// <summary>
    /// 名前
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 修飾子リスト
    /// </summary>
    List<string> Modifiers { get; }

    /// <summary>
    /// コメント
    /// </summary>
    List<string> Comments { get; }

    /// <summary>
    /// 文字列取得
    /// </summary>
    /// <param name="index">前スペース数</param>
    /// <returns>文字列</returns>
    string ToString(int index = 0);
  }

  /// <summary>
  /// アイテム種別
  /// </summary>
  public enum ItemTypes
  {
    Class,
    Field,
    Property,
    Method,
    MethodStatement
  }
}
