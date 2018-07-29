# ConvertCStoTS
C#のクラスをTypeScriptのクラスに変換する

# 実行環境
* .NET Core 2.0以上

# 使用方法
## ```dotnet ConvertCStoTS <SourcePath> [options]```  

### ```<SourcePath>``` C#ファイルのベースディレクトリ

### [options]
|コマンド          | ファイルパス      |備考|
|:----------------|:-----------------|:-------------|  
|```-f, --file``` | ```<C#ファイルパス>```    |SourcePath以降のC#ファイルまでのパス<br>単体のCSファイルだけ変換する場合に利用|
|```--o, --out``` | ```<TSファイル出力パス>```|TypeScriptを出力する起点ディレクトリ|
|```--r, --ref``` |```<参照TSファイルパス>``` |参照解決できない場合のTSファイルのパス|
|```--h, --help```|                         | ヘルプページを表示する|
