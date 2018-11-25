using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConvertCStoTS.CSharpAnalyze.Domain.Event
{
  /// <summary>
  /// イベントコンテナ
  /// </summary>
  public static class EventContainer
  {
    /// <summary>
    /// イベントハンドラリスト
    /// </summary>
    private static List<Delegate> Handles = new List<Delegate>();

    /// <summary>
    /// イベントの登録
    /// </summary>
    /// <param name="callback">イベントハンドラ</param>
    public static void Register<T>(Action<T> callback) where T : IEvent
    {
      Handles.Add(callback);
    }

    /// <summary>
    /// イベント発行
    /// </summary>
    /// <param name="args">発行イベント</param>
    public static void Raise<T>(T args) 
    {
      var targets = Handles.Where(handle => handle is Action<T>);
      foreach(var target in targets)
      {
        ((Action<T>) target)(args);
      }
    }
  }
}
