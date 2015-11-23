﻿using System;
using System.Collections.Generic;
using System.Linq;
using FlickrNet;
using Noobot.Domain.Configuration;
using Noobot.Domain.MessagingPipeline.Middleware;
using Noobot.Domain.MessagingPipeline.Request;
using Noobot.Domain.MessagingPipeline.Response;
using Noobot.Domain.Plugins.StandardPlugins;

namespace Noobot.Custom.Pipeline.Middleware
{
    public class FlickrMiddleware : MiddlewareBase
    {
        private readonly IConfigReader _configReader;
        private readonly StatsPlugin _statsPlugin;

        public FlickrMiddleware(IMiddleware next, IConfigReader configReader, StatsPlugin statsPlugin) : base(next)
        {
            _configReader = configReader;
            _statsPlugin = statsPlugin;

            HandlerMappings = new[]
            {
                new HandlerMapping
                {
                    ValidHandles = new [] { "flickr", "pic"},
                    Description = "Finds a pics from flickr - usage: /flickr birds",
                    EvaluatorFunc = FlickrHandler,
                }
            };
        }

        private IEnumerable<ResponseMessage> FlickrHandler(IncomingMessage message, string matchedHandle)
        {
            string searchTerm = message.TargetedText.Substring(matchedHandle.Length).Trim();

            if (string.IsNullOrEmpty(searchTerm))
            {
                yield return message.ReplyToChannel($"Please give me something to search, e.g. {matchedHandle} trains");
            }
            else
            {
                yield return message.IndicateTypingOnChannel();
                string apiKey = _configReader.GetConfig()["flickr"].Value<string>("apiKey");

                if (string.IsNullOrEmpty(apiKey))
                {
                    _statsPlugin.RecordStat("Flickr:Failed", 1);
                    yield return message.ReplyToChannel("Woops, looks like a Flickr API Key has not been entered. Please ask the admin to fix this");
                }
                else
                {
                    var flickr = new Flickr(apiKey);

                    var options = new PhotoSearchOptions { Tags = searchTerm, PerPage = 50, Page = 1};
                    PhotoCollection photos = flickr.PhotosSearch(options);

                    if (photos.Any())
                    {
                        _statsPlugin.RecordStat("Flickr:Sent", 1);

                        int i = new Random().Next(0, photos.Count);
                        Photo photo = photos[i];
                        yield return message.ReplyToChannel(photo.LargeUrl);
                    }
                    else
                    {
                        _statsPlugin.RecordStat("Flickr:Failed", 1);
                        yield return message.ReplyToChannel($"Sorry @{message.Username}, I couldn't find anything about {searchTerm}");
                    }
                }
            }
        }
    }
}