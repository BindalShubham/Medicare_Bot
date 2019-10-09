// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// See https://github.com/microsoft/botbuilder-samples for a more comprehensive list of samples.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BasicBot.Dialogs.Medic;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.BotBuilderSamples
{
    /// <summary>
    /// Main entry point and orchestration for bot.
    /// </summary>
    public class BasicBot : IBot
    {
        // Supported LUIS Intents
        public const string GreetingIntent = "Greeting";
        public const string CancelIntent = "Cancel";
        public const string HelpIntent = "Help";
        public const string NoneIntent = "None";

        public const string MedicIntent = "Medical_Help";

        // Cosmos DB Configrations 
        private const string CosmosServiceEndpoint = "https://smartcaredb.documents.azure.com:443/";
        private const string CosmosDBKey = "VumOoKENgbNZxLsDXvGa5eyn02slmx0W8QiRescMs859us5pt7dqFrGcV48b3b7fQOGuXWern4MYFsLHMOH0aQ==";
        private const string CosmosDBDatabaseName = "bot-smartcare-sql";
        private const string CosmosDBCollectionName = "bot-storage";


        private static readonly CosmosDbStorage _myStorage = new CosmosDbStorage(new CosmosDbStorageOptions
        {
            AuthKey = CosmosDBKey,
            CollectionId = CosmosDBCollectionName,
            CosmosDBEndpoint = new Uri(CosmosServiceEndpoint),
            DatabaseId = CosmosDBDatabaseName,
        });

        // Create cancellation token (used by Async Write operation).
        public CancellationToken cancellationToken { get; private set; }

        // Class for storing a log of utterances (text of messages) as a list.
        public class UtteranceLog : IStoreItem
        {
            // A list of things that users have said to the bot
            public List<string> UtteranceList { get; } = new List<string>();

            // The number of conversational turns that have occurred        
            public int TurnNumber { get; set; } = 0;

            // Create concurrency control where this is used.
            public string ETag { get; set; } = "*";
        }

        /// <summary>
        /// Key in the bot config (.bot file) for the LUIS instance.
        /// In the .bot file, multiple instances of LUIS can be configured.
        /// </summary>
        public static readonly string LuisConfiguration = "BasicBotLuisApplication";

        private readonly IStatePropertyAccessor<GreetingState> _greetingStateAccessor;
        private readonly IStatePropertyAccessor<MedicState> _medicStateAccessor;
        private readonly IStatePropertyAccessor<DialogState> _dialogStateAccessor;
        private readonly UserState _userState;
        private readonly ConversationState _conversationState;
        private readonly BotServices _services;

        /// <summary>
        /// Initializes a new instance of the <see cref="BasicBot"/> class.
        /// </summary>
        /// <param name="botServices">Bot services.</param>
        /// <param name="accessors">Bot State Accessors.</param>
        public BasicBot(BotServices services, UserState userState, ConversationState conversationState, ILoggerFactory loggerFactory)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _userState = userState ?? throw new ArgumentNullException(nameof(userState));
            _conversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));

            _greetingStateAccessor = _userState.CreateProperty<GreetingState>(nameof(GreetingState));
            _medicStateAccessor = _userState.CreateProperty<MedicState>(nameof(MedicState));

            _dialogStateAccessor = _conversationState.CreateProperty<DialogState>(nameof(DialogState));
            
            // Verify LUIS configuration.
            if (!_services.LuisServices.ContainsKey(LuisConfiguration))
            {
                throw new InvalidOperationException($"The bot configuration does not contain a service type of `luis` with the id `{LuisConfiguration}`.");
            }

            Dialogs = new DialogSet(_dialogStateAccessor);
            //Dialogs.Add(new GreetingDialog(_greetingStateAccessor, loggerFactory));
            Dialogs.Add(new MedicDialog(_medicStateAccessor, loggerFactory));
        }

        private DialogSet Dialogs { get; set; }

        /// <summary>
        /// Run every turn of the conversation. Handles orchestration of messages.
        /// </summary>
        /// <param name="turnContext">Bot Turn Context.</param>
        /// <param name="cancellationToken">Task CancellationToken.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var activity = turnContext.Activity;
            if(activity.Text == null && activity.Value != null)
            {
                turnContext.Activity.Text = turnContext.Activity.Value.ToString();
            }
            // Create a dialog context
            var dc = await Dialogs.CreateContextAsync(turnContext);

            if (activity.Type == ActivityTypes.Message)
            {

                var utterance = turnContext.Activity.Text;
                UtteranceLog logItems = null;

                // see if there are previous messages saved in sstorage.
                string[] utteranceList = { "UtteranceLog" };

                //logItems = _myStorage.ReadAsync<UtteranceLog>(utteranceList).Result?.FirstOrDefault().Value;

                // If no stored messages were found, create and store a new entry.
                if (logItems is null)
                {
                    logItems = new UtteranceLog();
                }

                // add new message to list of messages to display.
                logItems.UtteranceList.Add(utterance);
                // increment turn counter.
                logItems.TurnNumber++;

                var changes = new Dictionary<string, object>();

                // show user new list of saved messages.
                //await turnContext.SendActivityAsync($"The list is now: {string.Join(", ", logItems.UtteranceList)}");

                // Create Dictionary object to hold new list of messages.

                {
                    changes.Add("UtteranceLog", logItems);
                };

                // Save new list to your Storage.
                await _myStorage.WriteAsync(changes, cancellationToken);

                // Perform a call to LUIS to retrieve results for the current activity message.
                var luisResults = await _services.LuisServices[LuisConfiguration].RecognizeAsync(dc.Context, cancellationToken).ConfigureAwait(false);

                // If any entities were updated, treat as interruption.
                // For example, "no my name is tony" will manifest as an update of the name to be "tony".
                var topScoringIntent = luisResults?.GetTopScoringIntent();

                var topIntent = topScoringIntent.Value.intent;

                // update greeting state with any entities captured
                //await UpdateGreetingState(luisResults, dc.Context);
                await UpdateMedicState(luisResults, dc.Context);


                // Handle conversation interrupts first.
                var interrupted = await IsTurnInterruptedAsync(dc, topIntent);
                if (interrupted)
                {
                    // Bypass the dialog.
                    // Save state before the next turn.
                    await _conversationState.SaveChangesAsync(turnContext);
                    await _userState.SaveChangesAsync(turnContext);
                    return;
                }

                // Continue the current dialog
                var dialogResult = await dc.ContinueDialogAsync();

                // if no one has responded,
                if (!dc.Context.Responded)
                {
                    // examine results from active dialog
                    switch (dialogResult.Status)
                    {
                        case DialogTurnStatus.Empty:
                            switch (topIntent)
                            {
                                case GreetingIntent:
                                    await dc.BeginDialogAsync(nameof(MedicDialog));
                                    break;

                                case MedicIntent:
                                    await dc.BeginDialogAsync(nameof(MedicDialog));
                                    break;

                                case NoneIntent:
                                default:
                                    // Help or no intent identified, either way, let's provide some help.
                                    // to the user
                                    await dc.Context.SendActivityAsync("I didn't understand what you just said to me.");
                                    break;
                            }

                            break;

                        case DialogTurnStatus.Waiting:
                            // The active dialog is waiting for a response from the user, so do nothing.
                            break;

                        case DialogTurnStatus.Complete:
                            await dc.EndDialogAsync();
                            break;

                        default:
                            await dc.CancelAllDialogsAsync();
                            break;
                    }
                }
            }
            else if (activity.Type == ActivityTypes.ConversationUpdate)
            {
                if (activity.MembersAdded.Any())
                {
                    // Iterate over all new members added to the conversation.
                    foreach (var member in activity.MembersAdded)
                    {
                        // Greet anyone that was not the target (recipient) of this message.
                        // To learn more about Adaptive Cards, see https://aka.ms/msbot-adaptivecards for more details.
                        if (member.Id != activity.Recipient.Id)
                        {
                            var welcomeCard = CreateAdaptiveCardAttachment();
                            var response = CreateResponse(activity, welcomeCard);
                            await dc.Context.SendActivityAsync(response).ConfigureAwait(false);
                        }
                    }
                }
            }

            await _conversationState.SaveChangesAsync(turnContext);
            await _userState.SaveChangesAsync(turnContext);
        }

        // Determine if an interruption has occured before we dispatch to any active dialog.
        private async Task<bool> IsTurnInterruptedAsync(DialogContext dc, string topIntent)
        {
            // See if there are any conversation interrupts we need to handle.
            if (topIntent.Equals(CancelIntent))
            {
                if (dc.ActiveDialog != null)
                {
                    await dc.CancelAllDialogsAsync();
                    await dc.Context.SendActivityAsync("Ok. I've cancelled our last activity.");
                    await _medicStateAccessor.DeleteAsync(dc.Context);
                }
                else
                {
                    await dc.Context.SendActivityAsync("I don't have anything to cancel.");
                }

                return true;        // Handled the interrupt.
            }

            if (topIntent.Equals(HelpIntent))
            {
                await dc.Context.SendActivityAsync("Let me try to provide some help.");
                await dc.Context.SendActivityAsync("I understand medic help, being asked for help, or being asked to cancel what I am doing.");
                if (dc.ActiveDialog != null)
                {
                    await dc.RepromptDialogAsync();
                }

                return true;        // Handled the interrupt.
            }

            return false;           // Did not handle the interrupt.
        }

        // Create an attachment message response.
        private Activity CreateResponse(Activity activity, Attachment attachment)
        {
            var response = activity.CreateReply();
            response.Attachments = new List<Attachment>() { attachment };
            return response;
        }

        // Load attachment from file.
        private Attachment CreateAdaptiveCardAttachment()
        {
            var adaptiveCard = File.ReadAllText(@".\Resources\welcomeCard.json");
            return new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(adaptiveCard),
                
            };
        }

        /// <summary>
        /// Helper function to update greeting state with entities returned by LUIS.
        /// </summary>
        /// <param name="luisResult">LUIS recognizer <see cref="RecognizerResult"/>.</param>
        /// <param name="turnContext">A <see cref="ITurnContext"/> containing all the data needed
        /// for processing this conversation turn.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        private async Task UpdateGreetingState(RecognizerResult luisResult, ITurnContext turnContext)
        {
            if (luisResult.Entities != null && luisResult.Entities.HasValues)
            {
                // Get latest GreetingState
                var greetingState = await _greetingStateAccessor.GetAsync(turnContext, () => new GreetingState());
                var entities = luisResult.Entities;

                // Supported LUIS Entities
                string[] userNameEntities = { "userName", "userName_paternAny" };
                string[] userLocationEntities = { "userLocation", "userLocation_patternAny" };

                // Update any entities
                // Note: Consider a confirm dialog, instead of just updating.
                foreach (var name in userNameEntities)
                {
                    // Check if we found valid slot values in entities returned from LUIS.
                    if (entities[name] != null)
                    {
                        // Capitalize and set new user name.
                        var newName = (string)entities[name][0];
                        greetingState.Name = char.ToUpper(newName[0]) + newName.Substring(1);
                        break;
                    }
                }

                foreach (var city in userLocationEntities)
                {
                    if (entities[city] != null)
                    {
                        // Captilize and set new city.
                        var newCity = (string)entities[city][0];
                        greetingState.City = char.ToUpper(newCity[0]) + newCity.Substring(1);
                        break;
                    }
                }

                // Set the new values into state.
                await _greetingStateAccessor.SetAsync(turnContext, greetingState);
            }
        }
        // update Medic State
        private async Task UpdateMedicState(RecognizerResult luisResult, ITurnContext turnContext)
        {
            if (luisResult.Entities != null && luisResult.Entities.HasValues)
            {
                // Get latest GreetingState
                var medicState = await _medicStateAccessor.GetAsync(turnContext, () => new MedicState());
                var entities = luisResult.Entities;

                // Supported LUIS Entities
                string[] medicComplainEntities = { "Internal_Medicine", "Opthalmology", "Paediatric" };
                string[] medicTimeSpanEntities = { "ComplaintDate" };
                string[] medicOtherComplaintEntities = { "" };
                string hospitalIdEntity = "HospitalId";
                string doctorIdEntity =  "DoctorId" ;
                string doctorAppointmentDate =  "DoctorAppointmentDate" ;
                // Update any entities
                // Note: Consider a confirm dialog, instead of just updating.
                foreach (var complaint in medicComplainEntities)
                {

                    // Check if we found valid slot values in entities returned from LUIS.
                    if (entities[complaint] != null && string.IsNullOrWhiteSpace(medicState.Complaint))
                    {
                    //    // Capitalize and set new user name.
                        
                        var newComplaint = (string)entities[complaint][0][0];
                        medicState.ComplaintCategory = complaint.Replace("_", " ");
                        medicState.Complaint = char.ToUpper(newComplaint[0]) + newComplaint.Substring(1);
                        break;
                    } 
                    
                }

                foreach (var time in medicTimeSpanEntities)
                {
                    if (entities[time] != null && string.IsNullOrWhiteSpace(medicState.TimeSpan))
                    {
                        //var jsonString = JObject.Parse(entities.ToString());
                        //var likes = jsonString["datetime"]["text"].ToString();


                        // Captilize and set new city.
                        var newtime = (string)entities["ComplaintDate"][0]["timex"][0];
                       
                        medicState.TimeSpan = newtime;
                        break;
                    }
                }

                foreach (var otherComplaint in medicOtherComplaintEntities)
                {
                    if (entities[otherComplaint] != null && string.IsNullOrWhiteSpace(medicState.OtherSymptoms))
                    {
                        //var jsonString = JObject.Parse(entities.ToString());
                        //var likes = jsonString["datetime"]["text"].ToString();


                        // Captilize and set new city.
                        var newtime = (string)entities["$instance"]["datetime"][0]["text"];

                        //medicState.TimeSpan = newtime;
                        break;
                    }
                }

                if (entities[hospitalIdEntity] != null && string.IsNullOrWhiteSpace(medicState.HospitalID))
                {
                    var hospitalID = (string)entities["$instance"][hospitalIdEntity][0]["text"];
                    medicState.HospitalID = hospitalID;

                    if (entities[doctorIdEntity] != null && string.IsNullOrWhiteSpace(medicState.DoctorID))
                    {
                        var doctorId = (string)entities["$instance"][doctorIdEntity][0]["text"];
                        medicState.DoctorID = doctorId;
                    }

                    
                }
                if (entities[doctorAppointmentDate] != null && medicState.DoctorApointmentDate < DateTime.Now)
                {
                    var appointmentDate = (DateTime)entities["$instance"][doctorAppointmentDate][0]["text"];
                    medicState.DoctorApointmentDate = appointmentDate;
                }


                // Set the new values into state.
                await _medicStateAccessor.SetAsync(turnContext, medicState);
            }
        }
    }
}
