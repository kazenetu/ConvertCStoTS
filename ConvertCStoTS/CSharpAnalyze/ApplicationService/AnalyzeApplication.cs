using CSharpAnalyze.Domain.Model;
using CSharpAnalyze.Domain.Model.Analyze;
using CSharpAnalyze.Domain.Service;
using CSharpAnalyze.Infrastructure;
using System.Collections.Generic;
using System.Linq;

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

      return analizeService.GetAnalyzeResult();
    }

  }
}
