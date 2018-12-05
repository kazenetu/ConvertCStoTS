using CSharpAnalyze.Domain.Event;
using CSharpAnalyze.Domain.Event.Analyze;
using CSharpAnalyze.Domain.Model.Analyze;
using CSharpAnalyze.Domain.Service;
using CSharpAnalyze.Infrastructure;
using System;
using System.Collections.Generic;

namespace CSharpAnalyze.ApplicationService
{
  /// <summary>
  /// C#解析アプリケーション
  /// </summary>
  /// <remarks>リクエストとレスポンスの変換アダプタなどを行う</remarks>
  public class AnalyzeApplication
  {
    public List<SemanticModelAnalyze> GetAnalyzeResult(string rootPath)
    {
      var fileRepository = new CSFileRepository();
      var analizeService = new AnalyzeService(rootPath, fileRepository);

      // HACK Domainイベントハンドラ設定
      EventContainer.Register<Analyzed>(this,(ev) =>
      {
        Console.WriteLine($"[{ev.FilePath}]");
        Console.WriteLine(ev.AnalyzeResult.ToString());
      });

      return analizeService.GetAnalyzeResult();
    }

  }
}
