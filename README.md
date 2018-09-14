# ConvertCStoTS
C#のクラスをTypeScriptのクラスに変換する

# 実行環境
* .NET Core SDK 2.0以上

# 前準備
1. リポジトリのルートディレクトリで下記を実行  
```dotnet publish ConvertCStoTS -c Release -o ../publish```  
※ルートディレクトリ直下に「publish」フォルダが作成される

# 使用方法
## ```dotnet publish/ConvertCStoTS.dll <SourcePath> [options]```  

### ```<SourcePath>``` C#ファイルのベースディレクトリ

### [options]
|コマンド          | ファイルパス      |備考|
|:----------------|:-----------------|:-------------|  
|```-f, --file``` | ```<C#ファイルパス>```    |SourcePath以降のC#ファイルまでのパス<br>単体のCSファイルだけ変換する場合に利用|
|```--o, --out``` | ```<TSファイル出力パス>```|TypeScriptを出力する起点ディレクトリ|
|```--r, --ref``` |```<参照TSファイルパス>``` |参照解決できない場合のTSファイルのパス|
|```--no_method_output``` |  |コンストラクタ・メソッドは出力対象外|
|```--h, --help```|                         | ヘルプページを表示する|

# 実行例
## 単体C#ファイルのTypeScript変換
* C#ファイルのベースディレクトリ:**TargetSources**
* TypeScriptを出力する起点ディレクトリ:**TargetSources/dist**
* SourcePath以降のC#ファイルまでのパス:**Response/OrderList/SearchResponse.cs**
* 参照TSファイルパス:**base**  
  (TargetSources/dist/base)  
  ※デフォルト設定
* コンストラクタ・メソッド出力:行う

```dotnet publish/ConvertCStoTS.dll TargetSources --out TargetSources/dist --file Response/OrderList/SearchResponse.cs```  
または  
```dotnet publish/ConvertCStoTS.dll TargetSources -o TargetSources/dist -f Response/OrderList/SearchResponse.cs```


## C#ファイルのTypeScript一括変換
* C#ファイルのベースディレクトリ:**TargetSources**
* TypeScriptを出力する起点ディレクトリ:**TargetSources/dist**
* 参照TSファイルパス:**baseclass**  
  (TargetSources/dist/baseclass)
* コンストラクタ・メソッド出力:行う

```dotnet publish/ConvertCStoTS.dll TargetSources --out TargetSources/dist --ref baseclass```  
または  
```dotnet publish/ConvertCStoTS.dll TargetSources -o TargetSources/dist -r baseclass```

## C#ファイルのTypeScript一括変換：プロパティのみ
* C#ファイルのベースディレクトリ:**TargetSources**
* TypeScriptを出力する起点ディレクトリ:**TargetSources/dist**
* 参照TSファイルパス:**baseclass**  
  (TargetSources/dist/baseclass)
* コンストラクタ・メソッド出力:**行わない**

```dotnet publish/ConvertCStoTS.dll TargetSources --out TargetSources/dist --ref baseclass --no_method_output```  
または  
```dotnet publish/ConvertCStoTS.dll TargetSources -o TargetSources/dist -r baseclass --no_method_output```

# 未実装
- [X] 複数のコンストラクタのTS変換対応
- [X] メソッドのTS変換
- [X] ヘッダーコメントの出力
- [X] List,Dictionaryのエミュレート
- [X] メソッド・コンストラクタを変換しないコマンドの追加
- [X] 内部処理：whileのTS変換
- [X] 内部処理：foreachのTS変換

