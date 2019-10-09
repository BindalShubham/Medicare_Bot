using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BasicBot.Resources.API_Classes;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace BasicBot.Dialogs.Medic
{
    public class MedicDialog : ComponentDialog
    {
        
        // User state for  dialog
        private const string ComplaintStateProperty = "complaintState";
        private const string Complaint = "complaintState";
        private const string TimeSpan = "complaintTimeSpan";
        private const string OtherSymptoms = "complaintOtherSymptoms";
        private const string OtherSymptoms2 = "complaintOtherSymptoms";

        // Prompts names
        private const string ComplaintPrompt = "complaintPrompt";
        private const string ComplaintTSPrompt = "complaintTSPrompt";
        private const string OtherComplaintPrompt = "otherComplaintPrompt";
        private const string OtherComplaintPrompt2 = "otherComplaintPrompt2";
        private const string GetSpecialitiesStepAsync = "getSpecialitiesStepAsync";
        private const string GetDoctorTimeStepAsync = "getDoctorTimeStepAsync";
        private const string BookAppointmentPrompt = "bookAppointmentPrompt";


        // Dialog IDs
        private const string ProfileDialog = "profileDialog";

        public MedicDialog(Microsoft.Bot.Builder.IStatePropertyAccessor<MedicState> userProfileStateAccessor, ILoggerFactory loggerFactory)
           : base(nameof(MedicDialog))
        {
            UserProfileAccessor = userProfileStateAccessor ?? throw new ArgumentNullException(nameof(userProfileStateAccessor));

            var waterfallSteps = new WaterfallStep[]
            {
                    InitializeStateStepAsync,
                    PromptForComplaintStepAsync,
                    PromptForTSStepAsync,
                    PromptForOtherComplaintStepAsync,
                    PromptForOtherComplaint2StepAsync,
                    PromptForGetSpecialitiesStepAsync,
                    PromptForDoctorTimeStepAsync,
                    PromptForBookAppointmentStepAsync,
                    //      DisplayGreetingStateStepAsync,
            };

            AddDialog(new WaterfallDialog(ProfileDialog, waterfallSteps));
            AddDialog(new TextPrompt(ComplaintPrompt, ValidateComplaint));
            AddDialog(new TextPrompt(ComplaintTSPrompt, ValidateTS));
            AddDialog(new TextPrompt(OtherComplaintPrompt));
            AddDialog(new TextPrompt(OtherComplaintPrompt2));
            AddDialog(new TextPrompt(GetSpecialitiesStepAsync, ValidateDate));
            AddDialog(new TextPrompt(GetDoctorTimeStepAsync));
            AddDialog(new TextPrompt(BookAppointmentPrompt));
        }

        public IStatePropertyAccessor<MedicState> UserProfileAccessor { get; }

        private async Task<DialogTurnResult> InitializeStateStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var medicState = await UserProfileAccessor.GetAsync(stepContext.Context, () => null);
            if (medicState == null)
            {
                var medicStateOpt = stepContext.Options as MedicState;
                if (medicStateOpt != null)
                {
                    await UserProfileAccessor.SetAsync(stepContext.Context, medicStateOpt);
                }
                else
                {
                    await UserProfileAccessor.SetAsync(stepContext.Context, new MedicState());
                }
            }

            return await stepContext.NextAsync();
        }

        private async Task<DialogTurnResult> PromptForComplaintStepAsync(
                                                WaterfallStepContext stepContext,
                                                CancellationToken cancellationToken)
        {
            var medicState = await UserProfileAccessor.GetAsync(stepContext.Context);

            // if we have everything we need, greet user and return.
            if (medicState != null && !string.IsNullOrWhiteSpace(medicState.Complaint) && !string.IsNullOrWhiteSpace(medicState.OtherSymptoms) && !string.IsNullOrWhiteSpace(medicState.TimeSpan))
            {
                //return await GreetUser(stepContext);
            }

            if (string.IsNullOrWhiteSpace(medicState.Complaint))
            {
                // prompt for name, if missing
                var opts = new PromptOptions
                {
                    Prompt = new Activity
                    {
                        Type = ActivityTypes.Message,
                        Text = "What is the problem you are facing?",
                        SuggestedActions = new SuggestedActions()
                        {
                            Actions = new List<CardAction>()
                            {
                                new CardAction(type: ActionTypes.ImBack, title: "Oral Ulcers", value: "Ulcers"),
                                new CardAction(type: ActionTypes.ImBack, title: "Visual defect", value: "Visual defect"),
                                new CardAction(type: ActionTypes.ImBack, title: "Dyspepsia", value: "Dyspepsia"),
                                new CardAction(type: ActionTypes.ImBack, title: "Fever", value: "Fever"),
                            },
                        },
                    },

                };
                return await stepContext.PromptAsync(ComplaintPrompt, opts);
            }
            else
            {
                return await stepContext.NextAsync();
            }
        }

        private async Task<DialogTurnResult> PromptForTSStepAsync(
                                                       WaterfallStepContext stepContext,
                                                       CancellationToken cancellationToken)
        {
            // Save name, if prompted.
            var medicState = await UserProfileAccessor.GetAsync(stepContext.Context);
            var lowerCaseCompaint = stepContext.Result as string;
            if (string.IsNullOrWhiteSpace(medicState.Complaint) && lowerCaseCompaint != null)
            {
                // Capitalize and set name.
                //medicState.Complaint = char.ToUpper(lowerCaseCompaint[0]) + lowerCaseCompaint.Substring(1);
                //await UserProfileAccessor.SetAsync(stepContext.Context, medicState);
            }

            if (string.IsNullOrWhiteSpace(medicState.TimeSpan))
            {
                var opts = new PromptOptions
                {
                    Prompt = new Activity
                    {
                        Type = ActivityTypes.Message,
                        Text = $"from how long are you facing {medicState.Complaint}",
                        SuggestedActions = new SuggestedActions()
                        {
                            Actions = new List<CardAction>()
                            {
                                new CardAction(type: ActionTypes.ImBack, title: "from yesterday", value: "from yesterday"),
                                new CardAction(type: ActionTypes.ImBack, title: "been 2 days", value: "been 2 days"),
                                new CardAction(type: ActionTypes.ImBack, title: "from last week", value: "from last week"),
                                new CardAction(type: ActionTypes.ImBack, title: "last monday evening", value: "last monday evening"),
                            },
                        },
                    },

                    RetryPrompt = new Activity
                    {
                        Type = ActivityTypes.Message,
                        Text = $"can please tell me correctly how long you been facing {medicState.Complaint}",
                    },

                };

                return await stepContext.PromptAsync(ComplaintTSPrompt, opts);
            }
            else
            {
                return await stepContext.NextAsync();
            }
        }

        private async Task<DialogTurnResult> PromptForOtherComplaintStepAsync(
                                                      WaterfallStepContext stepContext,
                                                      CancellationToken cancellationToken)
        {
            // Save name, if prompted.
            var medicState = await UserProfileAccessor.GetAsync(stepContext.Context);
            var lowerCaseCompaint = stepContext.Result as string;
            if (string.IsNullOrWhiteSpace(medicState.TimeSpan) && lowerCaseCompaint != null)
            {
                // Capitalize and set name.
                medicState.TimeSpan = lowerCaseCompaint;
                await UserProfileAccessor.SetAsync(stepContext.Context, medicState);
            }

            if (medicState.OtherSymptoms == null)
            {
                var otherComplaintsCard = CreateAdaptiveCardAttachmentForOtherComplaints();
                var opts = new PromptOptions
                {
                    Prompt = new Activity
                    {
                        Type = ActivityTypes.Message,
                        Text = $"Do you have other troubles too apart from {medicState.Complaint}",
                        //AttachmentLayout = AttachmentLayoutTypes.List,
                        Attachments = new List<Attachment> { otherComplaintsCard },
                        //SuggestedActions = new SuggestedActions()
                        //{
                        //    Actions = new List<CardAction>()
                        //    {
                        //        new CardAction(type: ActionTypes.ImBack, title: "Abdominal Pain", value: "Addition Symptoms: Abdominal Pain"),
                        //        new CardAction(type: ActionTypes.ImBack, title: "Joint Pain", value: "Addition Symptoms: Joint Pain"),
                        //        new CardAction(type: ActionTypes.ImBack, title: "Sore Throat", value: "Addition Symptoms: Sore Throat"),
                        //        new CardAction(type: ActionTypes.ImBack, title: "None of these", value: "Addition Symptoms: None"),
                        //    },

                        //},
                    },
                };
                return await stepContext.PromptAsync(OtherComplaintPrompt, opts);
            }
            else
            {
                return await stepContext.NextAsync();
            }
        }

        private async Task<DialogTurnResult> PromptForOtherComplaint2StepAsync(
                                                      WaterfallStepContext stepContext,
                                                      CancellationToken cancellationToken)
        {
            // Save name, if prompted.
            var medicState = await UserProfileAccessor.GetAsync(stepContext.Context);
            var lowerCaseCompaint = stepContext.Result as string;
            if (string.IsNullOrWhiteSpace(medicState.OtherSymptoms) && lowerCaseCompaint != null)
            {
                // Capitalize and set name.
                medicState.OtherSymptoms = lowerCaseCompaint;
                await UserProfileAccessor.SetAsync(stepContext.Context, medicState);
            }

            if (medicState.OtherSymptoms2 == null)
            {
                var otherComplaintsCard = CreateAdaptiveCardAttachmentForOtherComplaints2();
                var opts = new PromptOptions
                {
                    Prompt = new Activity
                    {
                        Type = ActivityTypes.Message,
                        Text = $"Do you have other troubles too apart from {medicState.Complaint}",
                        //AttachmentLayout = AttachmentLayoutTypes.List,
                        Attachments = new List<Attachment> { otherComplaintsCard },
                        //SuggestedActions = new SuggestedActions()
                        //{
                        //    Actions = new List<CardAction>()
                        //    {
                        //        new CardAction(type: ActionTypes.ImBack, title: "Difficult and painful Swallowing", value: "More Symptoms: Difficult and painful Swallowing"),
                        //        new CardAction(type: ActionTypes.ImBack, title: "Runny Nose", value: "More Symptoms: Runny Nose"),
                        //        new CardAction(type: ActionTypes.ImBack, title: "Chest Pain", value: "More Symptoms: Chest Pain"),
                        //        new CardAction(type: ActionTypes.ImBack, title: "None of these", value: "More Symptoms: None"),
                        //    },

                        //},
                    },
                };
                return await stepContext.PromptAsync(OtherComplaintPrompt, opts);
            }
            else
            {
                return await stepContext.NextAsync();
            }
        }

        private async Task<DialogTurnResult> PromptForGetSpecialitiesStepAsync(
                                                      WaterfallStepContext stepContext,
                                                      CancellationToken cancellationToken)
        {
            var medicState = await UserProfileAccessor.GetAsync(stepContext.Context);
            var lowerCaseCompaint = stepContext.Result as string;
            if (string.IsNullOrWhiteSpace(medicState.OtherSymptoms2) && lowerCaseCompaint != null)
            {
                // Capitalize and set name.
                medicState.OtherSymptoms2 = lowerCaseCompaint;
                await UserProfileAccessor.SetAsync(stepContext.Context, medicState);
            }

            using (var client = new HttpClient())
            {
                //client.BaseAddress = new Uri("http://mimsyshis.southindia.cloudapp.azure.com");
                //client.DefaultRequestHeaders.Accept.Clear();
                //client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // POST Method
                // Getting the SpecialtyId
                //HttpResponseMessage response = await client.PostAsJsonAsync("/PATIENTAPP/PatAppMasters/GetSpecility", "{}");
                HttpRequestMessage httpRequest1 = new HttpRequestMessage(HttpMethod.Post, new Uri("http://mimsyshis.southindia.cloudapp.azure.com/PATIENTAPP/PatAppMasters/GetSpecility"));
                httpRequest1.Content = new StringContent("{}", Encoding.UTF8, "application/json");
                var response = await client.SendAsync(httpRequest1).ConfigureAwait(false);

                string tempSpecialtyId = "";

                if (response.IsSuccessStatusCode)
                {
                    var value = await response.Content.ReadAsAsync<RootGetSpecility>();
                    foreach (var v in value.lstspecility)
                    {
                        if(v.strSPECDESC.ToLower() == medicState.ComplaintCategory.ToLower())
                        {
                            tempSpecialtyId = v.strSPECID.ToString();
                            break;
                        }
                    }

                    // Get the URI of the created resource.
                }
                
                else
                {
                    Console.Write("Issue with Specialty List");
                }

                // Getting the DoctorList
                string _strSERVURL = "http://apps.azhd.ae/PATIENTAPP/";
               
                var docListInput = new DoctorListInput() { specIds = tempSpecialtyId };

                HttpClient httpClient = new HttpClient();
                //string a = "{ \"strSpecialityDoctor\":\"00945\"}";
                HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, new Uri("http://mimsyshis.southindia.cloudapp.azure.com/PATIENTAPP/SearchDoctors/getNewDoctorList"));
                httpRequest.Content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(docListInput), Encoding.UTF8, "application/json");
                var httpResponse = await httpClient.SendAsync(httpRequest).ConfigureAwait(false);

                if (httpResponse.IsSuccessStatusCode)
                {

                    var value = await httpResponse.Content.ReadAsAsync<List<DoctorInfo>>();
                    var doctorsCard = CreateAdaptiveCardAttachment(value);
                    //var response = CreateResponse(stepContext.Context.Activity, doctorsCard);

                    var opts = new PromptOptions
                    {
                        
                        Prompt = new Activity
                        {
                            Type = ActivityTypes.Message,
                            Text = $"Please choose the doctor of your preference",
                            AttachmentLayout = AttachmentLayoutTypes.Carousel,
                            Attachments = doctorsCard,
                        },
                    };
                    return await stepContext.PromptAsync(GetSpecialitiesStepAsync, opts);
                }
                else
                {
                    Console.Write("Issue with Doctor List");
                    return await stepContext.NextAsync();
                } 
            }
        }

        private async Task<DialogTurnResult> PromptForDoctorTimeStepAsync(
                                                      WaterfallStepContext stepContext,
                                                      CancellationToken cancellationToken)
        {
            var medicState = await UserProfileAccessor.GetAsync(stepContext.Context);

            using (HttpClient httpClient = new HttpClient())
            {
                // POST Method
                // Getting the DoctorTime
                var _dtSelectedDay = medicState.DoctorApointmentDate.ToString();
                var _strDoctorid = medicState.DoctorID;
                var _strHospitalId = medicState.HospitalID;

                var docTimeInput = new DoctorTimeInput() { dtSelectedDay = _dtSelectedDay, strDoctorid = _strDoctorid , strHospitalId = _strHospitalId };
                HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, new Uri("http://mimsyshis.southindia.cloudapp.azure.com/PATIENTAPP/SearchDoctors/getDoctorDailySlot"));
                httpRequest.Content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(docTimeInput), Encoding.UTF8, "application/json");
                var httpResponse = await httpClient.SendAsync(httpRequest).ConfigureAwait(false);

                if (httpResponse.IsSuccessStatusCode)
                {

                    var value = await httpResponse.Content.ReadAsAsync<DoctorTimeOutput>();
                    var doctorsCard = CreateAdaptiveCardAttachmentForTimeSlot(value);
                    //var response = CreateResponse(stepContext.Context.Activity, doctorsCard);
                    var actionList = new List<CardAction> { };

                    foreach (var v in value.objDoctorTimeSlots)
                    {
                        actionList.Add(new CardAction(type: ActionTypes.ImBack, title: v.strShowSlot, value: v.intSlotId , displayText: v.strSlotDesc));
                    }

                    var opts = new PromptOptions
                    {
                        Prompt = new Activity
                        {
                            Type = ActivityTypes.Message,
                            Text = $"Please choose the time for the doctor!",
                            // AttachmentLayout = AttachmentLayoutTypes.Carousel,
                            // Attachments = new List<Attachment>() {doctorsCard },
                            SuggestedActions = new SuggestedActions()
                            {
                                Actions = actionList,
                            },
                        },
                    };
                    return await stepContext.PromptAsync(GetDoctorTimeStepAsync, opts);
                }
                else
                {
                    Console.Write("Issue with Doctor timing");
                    return await stepContext.NextAsync();
                }
            }
        }

        private async Task<DialogTurnResult> PromptForBookAppointmentStepAsync(
                                                      WaterfallStepContext stepContext,
                                                      CancellationToken cancellationToken)
        {
            var medicState = await UserProfileAccessor.GetAsync(stepContext.Context);
            var selectedTimeSlot = stepContext.Result as string;
            //if (string.IsNullOrWhiteSpace(medicState.OtherSymptoms2) && lowerCaseCompaint != null)
            //{
                //// Capitalize and set name.
                //medicState.OtherSymptoms2 = lowerCaseCompaint;
                //await UserProfileAccessor.SetAsync(stepContext.Context, medicState);
           // }

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://mimsyshis.southindia.cloudapp.azure.com");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Getting the DoctorList
                string _strHospitalId = medicState.HospitalID;
                string _strDOCID = medicState.DoctorID;
                string _strSPECIALITYDESC = medicState.ComplaintCategory;
                string _strTIMESLOT = selectedTimeSlot;
                DateTime _dtAPPDATE = medicState.DoctorApointmentDate;

                var doctorBookInput = new DoctorBookInput() { strHospitalId = _strHospitalId, strDOCID = _strDOCID, strSPECIALITYDESC = _strSPECIALITYDESC, strTIMESLOT = _strTIMESLOT, dtAPPDATE = _dtAPPDATE, intAPPSTATUS = "", intHAVEUHID = "", intPATGENDER = "" , intPATTITLE = "" , strCityId = "", strDOCNAME = "", strEMAILID = "", strHOMEPHONE = "", strMOBILENO = "", strPATFIRSTNAME = "", strPATLASTNAME = "", strPATMIDNAME = "", strREMARKS = "" , strSPECID = "", strUHID = "" };

                HttpClient httpClient = new HttpClient();
                HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, new Uri("http://mimsyshis.southindia.cloudapp.azure.com/PATIENTAPP/SearchDoctors/NewbookAppointment"));
                httpRequest.Content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(doctorBookInput), Encoding.UTF8, "application/json");
                var httpResponse = await httpClient.SendAsync(httpRequest).ConfigureAwait(false);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var value = await httpResponse.Content.ReadAsAsync<DoctorBookOutput>();
                    if (!string.IsNullOrWhiteSpace(value.strError))
                    {
                        
                        var opts = new PromptOptions
                        {
                            Prompt = new Activity
                            {
                                Type = ActivityTypes.Message,
                                Text = value.strError,
                                //AttachmentLayout = AttachmentLayoutTypes.Carousel,
                                //Attachments = doctorsCard,
                            },
                        };
                        return await stepContext.PromptAsync(BookAppointmentPrompt, opts);
                    }
                    else
                    {
                        string outputMessage = value.strMessage;
                        if (string.IsNullOrWhiteSpace(value.strMessage))
                        {
                             outputMessage = "There is no response from API for Doctors Appointment.";
                        }

                        var opts = new PromptOptions
                        {
                            Prompt = new Activity
                            {
                                Type = ActivityTypes.Message,
                                Text = outputMessage,
                                //AttachmentLayout = AttachmentLayoutTypes.Carousel,
                                //Attachments = doctorsCard,
                            },
                        };
                        return await stepContext.PromptAsync(BookAppointmentPrompt, opts);
                    }
                }
                else
                {
                    Console.Write("Issue with doctor appointment.");
                    return await stepContext.NextAsync();
                }
            }
        }

        // Helper function to greet user with information in GreetingState.
        private async Task<DialogTurnResult> GreetUser(WaterfallStepContext stepContext)
        {
            var context = stepContext.Context;
            var medicState = await UserProfileAccessor.GetAsync(context);

            // Display their profile information and end dialog.
            await context.SendActivityAsync($"Hi {medicState.Complaint}, from {medicState.TimeSpan}, nice to meet you!");
            return await stepContext.EndDialogAsync();
        }

        private async Task<bool> ValidateComplaint(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            var medicState = await UserProfileAccessor.GetAsync(promptContext.Context);
            // If any entities were updated, treat as interruption.
            // For example, "no my name is tony" will manifest as an update of the name to be "tony".
            //var topScoringIntent = luisResults?.GetTopScoringIntent();
            // Validate that the user entered a minimum length for their name.
            var value = promptContext.Recognized.Value?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(medicState.Complaint))
            {
                promptContext.Recognized.Value = value;
                return true;
            }
            else
            {
                await promptContext.Context.SendActivityAsync($"this is not a valid issue").ConfigureAwait(false);
                return false;
            }
        }

        private async Task<bool> ValidateDate(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            var medicState = await UserProfileAccessor.GetAsync(promptContext.Context);
            // If any entities were updated, treat as interruption.
            // For example, "no my name is tony" will manifest as an update of the name to be "tony".
            //var topScoringIntent = luisResults?.GetTopScoringIntent();
            // Validate that the user entered a minimum length for their name.

            HttpClient httpClient = new HttpClient();
            HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, new Uri("http://mimsyshis.southindia.cloudapp.azure.com/PATIENTAPP/SearchDoctors/getDoctorInfo"));
            httpRequest.Content = new StringContent($"{{\"strHospitalId\":\"{medicState.HospitalID}\",\"strDoctorid\":\"{medicState.DoctorID}\"}}", Encoding.UTF8, "application/json");
            var httpResponse = await httpClient.SendAsync(httpRequest).ConfigureAwait(false);
            var value = await httpResponse.Content.ReadAsAsync<DoctorInfo>();

            if (httpResponse.IsSuccessStatusCode && value.strWorkingHours.Contains(medicState.DoctorApointmentDate.DayOfWeek.ToString()) && medicState.DoctorApointmentDate != new DateTime())
            {
                return true;

            }
            else
            {   
                await promptContext.Context.SendActivityAsync($"the date you have selected is not valid. Please try again!").ConfigureAwait(false);
                medicState.DoctorApointmentDate = new DateTime();
                await UserProfileAccessor.SetAsync(promptContext.Context, medicState);
                //await _medicStateAccessor.SetAsync(turnContext, medicState);
                return false;
            }






            //var value = promptContext.Recognized.Value?.Trim() ?? string.Empty;
            //if (!string.IsNullOrWhiteSpace(medicState.Complaint))
            //{
            //    promptContext.Recognized.Value = value;
            //    return true;
            //}
            //else
            //{
            //    await promptContext.Context.SendActivityAsync($"this is not a valid issue").ConfigureAwait(false);
            //    return false;
            //}
        }


        private Attachment CreateAdaptiveCardAttachmentForTimeSlot(DoctorTimeOutput value)
        {
            var adaptiveCard = File.ReadAllText(@".\JSONCards\doctorTimeInput.json");
                //adaptiveCard = adaptiveCard.Replace("DOCTORNAME_VALUE", value.strDOCNAME.Trim());
                //adaptiveCard = adaptiveCard.Replace("DOCTORID_VALUE", v.strDOCID.Trim());
                //adaptiveCard = adaptiveCard.Replace("TODAYDATE_VALUE", $"{DateTime.Now.Year}-{DateTime.Now.Month}-{DateTime.Now.Date}");

            var attachment = new Attachment
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(adaptiveCard),
            };

            return attachment;
        }

        private Attachment CreateAdaptiveCardAttachmentForOtherComplaints()
        { 
            var adaptiveCard = File.ReadAllText(@".\JSONCards\otherComplaints.json");
            //adaptiveCard = adaptiveCard.Replace("DOCTORNAME_VALUE", value.strDOCNAME.Trim());
            //adaptiveCard = adaptiveCard.Replace("DOCTORID_VALUE", v.strDOCID.Trim());
            //adaptiveCard = adaptiveCard.Replace("TODAYDATE_VALUE", $"{DateTime.Now.Year}-{DateTime.Now.Month}-{DateTime.Now.Date}");

            var attachment = new Attachment
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(adaptiveCard),
            };

            return attachment;
        }

        private Attachment CreateAdaptiveCardAttachmentForOtherComplaints2()
        {
            var adaptiveCard = File.ReadAllText(@".\JSONCards\otherComplaint2.json");

            var attachment = new Attachment
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(adaptiveCard),
            };

            return attachment;
        }

        private List<Attachment> CreateAdaptiveCardAttachment(List<DoctorInfo> value)
        {
            var attachmentList = new List<Attachment>();
            foreach (var v in value)
            {
                var adaptiveCard = File.ReadAllText(@".\JSONCards\Doctors.json");
                adaptiveCard = adaptiveCard.Replace("DOCTORNAME_VALUE", v.strDOCNAME.Trim());
                adaptiveCard = adaptiveCard.Replace("DOCTORID_VALUE", v.strDOCID.Trim());
                adaptiveCard = adaptiveCard.Replace("HOSPITALID_VALUE", v.strHospitalId.Trim());
                adaptiveCard = adaptiveCard.Replace("DESIGNATION_VALUE", v.strDESIGNATION.Trim());
                adaptiveCard = adaptiveCard.Replace("SPECIALITY_VALUE", v.strSPECID.Trim());
                adaptiveCard = adaptiveCard.Replace("LANGUAGES_VALUE", v.strLANGUAGES.Trim());
                adaptiveCard = adaptiveCard.Replace("WORKINGHOURS_VALUE", v.strWorkingHours.Trim());
                adaptiveCard = adaptiveCard.Replace("TODAYDATE_VALUE", $"{DateTime.Now.Year}-{DateTime.Now.Month}-{DateTime.Now.Date}");

                attachmentList.Add(new Attachment
                {
                    ContentType = "application/vnd.microsoft.card.adaptive",
                    Content = JsonConvert.DeserializeObject(adaptiveCard),
                });
            }

            return attachmentList;
        }

        // Create an attachment message response.
        private Activity CreateResponse(Activity activity, Attachment attachment)
        {
            var response = activity.CreateReply();
            response.Attachments = new List<Attachment>() { attachment };
            return response;
        }

        private async Task<bool> ValidateTS(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            // Validate that the user entered a minimum length for their name.
            var medicState = await UserProfileAccessor.GetAsync(promptContext.Context);
            var value = promptContext.Recognized.Value?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(medicState.TimeSpan))
            {
                promptContext.Recognized.Value = value;
                return true;
            }
            else
            {
                await promptContext.Context.SendActivityAsync($"This doesnt look valid time period to me, please try again.").ConfigureAwait(false);
                return false;
            }
        }
    }
}
