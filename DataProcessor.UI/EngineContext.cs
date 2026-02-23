using System;
using System.Collections.Generic;
using DataProcessor.Engine;

namespace DataProcessor
{
    /// <summary>
    /// Application-wide singleton providing access to the Engine and events for
    /// cross-form communication. Named EngineContext to avoid collision with System.AppContext.
    /// </summary>
    public static class EngineContext
    {
        public static Engine.Engine Engine { get; } = new Engine.Engine();
        public static string CurrentScript { get; set; } = "default";

        public static event EventHandler<ExecutionCompletedEventArgs>? ExecutionCompleted;

        public static void RaiseExecutionCompleted(List<string> log)
        {
            ExecutionCompleted?.Invoke(null, new ExecutionCompletedEventArgs
            {
                LogEntries = log,
                DataStoreNames = Engine.GetDataStoreNames()
            });
        }
    }

    public class ExecutionCompletedEventArgs : EventArgs
    {
        public List<string> LogEntries { get; init; } = new();
        public IReadOnlyList<string> DataStoreNames { get; init; } = Array.Empty<string>();
    }
}
