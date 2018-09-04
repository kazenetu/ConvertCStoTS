export class TableBase {
  public GetType(): TableBase {
      return this;
  }

  public GetProperty(paramName: string): PropertyInfo {
      return new PropertyInfo(this, paramName);
  }
}

export class PropertyInfo {

  public GetValue(): object {
      return this.instance[this.propertyName];
  }

  public SetValue(value: object) {
      this.instance[this.propertyName] = value;
  }

  public get PropertyName() {
      return this.propertyName;
  }

  constructor(private instance: object, private propertyName: string) {

  }
}

export class ResponseBase<T>
{
  public ResponseData: T;
  constructor(result?: Results, errorMessage?: string, responseData?: T) {

  }
}

export class Results {
  public static OK: string = "OK";
  public static NG: string = "NG";
}

export class RequestBase {

}