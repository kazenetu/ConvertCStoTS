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

    public int propInt { set; get; } = 50;

    /// <summary>
    /// メソッド例
    /// </summary>
    public void Method()
    {
      int test = 0;
      if (prop.Length > 0)
      {
        test = this.Method2(prop);
      }
    }

    /// <summary>
    /// メソッド例2
    /// </summary>
    public int Method2(string src)
    {
      return int.Parse(src);
    }

    /// <summary>
    /// コンストラクタ
    /// /// </summary>
    public TestLogic()
    {
      // ローカル変数宣言確認(型推論)
      var local = 123;

      // ローカル変数の値分岐
      if (local >= 10)
      {
        prop = local.ToString();
        local = 1;
      }
      else
      {
        // ローカル変数宣言確認(型指定)
        int test = 0;
      }

      // プロパティの値分岐
      if(prop == "123"){
        var localString = "";
        localString = prop;
      }

      // ローカル変数でのswitch
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

      // for分確認
      for (var i = 0; i < 10; i++)
      {
        local = i;
        prop = local.ToString();
      }

      // for分確認(プロパティ)
      for (var i = 0; i < propInt; i++)
      {
        local = i;
        prop = local.ToString();
      }

      // 計算代入式1
      local += local *3;

      // 計算代入式2
      local -= local /propInt;
      local = local.ToString().Length;
    }
 
    /// <summary>
    /// 複数コンストラクタ1
    /// </summary>
    /// <value></value>
    public TestLogic(int paramValue)
    {
      prop = paramValue.ToString();
    }

    /// <summary>
    /// 複数コンストラクタ2
    /// </summary>
    /// <value></value>
    public TestLogic(string param)
    {
      prop = "コンストラクタ";
    }

    /// <summary>
    /// 複数コンストラクタ3
    /// </summary>
    /// <value></value>
    public TestLogic(string param,bool boolValue)
    {
      prop = "コンストラクタ";
    }

    /// <summary>
    /// 複数コンストラクタ4
    /// </summary>
    /// <value></value>
    public TestLogic(string param,DateTime dateValue)
    {
      param = "コンストラクタ";
    }

  }
}
