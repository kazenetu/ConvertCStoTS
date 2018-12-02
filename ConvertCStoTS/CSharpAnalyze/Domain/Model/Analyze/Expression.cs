using System;

namespace CSharpAnalyze.Domain.Model.Analyze
{
  /// <summary>
  /// Expression ValueObject
  /// </summary>
  public class Expression: IEquatable<Expression>
  {
    /// <summary>
    /// 名前
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 変数型名
    /// </summary>
    public string TypeName { get; }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="name">名前</param>
    /// <param name="typeName">変数型名</param>
    public Expression(string name, string typeName)
    {
      Name = name;
      TypeName = typeName;
    }

    /// <summary>
    /// 比較
    /// </summary>
    /// <param name="other">比較対象</param>
    /// <returns>比較結果</returns>
    public bool Equals(Expression other)
    {
      if (ReferenceEquals(null, other)) return false;
      if (ReferenceEquals(this, other)) return true;
      return Name == other.Name && TypeName == other.TypeName;
    }

    /// <summary>
    /// 比較
    /// </summary>
    /// <returns>比較結果</returns>
    public override bool Equals(object obj)
    {
      if (ReferenceEquals(null, obj)) return false;
      if (ReferenceEquals(this, obj)) return true;
      if (obj.GetType() != this.GetType()) return false;
      return Equals((Expression)obj);
    }

    /// <summary>
    /// ハッシュ値取得
    /// </summary>
    /// <returns>ハッシュ値</returns>
    public override int GetHashCode()
    {
      return Name.GetHashCode(StringComparison.CurrentCulture) + TypeName.GetHashCode(StringComparison.CurrentCulture);
    }
  }
}
