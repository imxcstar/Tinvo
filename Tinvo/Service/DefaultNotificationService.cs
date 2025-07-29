using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tinvo.Abstractions;

namespace Tinvo.Service
{
    public class DefaultNotificationService : INotification
    {
        private readonly IServiceProvider _serviceProvider;

        public ConcurrentQueue<NotificationInfo> NotificationQueue { get; set; }

        public DefaultNotificationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            NotificationQueue = new ConcurrentQueue<NotificationInfo>();
        }

        public void Error(string message, Exception? exception = null)
        {
            NotificationQueue.Enqueue(new NotificationInfo()
            {
                Message = message,
                Exception = exception,
                Type = NotificationType.Error
            });
        }

        public void Info(string message)
        {
            NotificationQueue.Enqueue(new NotificationInfo()
            {
                Message = message,
                Type = NotificationType.Info
            });
        }
    }
}
