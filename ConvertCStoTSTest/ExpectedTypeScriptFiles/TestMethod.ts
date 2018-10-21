import { OtherClass } from './otherclass'
import { OtherClass_InnerClass } from './otherclass'


/**
 * メソッドテストクラス
 */
export class TestMethod {

  /**
   * 定数フィールド
   */
  public readonly ConstField: string = "123";

  /**
   * プロパティ例
   */
  public prop: string = "";

  public propInt: number = 50;

  public static StaticMethod(): string {
    return "ccc";
  }

  /**
   * メソッド例
   */
  public Method(): void {
    // this.prop = TestMethod.StaticMethod()
    this.prop = TestMethod.StaticMethod();
    // this.prop = TestLogic.StaticMethod()
    this.prop = TestMethod.StaticMethod();

    // this.prop = TestMethod.InnerClass.StaticMethod()
    this.prop = TestMethod_InnerClass.StaticMethod();
    // this.prop = TestMethod.InnerClass.StaticMethod()
    this.prop = TestMethod_InnerClass.StaticMethod();
    // this.prop = TestMethod.InnerClass.StaticMethodArg("test")
    this.prop = TestMethod_InnerClass.StaticMethodArg("test");
    // this.prop = TestMethod.InnerClass.StaticMethodArg(propInt.toString())
    this.prop = TestMethod_InnerClass.StaticMethodArg(this.propInt.toString());

    // this.prop = OtherClass.StaticMethod()
    this.prop = OtherClass.StaticMethod();

    // this.prop = OtherClass.InnerClass.StaticMethod()
    this.prop = OtherClass_InnerClass.StaticMethod();
    // this.prop = OtherClassc.InnerClass.StaticMethodArg("test")
    this.prop = OtherClass_InnerClass.StaticMethodArg("test");
    // this.prop = OtherClass.InnerClass.StaticMethodArg(propInt.toString())
    this.prop = OtherClass_InnerClass.StaticMethodArg(this.propInt.toString());
  }
}

export class TestMethod_InnerClass {

  public static StaticField: string = "789";

  public static StaticMethod(): string {
    return "bbb";
  }

  public static StaticMethodArg(name: string): string {
    return "hey!" + name;
  }
}
