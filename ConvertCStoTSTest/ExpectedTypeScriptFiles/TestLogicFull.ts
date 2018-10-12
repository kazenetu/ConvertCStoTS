import { Dictionary } from './Dictionary'
import { List } from './List'


/**
 * テストクラス
 */
export class TestLogic {

  /**
   * 定数フィールド
   */
  public readonly ConstField: string = "123";

  /**
   * プロパティ例
   */
  public prop: string = "";

  public propInt: number = 50;

  /**
   * メソッド例
   */
  public Method(): void {
    let a = (arg: number): number => {
      let b = arg;
      b += arg;
      return b;
    }

    let test: number = a(10);
    if (this.prop.length > 0) {
      test = this.Method2(this.prop);
    }

    // while構文(インクリメント)
    let index = 0;
    while (index < 9) {
      this.propInt = index;

      // 条件用変数をインクリメント
      index++;
    }

    // while構文(デクリメント)
    index = 9;
    while (index > 0) {
      test = index;

      // 条件用変数をデクリメント
      index--;
    }

    let dc = new Dictionary<string, TestLogic>();
    dc.Add("aa", this);
    let value = dc.get("aa");
    for (let key in dc.Keys) {
      test = dc.get(key).propInt;
      let strTest = dc.get(key).toString();
    }
    this.prop = dc.Keys.toString();
    dc.Clear();

    let lst = new List<string>();
    lst.Add("aaa");
    for (let item in lst) {
      this.prop = item;
    }
    lst.Remove("aaa");
    lst.Clear();

    this.prop = '';
    this.prop = this.ConstField;
  }

  /**
   * メソッド例2 A
   * @param src 文字列
   * @return 戻り値
   */
  private Method20(src: string): number {
    return parseInt(src);
  }

  /**
   * メソッド例2 B
   * @param src 数値
   * @return 戻り値
   */
  private Method21(src: number): number {
    return src * 2;
  }

  /**
   * Method2 Summary Method
   * @param p0 string or number
   */
  public Method2(p0: string | number): number {
    if (typeof p0 === 'string') {
      return this.Method20(p0);
    }
    if (typeof p0 === 'number') {
      return this.Method21(p0);
    }
    return 0;
  }

  /**
   * コンストラクタ
   */
  private constructor0() {
    // ローカル変数宣言確認(型推論)
    let local = 123;

    // ローカル変数の値分岐
    if (local >= 10) {
      this.prop = local.toString();
      local = 1;
    }
    else {
      // ローカル変数宣言確認(型指定)
      let test: number = 0;
    }

    // プロパティの値分岐
    if (this.prop == "123") {
      let localString = "";
      localString = this.prop;
    }

    // ローカル変数でのswitch
    switch (local) {
      case 1:
        this.prop = "1";
        break;
      case 2:
        this.prop = "2";
        break;
      case 3:
      case 4:
        this.prop = "34";
        break;
      default:
        this.prop = "333";
        break;
    }

    // for分確認
    for (let i = 0; i < 10; i++) {
      local = i;
      this.prop = local.toString();
    }

    // for分確認(プロパティ)
    for (let i = 0; i < this.propInt; i++) {
      local = i;
      this.prop = local.toString();
    }

    // 計算代入式1
    local += local * 3;

    // 計算代入式2
    local -= local / this.propInt;
    local = local.toString().length;
  }

  /**
   * 複数コンストラクタ1
   * @param paramValue パラメータ
   */
  private constructor1(paramValue: number) {
    this.prop = paramValue.toString();
  }

  /**
   * 複数コンストラクタ2
   * @param param パラメータ
   */
  private constructor2(param: string) {
    this.prop = "コンストラクタ";
  }

  /**
   * 複数コンストラクタ3
   * @param param パラメータ1
   * @param boolValue パラメータ2
   */
  private constructor3(param: string, boolValue: boolean) {
    this.prop = "コンストラクタ";
  }

  /**
   * 複数コンストラクタ4
   * @param param パラメータ1
   * @param dateValue パラメータ2
   */
  private constructor4(param: string, dateValue: Date) {
    param = "コンストラクタ";
  }

  /**
   * constructor Summary Method
   * @param p0 null or number or string
   * @param p1 null or boolean or Date
   */
  public constructor(p0: null | number | string, p1: null | boolean | Date) {
    if (p0 === null && p1 === null) {
      this.constructor0();
    }
    if (typeof p0 === 'number' && p1 === null) {
      this.constructor1(p0);
    }
    if (typeof p0 === 'string' && p1 === null) {
      this.constructor2(p0);
    }
    if (typeof p0 === 'string' && typeof p1 === 'boolean') {
      this.constructor3(p0, p1);
    }
    if (typeof p0 === 'string' && p1 instanceof Date) {
      this.constructor4(p0, p1);
    }
  }
}
