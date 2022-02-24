﻿using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MilkiBotFramework.ContractsManaging;
using MilkiBotFramework.ContractsManaging.Models;
using MilkiBotFramework.ContractsManaging.Results;
using MilkiBotFramework.Messaging;
using MilkiBotFramework.Platforms.GoCqHttp.Connecting;
using MilkiBotFramework.Tasking;

namespace MilkiBotFramework.Platforms.GoCqHttp.ContractsManaging;

public sealed class GoCqContractsManager : ContractsManagerBase
{
    private readonly GoCqApi _goCqApi;
    private readonly ILogger<GoCqContractsManager> _logger;

    public GoCqContractsManager(GoCqApi goCqApi, BotTaskScheduler botTaskScheduler, ILogger<GoCqContractsManager> logger)
        : base(botTaskScheduler, logger)
    {
        _goCqApi = goCqApi;
        _logger = logger;
    }

    public override async Task<SelfInfoResult> TryGetSelfInfo()
    {
        var result = await _goCqApi.GetLoginInfo();
        return new SelfInfoResult()
        {
            IsSuccess = true,
            UserId = result.UserId.ToString(),
            Nickname = result.Nickname
        };
    }

    public override async Task<ChannelInfoResult> TryGetChannelInfoByMessageContext(MessageIdentity messageIdentity, string userId)
    {
        if (messageIdentity.Id == null) throw new ArgumentNullException(nameof(messageIdentity.Id));

        var channelId = messageIdentity.Id;
        var subChannelId = messageIdentity.SubId;
        if (!TryGetChannelInfo(channelId, out var channelInfo, subChannelId))
        {
            if (subChannelId == null)
            {
                try
                {
                    var groupInfo = await _goCqApi.GetGroupInfo(long.Parse(channelId));
                    channelInfo = new ChannelInfo(channelId) { Name = groupInfo.GroupName };
                    AddChannel(channelInfo);
                }
                catch (GoCqApiException ex)
                {
                    _logger.LogWarning("获取群信息时API返回错误：" + ex.Message);
                    return new ChannelInfoResult();
                }
            }
            else
            {
                throw new NotImplementedException(); // guild
            }
        }

        if (!TryGetMemberInfo(channelId, userId, out var memberInfo, subChannelId))
        {
            try
            {
                var groupMember = await _goCqApi.GetGroupMemberDetail(long.Parse(channelId), long.Parse(userId));
                memberInfo = new MemberInfo(groupMember.UserId)
                {
                    Nickname = groupMember.Nickname,
                    Card = groupMember.Card,
                    MemberRole = groupMember.Role switch
                    {
                        "owner" => MemberRole.Owner,
                        "admin" => MemberRole.Admin,
                        "member" => MemberRole.Member,
                        _ => MemberRole.Member
                    }
                };
                AddMember(channelId, memberInfo);
            }
            catch (GoCqApiException ex)
            {
                _logger.LogWarning("获取群成员信息时API返回错误：" + ex.Message);
                return new ChannelInfoResult { ChannelInfo = channelInfo };
            }
        }

        return new ChannelInfoResult { ChannelInfo = channelInfo, IsSuccess = true, MemberInfo = memberInfo };
    }

    public override async Task<PrivateInfoResult> TryGetPrivateInfoByMessageContext(MessageIdentity messageIdentity)
    {
        if (messageIdentity.Id == null) throw new ArgumentNullException(nameof(messageIdentity.Id));

        var userId = messageIdentity.Id;
        if (!TryGetPrivateInfo(userId, out var privateInfo))
        {
            try
            {
                var stranger = await _goCqApi.GetStrangerInfo(long.Parse(userId));
                privateInfo = new PrivateInfo(userId)
                {
                    Nickname = stranger.Nickname,
                };
                AddPrivate(privateInfo);
            }
            catch (GoCqApiException ex)
            {
                _logger.LogWarning("获取私聊用户信息时API返回错误：" + ex.Message);
                return new PrivateInfoResult { PrivateInfo = privateInfo };
            }
        }

        return new PrivateInfoResult { PrivateInfo = privateInfo, IsSuccess = true };
    }
}