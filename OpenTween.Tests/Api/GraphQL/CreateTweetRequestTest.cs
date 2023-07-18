﻿// OpenTween - Client of Twitter
// Copyright (c) 2023 kim_upsilon (@kim_upsilon) <https://upsilo.net/~upsilon/>
// All rights reserved.
//
// This file is part of OpenTween.
//
// This program is free software; you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the Free
// Software Foundation; either version 3 of the License, or (at your option)
// any later version.
//
// This program is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License
// for more details.
//
// You should have received a copy of the GNU General Public License along
// with this program. If not, see <http://www.gnu.org/licenses/>, or write to
// the Free Software Foundation, Inc., 51 Franklin Street - Fifth Floor,
// Boston, MA 02110-1301, USA.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using OpenTween.Connection;
using Xunit;

namespace OpenTween.Api.GraphQL
{
    public class CreateTweetRequestTest
    {
        [Fact]
        public async Task Send_Test()
        {
            var responseText = File.ReadAllText("Resources/Responses/CreateTweet_CircleTweet.json");

            var mock = new Mock<IApiConnection>();
            mock.Setup(x =>
                    x.PostJsonAsync(It.IsAny<Uri>(), It.IsAny<string>())
                )
                .Callback<Uri, string>((url, json) =>
                {
                    Assert.Equal(new("https://twitter.com/i/api/graphql/tTsjMKyhajZvK4q76mpIBg/CreateTweet"), url);
                    Assert.Contains(@"""tweet_text"":""tetete""", json);
                    Assert.DoesNotContain(@"""reply"":", json);
                    Assert.DoesNotContain(@"""media"":", json);
                })
                .ReturnsAsync(responseText);

            var request = new CreateTweetRequest
            {
                TweetText = "tetete",
            };

            var status = await request.Send(mock.Object).ConfigureAwait(false);
            Assert.Equal("1680534146492317696", status.IdStr);

            mock.VerifyAll();
        }

        [Fact]
        public async Task Send_ReplyTest()
        {
            var responseText = File.ReadAllText("Resources/Responses/CreateTweet_CircleTweet.json");

            var mock = new Mock<IApiConnection>();
            mock.Setup(x =>
                    x.PostJsonAsync(It.IsAny<Uri>(), It.IsAny<string>())
                )
                .Callback<Uri, string>((url, json) =>
                {
                    Assert.Contains(@"""reply"":{""exclude_reply_user_ids"":[""11111"",""22222""],""in_reply_to_tweet_id"":""12345""}", json);
                })
                .ReturnsAsync(responseText);

            var request = new CreateTweetRequest
            {
                TweetText = "tetete",
                InReplyToTweetId = new("12345"),
                ExcludeReplyUserIds = new[] { "11111", "22222" },
            };
            await request.Send(mock.Object).ConfigureAwait(false);
            mock.VerifyAll();
        }

        [Fact]
        public async Task Send_MediaTest()
        {
            var responseText = File.ReadAllText("Resources/Responses/CreateTweet_CircleTweet.json");

            var mock = new Mock<IApiConnection>();
            mock.Setup(x =>
                    x.PostJsonAsync(It.IsAny<Uri>(), It.IsAny<string>())
                )
                .Callback<Uri, string>((url, json) =>
                {
                    Assert.Contains(@"""media"":{""media_entities"":[{""media_id"":""11111"",""tagged_users"":[]},{""media_id"":""22222"",""tagged_users"":[]}],""possibly_sensitive"":false}", json);
                })
                .ReturnsAsync(responseText);

            var request = new CreateTweetRequest
            {
                TweetText = "tetete",
                MediaIds = new[] { "11111", "22222" },
            };
            await request.Send(mock.Object).ConfigureAwait(false);
            mock.VerifyAll();
        }
    }
}
