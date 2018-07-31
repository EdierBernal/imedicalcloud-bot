using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.CognitiveServices.QnAMaker;
using Microsoft.Bot.Connector;
using Microsoft.Azure;
using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;

namespace Microsoft.Bot.Sample.QnABot
{
    [Serializable]
    public class qnaMakerDialogImedical : IDialog<object>
    {
        public async Task StartAsync(IDialogContext context)
        {
            /* Wait until the first message is received from the conversation and call MessageReceviedAsync 
           *  to process that message. */

            context.Wait(this.MessageReceivedAsync);
        }
        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            /* When MessageReceivedAsync is called, it's passed an IAwaitable<IMessageActivity>. To get the message,
             *  await the result. */
            var message = await result;

            var qnaAuthKey = CloudConfigurationManager.GetSetting("QnAAuthKey");
            var qnaKBId = CloudConfigurationManager.GetSetting("QnAKnowledgebaseId");
            var endpointHostName = CloudConfigurationManager.GetSetting("QnAEndpointHostName");

            // QnA Subscription Key and KnowledgeBase Id null verification
            if (!string.IsNullOrEmpty(qnaAuthKey) && !string.IsNullOrEmpty(qnaKBId))
            {
                // Forward to the appropriate Dialog based on whether the endpoint hostname is present
                if (string.IsNullOrEmpty(endpointHostName))
                {
                    await context.Forward(new BasicQnAMakerPreviewDialogImedical(), AfterAnswerAsync, message, CancellationToken.None);
                }
                else
                {
                    string messageToLower = message.Text.ToString().ToLower();
                    if ((messageToLower).Contains("hola"))
                    {
                        //Creat Card Start App
                        QnAMakerResults resultQnA = new QnAMakerResults();
                        await RespondFromQnAMakerResultAsync(context, message, resultQnA);
                    }

                    else
                    {
                        await context.Forward(new BasicQnAMakerDialogImedical(), AfterAnswerAsync, message, CancellationToken.None);
                    }
                }
            }
            else
            {
                await context.PostAsync("Error al conectarse con la base de conocimientos, por favor comuniquese con el área de TI");
            }

        }
        static string BlobUrl(string nameImage)
        {
            //Get url Image wellcome
            var account = new CloudStorageAccount(new StorageCredentials(CloudConfigurationManager.GetSetting("AccountStorageName"), CloudConfigurationManager.GetSetting("AccountStorageKey")), true);
            var cloudBlobClient = account.CreateCloudBlobClient();
            var container = cloudBlobClient.GetContainerReference(CloudConfigurationManager.GetSetting("StorageContainerName"));
            var blob = container.GetBlockBlobReference(nameImage);
            //blob.UploadFromFile("File Path ....");//Upload file....

            var blobUrl = blob.Uri.AbsoluteUri;
            return blobUrl;
        }
        protected async Task RespondFromQnAMakerResultAsync(IDialogContext context, IMessageActivity message, QnAMakerResults result)
        {
            Activity respuesta = ((Activity)context.Activity).CreateReply();
            //var firstAnswer = result.Answers.First().Answer;
            //var answerQnAdata = firstAnswer.Split(';');

            //if (answerQnAdata.Length == 1)
            //{
            //    await context.PostAsync(firstAnswer);
            //    return;
            //}

            //var titulo = answerQnAdata[0];
            //var descripcion = answerQnAdata[1];
            //var url = answerQnAdata[2];

            var titulo = "iMedical Support";
            var descripcion = "Hola! Bienvenido, en qué te podemos ayudar?.";
            //var url = "";
            var urlimagen = BlobUrl("wellcome.png");

            HeroCard card = new HeroCard
            {
                Title = titulo,
                Subtitle = descripcion
            };

            //card.Buttons = new List<CardAction>
            //         {
            //        new CardAction(ActionTypes.OpenUrl, "iMedical E-Learning", value:url)
            //         };

            card.Images = new List<CardImage>
                     {
                    new CardImage(urlimagen)
                     };

            respuesta.Attachments.Add(card.ToAttachment());
            await context.PostAsync(respuesta);
        }

        private async Task AfterAnswerAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            // wait for the next user message
            context.Wait(MessageReceivedAsync);
        }

        public static string GetSetting(string key)
        {
            var value = CloudConfigurationManager.GetSetting(key);
            if (String.IsNullOrEmpty(value) && key == "QnAAuthKey")
            {
                value = CloudConfigurationManager.GetSetting("QnASubscriptionKey"); // QnASubscriptionKey for backward compatibility with QnAMaker (Preview)
            }
            return value;
        }
    }

    // Dialog for QnAMaker Preview service
    [Serializable]
    public class BasicQnAMakerPreviewDialogImedical : QnAMakerDialog
    {
        // Go to https://qnamaker.ai and feed data, train & publish your QnA Knowledgebase.
        // Parameters to QnAMakerService are:
        // Required: subscriptionKey, knowledgebaseId, 
        // Optional: defaultMessage, scoreThreshold[Range 0.0 – 1.0]
        public BasicQnAMakerPreviewDialogImedical() : base(new QnAMakerService(new QnAMakerAttribute(RootDialog.GetSetting("QnAAuthKey"), CloudConfigurationManager.GetSetting("QnAKnowledgebaseId"), "No good match in FAQ.", 0.5)))
        {

        }
    }

    // Dialog for QnAMaker GA service
    [Serializable]
    public class BasicQnAMakerDialogImedical : QnAMakerDialog
    {
        // Go to https://qnamaker.ai and feed data, train & publish your QnA Knowledgebase.
        // Parameters to QnAMakerService are:
        // Required: qnaAuthKey, knowledgebaseId, endpointHostName
        // Optional: defaultMessage, scoreThreshold[Range 0.0 – 1.0]
        public BasicQnAMakerDialogImedical() : base(new QnAMakerService(new QnAMakerAttribute(RootDialog.GetSetting("QnAAuthKey"), CloudConfigurationManager.GetSetting("QnAKnowledgebaseId"), "No good match in FAQ.", 0.5, 1, CloudConfigurationManager.GetSetting("QnAEndpointHostName"))))
        {

        }
    }
}