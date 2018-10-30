# これまでの実装と問題
[0.9.1](https://github.com/kazenetu/ConvertCStoTS/releases/tag/0.9.1)までの実装では
C#の解析とTypeScript変換の役割に不備がある  
* C#の解析処理でTypeScript変換まで行う
* TypeScript変換処理ではファイルの入出力のみ

# 改善案
C#の解析とTypeScript変換の役割を明確にする
* C#の解析  
  * csファイルの読み込み
  * SemanticModelと補助情報のリスト作成
* TypeScript変換
  * 「SemanticModelと補助情報のリスト」を元にTypeScript変換
  * tsファイルの書き出し

※別クラスとして作成し、ある程度完成した段階で切替を行う