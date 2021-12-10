namespace Alteruna.Trinity
{
    /// <summary>
    /// Class <c>UnityLog</c> is responsible for logging <c>Trinity</c> messages and events. 
    /// </summary>
    public class UnityLog : LogBase 
    {
        public override void Present(Severity severity, string message)
        {
            switch (severity)
            {
                case Severity.Debug:
                    {
                        UnityEngine.Debug.Log(message);
                        break;
                    }
                case Severity.Error:
                    {
                        UnityEngine.Debug.LogError(message);
                        break;
                    }
                case Severity.Warning:
                    {
                        UnityEngine.Debug.LogWarning(message);
                        break;
                    }
                case Severity.Info:
                    {
                        UnityEngine.Debug.Log(message);
                        break;
                    }
                case Severity.None:
                    {
                        UnityEngine.Debug.Log(message);
                        break;
                    }

                default:
                    break;
            }
        }
    }
}
