using System;

namespace Arakuma.NetworkUtil {
    /// <summary>
    /// Task interface. Task should always be canceled
    /// </summary>
    internal interface ICancelableTask {
        void Cancel();
    }
}
