using System;

namespace Kesco.Lib.Log
{
    /// <summary>
    /// Заглушка
    /// </summary>
    internal sealed class MockLocalLogger : ILocalLogger
    {
        static MockLocalLogger()
        {
            MockDurationMetter = new MockDurationMetter();
        }


        private static readonly MockDurationMetter MockDurationMetter;


        public void LogMessage(string message)
        {
         
        }

        public void LogWarning(string message)
        {
       
        }

        public void Error(string message)
        {
       
        }

        public void Exception(string message, Exception ex)
        {
      
        }

        public void EnterMethod(object instance, string methodName)
        {
        
        }

        public void LeaveMethod(object instance, string methodName)
        {
    
        }

        public void StackTrace()
        {
         
        }

        public void ObjectProperties(object instance)
        {
        
        }

        public IDurationMetter GetDurationMetter(string message)
        {
            return MockDurationMetter;
        }
    }
}
