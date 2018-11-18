using System;

namespace ConvertCStoTS.CSharpAnalyze.Domain.Model.Analyze
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
    /// Type
    /// </summary>
    public Type ExpressionType { get; }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="name">名前</param>
    /// <param name="expressionType">Type</param>
    public Expression(string name, Type expressionType)
    {
      Name = name;
      ExpressionType = expressionType;
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
      return Name == other.Name && ExpressionType.Name == other.ExpressionType.Name;
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
      return Name.GetHashCode(StringComparison.CurrentCulture) + ExpressionType.GetHashCode();
    }
  }
}
