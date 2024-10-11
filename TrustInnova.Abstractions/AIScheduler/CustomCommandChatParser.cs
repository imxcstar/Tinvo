using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace TrustInnova.Abstractions.AIScheduler
{
    public enum HandleRetMsgState
    {
        None,
        Mark,
        Command,
        CommandContent
    }

    public class CustomCommandChatParser : IAIChatParser
    {
        private List<string> aiCommands;
        private Dictionary<string, string> aiCommandsLowerMap;

        private readonly StringBuilder rawDataBuilder = new();
        private readonly StringBuilder retDataBuilder = new();
        private readonly StringBuilder commandBuilder = new();
        private HandleRetMsgState handleState = HandleRetMsgState.Mark;
        private bool isvmEnd = false;
        private string contentCommand = "";
        private string contentCommandValue = "";
        private string defaultMsg = "";
        private bool isBrowser = false;

        private bool TryGetLowerCommand(string name, out string value)
        {
            foreach (var item in aiCommands)
            {
                if (item.ToLower() == name.ToLower())
                {
                    value = item;
                    return true;
                }
            }
            value = "";
            return false;
        }

        public CustomCommandChatParser()
        {
            isBrowser = OperatingSystem.IsBrowser();
        }

        public void ResetHandleState()
        {
            handleState = HandleRetMsgState.Mark;
        }

        public async IAsyncEnumerable<IAIChatHandleResponse> Handle(object msg, IFunctionManager? functionManager)
        {
            if (msg is not string smsg)
                yield break;

            aiCommands = functionManager == null ? [] : functionManager.FunctionInfos.Select(x => x.Name).OrderByDescending(x => x.Length).ToList();
            aiCommandsLowerMap = functionManager == null ? [] : aiCommands.ToDictionary(x => x.ToLower(), x => x);

            retDataBuilder.Append(smsg);
            var retData = retDataBuilder.ToString();
            foreach (var commandChar in retData)
            {
                rawDataBuilder.Append(commandChar);
                commandBuilder.Append(commandChar);
                retDataBuilder.Remove(0, 1);
            mark:
                switch (handleState)
                {
                    case HandleRetMsgState.Mark:
                        var mark = commandBuilder.ToString().ToLower().Replace("\r", "").Replace("\n", "").Replace(" ", "");
                        switch (mark)
                        {
                            case "#start":
                                handleState = HandleRetMsgState.Command;
                                commandBuilder.Clear();
                                contentCommand = "";
                                contentCommandValue = "";
                                isvmEnd = false;
                                break;
                            case "#end":
                                if (TryGetLowerCommand(contentCommand, out var ocommand))
                                {
                                    yield return new AIProviderHandleFunctionCallResponse()
                                    {
                                        FunctionManager = functionManager!,
                                        FunctionName = ocommand,
                                        Arguments = null
                                    };
                                }
                                commandBuilder.Clear();
                                contentCommand = "";
                                contentCommandValue = "";
                                if (isvmEnd)
                                {
                                    isvmEnd = false;
                                    handleState = HandleRetMsgState.Command;
                                }
                                break;
                            default:
                                if (mark.Length > 6)
                                {
                                    handleState = HandleRetMsgState.None;
                                    if (string.IsNullOrWhiteSpace(defaultMsg))
                                        mark = mark.TrimStart('\r').TrimStart('\n').TrimStart(' ');
                                    if (!string.IsNullOrWhiteSpace(mark))
                                    {
                                        defaultMsg += mark;
                                        yield return new AIProviderHandleTextMessageResponse()
                                        {
                                            Message = mark
                                        };
                                    }
                                    commandBuilder.Clear();
                                }
                                break;
                        }
                        break;
                    case HandleRetMsgState.Command:
                        var rawCommand = commandBuilder.ToString();
                        var command = rawCommand.ToLower().Replace("\r", "").Replace("\n", "").Replace(" ", "");
                        if (command == "#")
                        {
                            handleState = HandleRetMsgState.Mark;
                        }
                        else if (TryGetLowerCommand(command, out var ocommand))
                        {
                            contentCommand = command;
                            commandBuilder.Clear();
                            contentCommandValue = "";
                            handleState = HandleRetMsgState.CommandContent;
                            yield return new AIProviderHandleFunctionStartResponse()
                            {
                                FunctionManager = functionManager!,
                                FunctionName = ocommand
                            };
                        }
                        else
                        {
                            var isCommand = false;
                            foreach (var aiCommand in aiCommands)
                            {
                                var laiCommand = aiCommand.ToLower();
                                if (laiCommand.StartsWith(command))
                                {
                                    isCommand = true;
                                    break;
                                }
                            }
                            if (!isCommand)
                            {
                                if (string.IsNullOrWhiteSpace(defaultMsg))
                                    rawCommand = rawCommand.TrimStart('\r').TrimStart('\n').TrimStart(' ');
                                if (!string.IsNullOrWhiteSpace(rawCommand))
                                {
                                    defaultMsg += rawCommand;
                                    yield return new AIProviderHandleTextMessageResponse()
                                    {
                                        Message = rawCommand
                                    };
                                }
                                commandBuilder.Clear();
                            }
                        }
                        break;
                    case HandleRetMsgState.CommandContent:
                        if (commandChar == '\r' || commandChar == '\n')
                        {
                            commandBuilder.Clear();
                            handleState = HandleRetMsgState.Mark;
                            commandBuilder.Append("#end");
                            isvmEnd = true;
                            if (isBrowser)
                                await Task.Delay(1);
                            goto mark;
                        }
                        if (aiCommandsLowerMap.ContainsKey(contentCommand))
                        {
                            contentCommandValue = commandBuilder.ToString().Trim('\r').Trim('\n');
                        }
                        else if (!(string.IsNullOrWhiteSpace(defaultMsg) && (commandChar == '\r' || commandChar == '\n' || commandChar == ' ')))
                        {
                            defaultMsg += commandChar;
                            yield return new AIProviderHandleTextMessageResponse()
                            {
                                Message = commandChar.ToString()
                            };
                        }
                        break;
                    default:
                        if (!(string.IsNullOrWhiteSpace(defaultMsg) && (commandChar == '\r' || commandChar == '\n' || commandChar == ' ')))
                        {
                            defaultMsg += commandChar;
                            yield return new AIProviderHandleTextMessageResponse()
                            {
                                Message = commandChar.ToString()
                            };
                        }
                        break;
                }
                if (isBrowser)
                    await Task.Delay(1);
            }
        }
    }
}
