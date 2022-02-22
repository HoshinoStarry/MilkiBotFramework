﻿using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MilkiBotFramework.Dispatching;
using MilkiBotFramework.Messaging;
using MilkiBotFramework.Tasking;

namespace MilkiBotFramework.ContractsManaging;

public abstract class ContractsManagerBase : IContractsManager
{
    private readonly BotTaskScheduler _botTaskScheduler;
    private readonly ILogger _logger;
    private IDispatcher? _dispatcher;

    protected ConcurrentDictionary<string, ConcurrentDictionary<string, ChannelInfo>> _subChannelMapping = new();
    protected ConcurrentDictionary<string, ChannelInfo> _channelMapping = new();
    protected ConcurrentDictionary<string, PrivateInfo> _privateMapping = new();

    protected ConcurrentDictionary<string, Avatar> _userAvatarMapping = new();
    protected ConcurrentDictionary<string, Avatar> _channelAvatarMapping = new();

    public ContractsManagerBase(BotTaskScheduler botTaskScheduler, ILogger logger)
    {
        _botTaskScheduler = botTaskScheduler;
        _logger = logger;
        _botTaskScheduler.AddTask("RefreshContractsTask", builder => builder
            .ByInterval(TimeSpan.FromSeconds(15))
            .AtStartup()
            .Do(RefreshContracts));
    }

    private void RefreshContracts(TaskContext context, CancellationToken token)
    {
        _logger.LogInformation("Refreshed!");
    }

    public IDispatcher? Dispatcher
    {
        get => _dispatcher;
        internal set
        {
            if (_dispatcher != null) _dispatcher.SystemMessageReceived -= Dispatcher_NoticeMessageReceived;
            _dispatcher = value;
            if (_dispatcher != null) _dispatcher.SystemMessageReceived += Dispatcher_NoticeMessageReceived;
        }
    }

    public bool TryGetMemberInfo(string channelId, string userId, [NotNullWhen(true)] out MemberInfo? memberInfo, string? subChannelId = null)
    {
        if (subChannelId == null)
        {
            if (_channelMapping.TryGetValue(channelId, out var channelInfo) &&
                channelInfo.Members.TryGetValue(userId, out memberInfo))
            {
                return true;
            }
        }
        else
        {
            if (_subChannelMapping.TryGetValue(channelId, out var subChannels) &&
                subChannels.TryGetValue(channelId, out var channelInfo) &&
                channelInfo.Members.TryGetValue(userId, out memberInfo))
            {
                return true;
            }
        }

        memberInfo = null;
        return false;
    }

    public bool TryGetChannelInfo(string channelId,
        [NotNullWhen(true)] out ChannelInfo? channelInfo,
        string? subChannelId = null)
    {
        if (subChannelId == null)
            return _channelMapping.TryGetValue(channelId, out channelInfo);

        channelInfo = null;
        return _subChannelMapping.TryGetValue(channelId, out var dict) &&
               dict.TryGetValue(subChannelId, out channelInfo);
    }

    public bool TryGetPrivateInfo(string userId, out PrivateInfo privateInfo)
    {
        throw new NotImplementedException();
    }

    public void AddMember(string channelId, MemberInfo member)
    {
    }

    public void RemoveMember(string channelId, string userId)
    {
    }

    public void AddChannel(ChannelInfo channelInfo)
    {
        _channelMapping.AddOrUpdate(channelInfo.ChannelId, channelInfo, (id, instance) => channelInfo);
    }

    public void RemoveChannel(string channelId)
    {
    }

    public void AddSubChannel(string channelId, ChannelInfo subChannelInfo)
    {
    }

    public void RemoveSubChannel(string channelId, string subChannelId)
    {
    }

    public void AddPrivate(PrivateInfo channelInfo)
    {
    }

    public void RemovePrivate(string userId)
    {
    }

    protected virtual Task<ContractUpdateResult> UpdateMemberIfPossible(MessageContext messageContext)
    {
        return Task.FromResult(new ContractUpdateResult(false, null, ContractUpdateType.Unspecified));
    }

    protected virtual Task<ContractUpdateResult> UpdateChannelsIfPossible(MessageContext messageContext)
    {
        return Task.FromResult(new ContractUpdateResult(false, null, ContractUpdateType.Unspecified));
    }

    protected virtual Task<ContractUpdateResult> UpdatePrivatesIfPossible(MessageContext messageContext)
    {
        return Task.FromResult(new ContractUpdateResult(false, null, ContractUpdateType.Unspecified));
    }

    private async Task Dispatcher_NoticeMessageReceived(MessageContext messageContext)
    {
        var updateResult = await UpdateMemberIfPossible(messageContext);
        if (updateResult.IsSuccess)
        {
            _logger.LogInformation("Member " + updateResult.ContractUpdateType + ": " + updateResult.Id);
            return;
        }

        updateResult = await UpdateChannelsIfPossible(messageContext);
        if (updateResult.IsSuccess)
        {
            _logger.LogInformation("Channel " + updateResult.ContractUpdateType + ": " + updateResult.Id);
            return;
        }

        updateResult = await UpdatePrivatesIfPossible(messageContext);
        if (updateResult.IsSuccess)
        {
            _logger.LogInformation("Private " + updateResult.ContractUpdateType + ": " + updateResult.Id);
        }
    }

    public abstract Task<ChannelInfoResult> TryGetChannelInfoByMessageContext(MessageIdentity messageIdentity, string userId);
    public abstract Task<PrivateInfoResult> TryGetPrivateInfoByMessageContext(MessageIdentity messageIdentity);
}

public sealed class ChannelInfoResult : ResultInfoBase
{
    public ChannelInfo? ChannelInfo { get; init; }
    public MemberInfo? MemberInfo { get; init; }
}

public sealed class PrivateInfoResult : ResultInfoBase
{
    public PrivateInfo? PrivateInfo { get; init; }
}

public abstract class ResultInfoBase
{
    public bool IsSuccess { get; init; }
}