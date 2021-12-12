using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ChatBridge.MessageContent;
using ImageProcessor;
using ImageProcessor.Imaging.Formats;
using ImageProcessor.Plugins.WebP.Imaging.Formats;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VkNet;
using VkNet.Enums;
using VkNet.Enums.SafetyEnums;
using VkNet.Extensions.Polling;
using VkNet.Extensions.Polling.Models.Configuration;
using VkNet.Extensions.Polling.Models.Update;
using VkNet.Model;
using VkNet.Model.Attachments;
using VkNet.Model.RequestParams;
using VkNet.Model.RequestParams.Polls;

namespace ChatBridge.Extensions.Vk.Internal
{
    /// <summary>
    /// Chat for Vk implementing <seealso cref="IBridgeChat"/> interface
    /// </summary>
    internal class VkChat : IBridgeChat
    {
        private readonly Random _random;
        private readonly VkApi _api;
        private readonly VkChatConfiguration _configuration;
        private readonly ILogger<VkChat> _logger;
        private Subject<BridgeMessage> _mesageSubject;
        private UserLongPoll _longPoll;
        private readonly long _peerId;
        private List<User> _chatUsers; 

        public string ServiceName => "Vk";

        public VkChat(
            Random random,
            IServiceCollection services,
            ILogger<VkChat> logger,
            VkChatConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _api = new VkApi(services);
            _peerId = Constants.GroupChatIdPrefix + configuration.ChatId.Value;
            _random = random;
        }

        /// <summary>
        /// Reads text from update
        /// </summary>
        /// <param name="update">Update event</param>
        /// <returns>null if event type is not supported or be parsed; <seealso cref="BridgeMessage"/> to send to other Chats otherwise</returns>
        private async Task<BridgeMessage> ReadMessageFromUpdateAsync(UserUpdate update)
        {
            await Task.CompletedTask;
            if (update.Message.PeerId.Value != _peerId)
            {
                return null;
            }
            Message message = update.Message;


            if (message.Type == MessageType.Sended)
            {
                return null;
            }
            long fromId = message.FromId.GetValueOrDefault();
            User sender = _chatUsers.FirstOrDefault(x => x.Id == fromId);
            string senderName = sender == null ? "Unknown" : $"{sender.FirstName.FirstCharToUpper()} {sender.LastName.FirstCharToUpper()}".Trim();
            List<BridgeMessageContent> contents = new List<BridgeMessageContent>(); ;

            //Для Reply
            if (message.ReplyMessage != null)
            {
                //Получение ID для Reply
                //message.ReplyMessage.Id;
            }

            //Для forward
            if (message.ForwardedMessages != null)
            {
                var fwdContents = new List<BridgeMessageContent>();

                foreach (var fwdMsg in message.ForwardedMessages)
                {
                    await GetContents(fwdContents, fwdMsg);
                }

                contents.Add(new ForwardContent(fwdContents, senderName));
            }

            await GetContents(contents, message);

            return new BridgeMessage(this, senderName, contents);
        }

        private async Task<List<BridgeMessageContent>> GetContents(List<BridgeMessageContent> contents, Message message)
        {
            long fromId = message.FromId.GetValueOrDefault();
            User sender = _chatUsers.FirstOrDefault(x => x.Id == fromId);
            string senderName = sender == null
                ? "Unknown"
                : $"{sender.FirstName.FirstCharToUpper()} {sender.LastName.FirstCharToUpper()}".Trim();

            foreach (var attachment in message.Attachments)
            {
                var tag = attachment.Instance.ToString();
                var type = BridgeMessageContentType.Text;

                if (tag.Contains("sticker"))
                {
                    _logger.LogInformation("vk:sticker");

                    var sticker = ((Sticker) attachment.Instance).Images.LastOrDefault().Url.ToString();
                    await using (var fileStream = new MemoryStream(new WebClient().DownloadData(sticker)))
                    {
                        fileStream.Position = 0L;
                        ISupportedImageFormat format = new WebPFormat();
                        await using (MemoryStream outStream = new MemoryStream())
                        {
                            // Initialize the ImageFactory using the overload to preserve EXIF metadata.
                            using (ImageFactory imageFactory = new ImageFactory(preserveExifData: true))
                            {
                                Size size = new Size(150, 150);
                                // Load, resize, set the format and quality and save an image.
                                imageFactory.Load(fileStream)
                                    .Resize(size)
                                    .Format(format)
                                    .Save(outStream);
                            }

                            contents.Add(new StickerContent(outStream.ToArray(), senderName));
                        }
                    }
                }
                else if (tag.Contains("photo"))
                {
                    var photoUrl = ((Photo) attachment.Instance).Sizes.Last().Url.AbsoluteUri;
                    contents.Add(new PhotoContent(photoUrl, message.Attachments.Count > 1 ? string.Empty : message.Text,
                        senderName));
                }
                else if (tag.Contains("video"))
                {
                    var video = (Video) attachment.Instance;
                    var title = video.Title;
                    var url = $"{video.Image.Last().Url.AbsoluteUri}";
                    contents.Add(new VideoContent(url, title,
                        message.Attachments.Count > 1 ? string.Empty : message.Text, senderName));
                }

                else if (tag.Contains("audio_message"))
                {
                    Console.WriteLine(tag + "sent");
                }
                else if (tag.Contains("audio"))
                {
                    Console.WriteLine(tag + "sent");
                }
                else if (tag.Contains("doc"))
                {
                    Console.WriteLine(tag + "sent");
                }
                else if (tag.Contains("poll"))
                {
                    var poll = (Poll) attachment.Instance;
                    var question = poll.Question;
                    var options = poll.Answers.Select(x => x.Text).ToArray();
                    var isAnonymous = poll.Anonymous;
                    var isMultiple = poll.Multiple;

                    contents.Add(new PollContent(question, options, isAnonymous, isMultiple, message.Text, senderName));
                }
            }
            if (message.Attachments.Count != 1)
            {
                contents.Add(new TextContent(message.Text, senderName));
            }
            return contents;
        }

