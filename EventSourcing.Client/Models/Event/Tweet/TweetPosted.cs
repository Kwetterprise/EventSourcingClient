﻿using System;

namespace Kwetterprise.EventSourcing.Client.Models.Event.Tweet
{
    public sealed class TweetPosted : EventBase
    {
        public TweetPosted()
        {
        }

        public TweetPosted(Guid id, Guid author, string content, Guid? parentTweet, DateTime postedOn)
            : base(EventType.TweetPosted)
        {
            this.Id = id;
            this.Author = author;
            this.Content = content;
            this.ParentTweet = parentTweet;
            this.PostedOn = postedOn;
        }

        public Guid Id { get; set; }

        public Guid Author { get; set; }

        public string Content { get; set; } = null!;

        public Guid? ParentTweet { get; set; }

        public DateTime PostedOn { get; set; }
    }
}
