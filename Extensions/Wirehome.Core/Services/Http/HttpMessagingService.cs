﻿using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Wirehome.ComponentModel.Messaging;
using Wirehome.Core.EventAggregator;
using Wirehome.Core.Extensions;

namespace Wirehome.Core.Services.Http
{
    public class HttpMessagingService : IHttpMessagingService
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly DisposeContainer _disposeContainer = new DisposeContainer();

        public HttpMessagingService(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
        }

        public void Dispose() => _disposeContainer.Dispose();

        public Task Initialize()
        {
            _disposeContainer.Add(_eventAggregator.SubscribeForAsyncResult<IHttpMessage>(MessageHandler));
            return Task.CompletedTask;
        }

        public async Task<object> MessageHandler(IMessageEnvelope<IHttpMessage> message)
        {
            var httpMessage = message.Message;

            if (httpMessage.RequestType == "POST")
            {
                return await SendPostRequest(message);
            }
            else
            if (httpMessage.RequestType == "GET")
            {
                return await SendGetRequest(message);
            }

            return null;
        }

        public async Task<object> SendGetRequest(IMessageEnvelope<IHttpMessage> message)
        {
            using (var httpClient = new HttpClient())
            {
                var address = message.Message.MessageAddress();
                var httpResponse = await httpClient.GetAsync(address);
                httpResponse.EnsureSuccessStatusCode();
                var responseBody = await httpResponse.Content.ReadAsStringAsync();

                return message.Message.ParseResult(responseBody, message.ResponseType);
            }
        }

        public async Task<object> SendPostRequest(IMessageEnvelope<IHttpMessage> message)
        {
            var address = message.Message.MessageAddress();
            var httpClientHandler = new HttpClientHandler();
            if (message.Message.Cookies != null)
            {
                httpClientHandler.CookieContainer = message.Message.Cookies;
                httpClientHandler.UseCookies = true;
            }

            if (message.Message.Creditionals != null)
            {
                httpClientHandler.Credentials = message.Message.Creditionals;
            }

            using (var httpClient = new HttpClient(httpClientHandler))
            {
                foreach (var header in message.Message.DefaultHeaders)
                {
                    httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                }

                if (!string.IsNullOrWhiteSpace(message.Message.AuthorisationHeader.Key))
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(message.Message.AuthorisationHeader.Key, message.Message.AuthorisationHeader.Value);
                }

                var content = new StringContent(message.Message.Serialize());
                if (!string.IsNullOrWhiteSpace(message.Message.ContentType))
                {
                    content.Headers.ContentType = new MediaTypeHeaderValue(message.Message.ContentType);
                }
                var response = await httpClient.PostAsync(address, content);
                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadAsStringAsync(Encoding.UTF8);

                return message.Message.ParseResult(responseBody, message.ResponseType);
            }
        }
    }
}