        /// <summary>
        /// Runs listening updates of the client
        /// </summary>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        private async Task StartListeningUpdates(CancellationToken cancelToken)
        {
            ChannelReader<UserUpdate> channel = _longPoll.AsChannelReader();

            while (cancelToken.IsCancellationRequested == false)
            {
                bool canContinue = await channel.WaitToReadAsync(cancelToken);
                if (!canContinue)
                {
                    break;
                }
                UserUpdate update = await channel.ReadAsync(cancelToken);
                BridgeMessage message = await ReadMessageFromUpdateAsync(update);
                if (message == null)
                {
                    continue;
                }
                _mesageSubject.OnNext(message);
            }
        }

        /// <inheritdoc/>
        public async Task InitializeAsync(CancellationToken cancelToken = default)
        {
            _logger.LogInformation("Start initializing Vk Chat");
            await _api.AuthorizeAsync(new ApiAuthParams
            {
                AccessToken = _configuration.Token
            });
            _api.VkApiVersion.SetVersion(5, 131);

            //Getting and caching all users of conversation, to not send get request every time we need their info (name, surname)
            GetConversationMembersResult chatMembers = await _api.Messages.GetConversationMembersAsync(_peerId);
            _chatUsers = new List<User>(chatMembers.Profiles);

            _logger.LogInformation("Initializing Vk Chat complete successfully");
        }

