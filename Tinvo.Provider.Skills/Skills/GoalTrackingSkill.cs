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
    public class GoalTrackingSkill
    {
        private readonly ILogger _logger;

        private static string _currentGoal;
        private static string _currentGoalProgressValue;
        private static string _currentGoalProgressDescription;

        public GoalTrackingSkill()
        {
            _logger = Log.ForContext<GoalTrackingSkill>();
        }

        [Description("记忆用户当前目标")]
        public Task<IAIChatHandleMessage> SetGoalAsync(
            [Description("目标内容"), Required] string goalContent)
        {
            _logger.Debug("GoalTrackingSkill: SetGoalAsync - {Content}", goalContent);

            _currentGoal = goalContent;
            _currentGoalProgressValue = "0";
            _currentGoalProgressDescription = "";

            return Task.FromResult<IAIChatHandleMessage>(new AIProviderHandleTextMessageResponse()
            {
                Message = $"已记住当前目标：{goalContent}"
            });
        }

        [Description("追加记录当前用户目标进度")]
        public Task<IAIChatHandleMessage> AddGoalProgressAsync(
            [Description("进度百分比(0-100)"), Required] string progressValue,
            [Description("已执行的进度详细描述"), Required] string progressDescription)
        {
            _logger.Debug("GoalTrackingSkill: AddGoalProgressAsync - {Value}% {Description}", progressValue, progressDescription);

            _currentGoalProgressValue = progressValue;
            _currentGoalProgressDescription = $"{_currentGoalProgressDescription}\r\n[{DateTime.Now}] {progressDescription}";

            return Task.FromResult<IAIChatHandleMessage>(new AIProviderHandleTextMessageResponse()
            {
                Message = $"当前目标：{_currentGoal}\r\n进度：{_currentGoalProgressValue}%\r\n进度描述：\r\n{_currentGoalProgressDescription}"
            });
        }

        [Description("查询当前用户目标进度")]
        public Task<IAIChatHandleMessage> GetGoalProgressAsync()
        {
            _logger.Debug("GoalTrackingSkill: GetGoalProgressAsync");

            if (string.IsNullOrEmpty(_currentGoalProgressValue) || string.IsNullOrEmpty(_currentGoalProgressDescription))
            {
                return Task.FromResult<IAIChatHandleMessage>(new AIProviderHandleTextMessageResponse()
                {
                    Message = "当前目标还没有记录进度。"
                });
            }

            return Task.FromResult<IAIChatHandleMessage>(new AIProviderHandleTextMessageResponse()
            {
                Message = $"当前目标：{_currentGoal}\r\n进度：{_currentGoalProgressValue}%\r\n进度描述：\r\n{_currentGoalProgressDescription}"
            });
        }
    }
}
