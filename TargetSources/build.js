const path = require('path');
const fs = require('fs');

/**
 * jsファイルのリストを出力
 * @param {string} targetDir 
 */
function getFiles(targetDir, result) {
  const directoryItems = fs.readdirSync(targetDir);

  directoryItems.forEach(item => {
    let itemPath = `${targetDir}/${item}`;
    const stat = fs.statSync(itemPath);

    // ディレクトリの場合は中身を検索
    if (stat.isDirectory()) {
      getFiles(itemPath, result);
      return;
    }

    // 拡張子がjsのファイルをリストに追加
    if (/.*\.js$/.test(itemPath)) {
      modifyImports(itemPath);
      result.push(itemPath);
    }
  });
}

/**
 * jsファイルのimport定義のパスにjs拡張子を設定
 * @param {string} jsFilePath 
 */
function modifyImports(jsFilePath){
  let text = fs.readFileSync(jsFilePath, 'utf8');
  text = text.replace(/import (.*) from '(.*)';/g, "import $1 from '$2.js';");
  fs.writeFileSync(jsFilePath, text);
}

const childProcess = require('child_process');
childProcess.exec('tsc', (error, stdout, stderr) => {
  if(error) {
    // エラー時は標準エラー出力を表示して終了
    console.log(stderr);
    return;
  }
  else {
    // 成功時
    let list = new Array();
    const targetDir = './dist';
    getFiles(targetDir, list);
    
    console.log(list);
  }
});