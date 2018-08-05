using System;

namespace DataTransferObjects
{
  /// <summary>
  /// テストクラス
  /// </summary>
  public class TestLogic
  {
    /// <summary>
    /// プロパティ例
    /// </summary>
    /// <value></value>
    public string prop { set; get; } = "";

    /// <summary>
    /// コンストラクタ
    /// /// </summary>
    public TestLogic()
    {
      var local = 123;
      if (local >= 10)
      {
        prop = local.ToString();
        local = 1;
      }
      else
      {
        int test = 0;
      }

      switch (local)
      {
        case 1:
          prop = "1";
          break;
        case 2:
          prop = "2";
          break;
        case 3:
        case 4:
          prop = "34";
          break;
        default:
          prop = "333";
          break;
      }

      for (var i = 0; i < 10; i++)
      {
        local = i;
        prop = local.ToString();
      }
    }

    /// <summary>
    /// メソッド例
    /// </summary>
    public void Method()
    {
      int test = 0;
      if (prop.Length > 0)
      {
        test = int.Parse(prop);
        test = 456;
      }
    }
  }
}
