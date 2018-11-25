﻿using System.Collections.Generic;
using System.Text;

namespace ConvertCStoTS.CSharpAnalyze.Domain.Model.Analyze.Items
{
  /// <summary>
  /// アイテム：ファイルルート
  /// </summary>
  public class ItemRoot : ISemanticModelAnalyzeItem
  {
    /// <summary>
    /// 子メンバ
    /// </summary>
    public List<ISemanticModelAnalyzeItem> Members { get; } = new List<ISemanticModelAnalyzeItem>();

    /// <summary>
    /// アイテム種別
    /// </summary>
    public ItemTypes ItemType { get; }

    /// <summary>
    /// 名前
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 修飾子リスト
    /// </summary>
    public List<string> Modifiers { get; } = new List<string>();

    /// <summary>
    /// コメント
    /// </summary>
    public List<string> Comments { get; } = new List<string>();

    /// <summary>
    /// コンストラクタ
    /// </summary>
    public ItemRoot()
    {
      ItemType = ItemTypes.Root;
    }

    /// <summary>
    /// 文字列取得
    /// </summary>
    /// <param name="index">前スペース数</param>
    /// <returns>文字列</returns>
    public string ToString(int index = 0)
    {
      var result = new StringBuilder();
      Members.ForEach(member => result.AppendLine(member.ToString(index)));
      return result.ToString();
    }
  }
}