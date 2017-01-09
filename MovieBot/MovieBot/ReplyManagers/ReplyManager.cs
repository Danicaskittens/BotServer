﻿using Microsoft.Bot.Connector;
using MovieBot.States;
using MovieBot.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace MovieBot.ReplyManagers
{
    public abstract class ReplyManager
    {
        protected Activity activity;
        protected string input;

        public ReplyManager(Activity activity, string input)
        {
            this.activity = activity;
            this.input = input;
        }

        public abstract Task<Activity> getResponse();
        public abstract Task<Activity> getResponseWithState<T>(T state);

        protected async Task<Activity> parseStateReply<T>(StateReply stateReplay, StateClient stateClient, T state, String dataProperty)
        {
            if (!(stateReplay.IsFinalState))
            {
                BotData userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);
                userData.SetProperty<bool>(dataProperty, true);

                userData.SetProperty<T>(dataProperty +"State", state);
                BotData response = await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
            }
            else
            {
                await stateClient.BotState.DeleteStateForUserAsync(activity.ChannelId, activity.From.Id);
            }

            Activity replyToConversation = activity.CreateReply(stateReplay.GetReplayMessage);

            Activity replyParsed = manageAttachment(replyToConversation, stateReplay);

            return replyToConversation;
        }

        private Activity manageAttachment(Activity replyToConversation, StateReply replyFromState)
        {
            string special = replyFromState.GetSpecial;
            switch (special){
                case "herocard":
                    HeroCard heroGet = replyFromState.HeroCard;
                    replyToConversation.Recipient = activity.From;
                    replyToConversation.Type = "message";
                    replyToConversation.Attachments = new List<Attachment>();

                    Attachment plAttachment = heroGet.ToAttachment();
                    replyToConversation.Attachments.Add(plAttachment);
                    break;
            }

            return replyToConversation;
        }
    }
}