export class TableBase {
  public GetType(): TableBase {
      return this;
  }

  public GetProperty(paramName: string): PropertyInfo {
      return new PropertyInfo(this, paramName);
  }
}

interface IPropertyInfo {
    [key: string]: object;
}

export class PropertyInfo {

  public GetValue(): object {
      let instance = this.instance as IPropertyInfo;
      return instance[this.propertyName];
  }

  public SetValue(value: object) {
    let instance = this.instance as IPropertyInfo;
    instance[this.propertyName] = value;
  }

  public get PropertyName() {
      return this.propertyName;
  }

  constructor(private instance: object, private propertyName: string) {

  }
}

export class ResponseBase<T>
{
  public ResponseData: null | T;
  constructor(result: null | Results, errorMessage: null | string, responseData: null | T) {
    this.ResponseData = responseData;
  }
}

export class Results {
  public static OK: string = "OK";
  public static NG: string = "NG";
}

export class RequestBase {

}