using System;

namespace Arakuma.Threading {
    /// <summary>
    /// 可取消的任务接口
    /// </summary>
    public interface ICancelableTask {
        event Action TaskCompleted; 
        void Cancel();
    }
}