        /// <inheritdoc/>
        public async Task SendMessageAsync(BridgeMessage message, CancellationToken cancelToken = default)
        {
            foreach (var content in message.Content)
            {
                switch (content.Type)
                {
                    //case BridgeMessageContentType.Unknown:
                    //    break;
                    case BridgeMessageContentType.Text:
                        await SendTextAsync(_peerId, content.AsTextContent());
                        break;
                    case BridgeMessageContentType.Photo:
                        await SendPhotoAsync(_peerId, content.AsPhotoContent());
                        break;
                    //case BridgeMessageContentType.Audio:
                    //    break;
                    case BridgeMessageContentType.Video:
                        await SendVideoAsync(_peerId, content.AsVideoContent());
                        break;
                    //case BridgeMessageContentType.Voice:
                    //    break;
                    //case BridgeMessageContentType.Document:
                    //    break;
                    case BridgeMessageContentType.Sticker:
                        await SendStickerAsync(_peerId, content.AsStickerContent());
                        break;
                    //case BridgeMessageContentType.ChatMembersAdded:
                    //    break;
                    //case BridgeMessageContentType.ChatMemberLeft:
                    //    break;
                    case BridgeMessageContentType.Poll:
                        await SendPollAsync(_peerId, content.AsPollContent());
                        break;
                    //case BridgeMessageContentType.Reply:
                    //    break;
                    //case BridgeMessageContentType.Forwarded:
                    //    break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public async Task<long> SendTextAsync(long conversationId, TextContent content)
        {
            var msgId = await _api.Messages.SendAsync(new MessagesSendParams
            { PeerId = conversationId, RandomId = new Random().Next(int.MaxValue), Message = content.FormMessage() });
            
            //Пример Reply
            //var ID; ID сообщения на которое надо Reply.
            //var replyMsg = await _api.Messages.SendAsync(new MessagesSendParams{ PeerId = conversationId, RandomId = new Random().Next(int.MaxValue), Message = content.FormMessage(), ReplyTo = ID});

            //Пример Forward
            //var IDS; Коллекция IDшек
            //var msg = await _api.Messages.SendAsync(new MessagesSendParams { PeerId = conversationId, RandomId = new Random().Next(int.MaxValue), Message = content.FormMessage(), ForwardMessages = IDS});

            return msgId;
        }

        internal async Task<long> SendPhotoAsync(long conversationId, PhotoContent content)
        {
            _logger.LogInformation($"Sent {content.Type} | {content.AsPhotoContent().Url}");
            var uploadServer = await _api.Photo.GetMessagesUploadServerAsync(0);
            var response = UploadFileAsync(uploadServer.UploadUrl, content.AsPhotoContent().Url).Result;
            var attachment = await _api.Photo.SaveMessagesPhotoAsync(response);
            return _api.Messages.Send(new MessagesSendParams { PeerId = conversationId, RandomId = new Random().Next(int.MaxValue), Message = content.FormMessage(), Attachments = attachment });
        }

        internal async Task<long> SendVideoAsync(long conversationId, VideoContent content)
        {
            _logger.LogInformation($"Sent {content.Type}");
            var uploadServer = await _api.Photo.GetMessagesUploadServerAsync(0);
            var response = UploadFileAsync(uploadServer.UploadUrl, content.AsVideoContent().Thumbnail).Result;
            var attachment = await _api.Photo.SaveMessagesPhotoAsync(response);
            return _api.Messages.Send(new MessagesSendParams { PeerId = conversationId, RandomId = new Random().Next(int.MaxValue), Message = content.FormMessage(), Attachments = attachment });
        }

        internal async Task<long> SendStickerAsync(long conversationId, StickerContent content)
        {
            _logger.LogInformation($"Sent {content.Type}");
            var uploadServer = await _api.Docs.GetMessagesUploadServerAsync(0, DocMessageType.Graffiti);
            var response = UploadFileAsync(uploadServer.UploadUrl, content.Url).Result;
            var attachment = new List<MediaAttachment> { _api.Docs.SaveAsync(response, new Random().Next(int.MaxValue).ToString(), null).Result.FirstOrDefault().Instance };
            return _api.Messages.Send(new MessagesSendParams { PeerId = conversationId, RandomId = new Random().Next(int.MaxValue), Message = content.AsStickerContent().FormMessage(), Attachments = attachment });
        }

        internal async Task<long> SendPollAsync(long conversationId, PollContent content)
        {
            _logger.LogInformation($"Sent {content.Type}");
            var pollConent = content.AsPollContent();
            var poll = await _api.PollsCategory.CreateAsync(new PollsCreateParams { Question = pollConent.Question, AddAnswers = pollConent.Options, IsAnonymous = pollConent.IsAnonymous, IsMultiple = pollConent.IsMultiple });
            return _api.Messages.Send(new MessagesSendParams { PeerId = conversationId, RandomId = new Random().Next(int.MaxValue), Message = pollConent.Caption, Attachments = new[] { poll } });
        }

        internal async Task<long> ReplyAsync(long conversationId, string message, long id)
        {
            var msgId = await _api.Messages.SendAsync(new MessagesSendParams
            { PeerId = conversationId, RandomId = new Random().Next(int.MaxValue), Message = message, ReplyTo = id });
            return msgId;
        }


        private static async Task<string> UploadFileAsync(string serverUrl, string fileUrl)
        {
            using (var client = new HttpClient())
            {
                var requestContent = new MultipartFormDataContent();
                var content = new ByteArrayContent(GetBytes(fileUrl));
                content.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
                requestContent.Add(content, "file", $"file.{fileUrl.Split(".").Last()}");

                var response = client.PostAsync(serverUrl, requestContent).Result;
                return Encoding.Default.GetString(await response.Content.ReadAsByteArrayAsync());
            }
        }

        private static byte[] GetBytes(string fileUrl)
        {
            using (var webClient = new WebClient())
            {
                return webClient.DownloadData(fileUrl);
            }
        }

        /// <inheritdoc/>
        public async Task<IObservable<BridgeMessage>> StartListenAsync(CancellationToken cancelToken = default)
        {
            _logger.LogInformation("Running Vk Chat...");

            _mesageSubject = new Subject<BridgeMessage>();
            _longPoll = _api.StartUserLongPollAsync(UserLongPollConfiguration.Default, cancelToken);

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            StartListeningUpdates(cancelToken);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            await Task.CompletedTask;

            _logger.LogInformation("Vk Chat is running.");
            return _mesageSubject;
        }
    }
}
