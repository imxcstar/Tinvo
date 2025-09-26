using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tinvo.Abstractions.AIScheduler;

namespace Tinvo.Provider.Skills.Skills
{
    public class TaskSchedulerSkill
    {
        private readonly ILogger _logger;

        private enum TaskStatus
        {
            未开始,
            进行中,
            已完成
        }

        private class TaskItem
        {
            public string Description { get; set; }
            public TaskStatus Status { get; set; } = TaskStatus.未开始;
            public List<string> Logs { get; set; } = new List<string>();
            public string Output { get; set; }  // ✅ 任务产出
        }

        private static readonly Dictionary<string, TaskItem> _tasks
            = new Dictionary<string, TaskItem>();

        public TaskSchedulerSkill()
        {
            _logger = Log.ForContext<TaskSchedulerSkill>();
        }

        [Description("添加一个新的任务计划")]
        public Task<IAIChatHandleMessage> AddTaskAsync(
            [Description("任务名称"), Required] string taskName,
            [Description("任务描述"), Required] string taskDescription)
        {
            _logger.Debug("TaskSchedulerSkill: AddTaskAsync - {Name}, {Description}", taskName, taskDescription);

            if (_tasks.ContainsKey(taskName))
            {
                return Task.FromResult<IAIChatHandleMessage>(new AIProviderHandleTextMessageResponse()
                {
                    Message = $"任务 '{taskName}' 已存在。"
                });
            }

            _tasks[taskName] = new TaskItem
            {
                Description = taskDescription,
                Status = TaskStatus.未开始
            };

            return Task.FromResult<IAIChatHandleMessage>(new AIProviderHandleTextMessageResponse()
            {
                Message = $"已添加任务：{taskName}\r\n描述：{taskDescription}\r\n状态：未开始"
            });
        }

        [Description("将任务标记为进行中")]
        public Task<IAIChatHandleMessage> StartTaskAsync(
            [Description("任务名称"), Required] string taskName,
            [Description("备注（可选）")] string note = "")
        {
            _logger.Debug("TaskSchedulerSkill: StartTaskAsync - {Name}, {Note}", taskName, note);

            if (!_tasks.ContainsKey(taskName))
            {
                return Task.FromResult<IAIChatHandleMessage>(new AIProviderHandleTextMessageResponse()
                {
                    Message = $"未找到任务 '{taskName}'。"
                });
            }

            var task = _tasks[taskName];
            task.Status = TaskStatus.进行中;
            if (!string.IsNullOrEmpty(note))
                task.Logs.Add($"[{DateTime.Now}] 开始任务：{note}");

            return Task.FromResult<IAIChatHandleMessage>(new AIProviderHandleTextMessageResponse()
            {
                Message = $"任务 '{taskName}' 已标记为进行中。"
            });
        }

        [Description("为任务添加产出，并自动标记为已完成")]
        public Task<IAIChatHandleMessage> AddTaskOutputAsync(
            [Description("任务名称"), Required] string taskName,
            [Description("任务产出"), Required] string output)
        {
            _logger.Debug("TaskSchedulerSkill: AddTaskOutputAsync - {Name}, {Output}", taskName, output);

            if (!_tasks.ContainsKey(taskName))
            {
                return Task.FromResult<IAIChatHandleMessage>(new AIProviderHandleTextMessageResponse()
                {
                    Message = $"未找到任务 '{taskName}'。"
                });
            }

            var task = _tasks[taskName];
            task.Output = output;
            task.Status = TaskStatus.已完成; // ✅ 在添加产出时自动完成
            task.Logs.Add($"[{DateTime.Now}] 添加任务产出。");

            return Task.FromResult<IAIChatHandleMessage>(new AIProviderHandleTextMessageResponse()
            {
                Message = $"任务 '{taskName}' 已完成，产出：{output}"
            });
        }

        [Description("查询所有任务的状态（含产出）")]
        public Task<IAIChatHandleMessage> GetTasksAsync()
        {
            _logger.Debug("TaskSchedulerSkill: GetTasksAsync");

            if (!_tasks.Any())
            {
                return Task.FromResult<IAIChatHandleMessage>(new AIProviderHandleTextMessageResponse()
                {
                    Message = "当前还没有任务。"
                });
            }

            var sb = new StringBuilder();
            foreach (var kvp in _tasks)
            {
                sb.AppendLine($"任务：{kvp.Key}");
                sb.AppendLine($"描述：{kvp.Value.Description}");
                sb.AppendLine($"状态：{kvp.Value.Status}");
                if (!string.IsNullOrEmpty(kvp.Value.Output))
                {
                    sb.AppendLine($"产出：{kvp.Value.Output}");
                }
                if (kvp.Value.Logs.Any())
                {
                    sb.AppendLine("日志：");
                    foreach (var log in kvp.Value.Logs)
                    {
                        sb.AppendLine($"- {log}");
                    }
                }
                sb.AppendLine("--------------------");
            }

            return Task.FromResult<IAIChatHandleMessage>(new AIProviderHandleTextMessageResponse()
            {
                Message = sb.ToString()
            });
        }

        [Description("删除一个任务")]
        public Task<IAIChatHandleMessage> RemoveTaskAsync(
            [Description("任务名称"), Required] string taskName)
        {
            _logger.Debug("TaskSchedulerSkill: RemoveTaskAsync - {Name}", taskName);

            if (_tasks.Remove(taskName))
            {
                return Task.FromResult<IAIChatHandleMessage>(new AIProviderHandleTextMessageResponse()
                {
                    Message = $"任务 '{taskName}' 已删除。"
                });
            }

            return Task.FromResult<IAIChatHandleMessage>(new AIProviderHandleTextMessageResponse()
            {
                Message = $"未找到任务 '{taskName}'。"
            });
        }
    }
}
