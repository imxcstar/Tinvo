using Metalama.Extensions.DependencyInjection;
using Metalama.Extensions.Multicast;
using Metalama.Framework.Aspects;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tinvo.Abstractions;

[assembly:
    AspectOrder(AspectOrderDirection.RunTime, typeof(ExceptionNotificationAttribute), typeof(DependencyAttribute))]

namespace Tinvo.Abstractions
{
    public interface INotification
    {
        public ConcurrentQueue<NotificationInfo> NotificationQueue { get; set; }

        public void Info(string message);
        public void Error(string message, Exception? exception = null);
    }

    public class NotificationInfo
    {
        public required string Message { get; set; }

        public Exception? Exception { get; set; }

        public NotificationType Type { get; set; }
    }

    public enum NotificationType
    {
        Info,
        Error
    }

    public class ExceptionNotificationAttribute : OverrideMethodMulticastAspect
    {
        [IntroduceDependency]
        private readonly INotification _notification;

        public override dynamic? OverrideMethod()
        {
            try
            {
                return meta.Proceed();
            }
            catch (Exception ex)
            {
                _notification.Error(ex.Message, ex);
                return default;
            }
        }
    }
}
