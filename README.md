# ConvertCStoTS
C#のクラスをTypeScriptのクラスに変換する

# 実行環境
* .NET Core 2.0以上

# 使用方法
## ```dotnet ConvertCStoTS.dll <SourcePath> [options]```  

### ```<SourcePath>``` C#ファイルのベースディレクトリ

### [options]
|コマンド          | ファイルパス      |備考|
|:----------------|:-----------------|:-------------|  
|```-f, --file``` | ```<C#ファイルパス>```    |SourcePath以降のC#ファイルまでのパス<br>単体のCSファイルだけ変換する場合に利用|
|```--o, --out``` | ```<TSファイル出力パス>```|TypeScriptを出力する起点ディレクトリ|
|```--r, --ref``` |```<参照TSファイルパス>``` |参照解決できない場合のTSファイルのパス|
|```--h, --help```|                         | ヘルプページを表示する|

# 実行例
## 単体C#ファイルのTypeScript変換
* C#ファイルのベースディレクトリ:**TargetSources**
* TypeScriptを出力する起点ディレクトリ:**TargetSources/dist**
* SourcePath以降のC#ファイルまでのパス:**Response/OrderList/SearchResponse.cs**
* 参照TSファイルパス:**base**  
  (TargetSources/dist/base)  
  ※デフォルト設定

```dotnet ConvertCStoTS.dll TargetSources --out TargetSources/dist --file Response/OrderList/SearchResponse.cs```  
または  
```dotnet ConvertCStoTS.dll TargetSources -o TargetSources/dist -f Response/OrderList/SearchResponse.cs```


## C#ファイルのTypeScript一括変換
* C#ファイルのベースディレクトリ:**TargetSources**
* TypeScriptを出力する起点ディレクトリ:**TargetSources/dist**
* 参照TSファイルパス:**baseclass**  
  (TargetSources/dist/baseclass)

```dotnet ConvertCStoTS.dll TargetSources --out TargetSources/dist --ref baseclass```  
または  
```dotnet ConvertCStoTS.dll TargetSources -o TargetSources/dist -r baseclass```
