﻿using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ReloChatBot.Models
{
    public class LodgingBot : LuisParser
    {
        // Spot the python programmer again...

        // The only way to get this to work, and not inherit the RESULTS from LuisParser (parent class)
        // Is to plug in api_endpoint as one of the paramters, and set a default

        public LuisParser masterbot;

        public LodgingBot(Activity activity, string api_endpoint = "https://api.projectoxford.ai/luis/v1/application?id=8547ee95-9496-43ca-a9a2-b583da92cd7e&subscription-key=6171c439d26540d6a380208a16b31958&q=") : base(activity, api_endpoint) { }
        public override string Reply
        {
            get { return this.GetReply(); }
        }

        private string GetReply()
        {
            LodgingBotFunctionality functionality = new LodgingBotFunctionality(this.Intent, this.activity, this.masterbot);
            return functionality.Reply;
        }

        public void Seed(LuisParser masterbot)
        {
            this.masterbot = masterbot;
        }

    }

    /// <summary>
    /// All the functionality of the bot
    /// </summary>
    public class LodgingBotFunctionality
    {

        private QuestionModel createQuestionBranching()
        {
            string yes = "PositiveConfirmation";
            string no = "NegativeConfirmation";
            QuestionModel root = new QuestionModel("Currently, Microsoft does not provide relocation services. Are you still interested in relocating?", this.masterbot); // 1
            QuestionModel AskFamiliarity = new QuestionModel("Are you familiar with the Pacific Northwest?", this.masterbot); // 3
            QuestionModel UserCityPreference = new QuestionModel("Do you have a paticular city in mind?", this.masterbot); // 4 // This should be Lobot determining the Intent.
            QuestionModel ProvideHelp = new QuestionModel("Would you like help in narrowing down a place to temporarily lodge?", this.masterbot); // 5
            QuestionModel CarAccess = new QuestionModel("Do you have access to car?", this.masterbot); // 6
            QuestionModel BusAccess = new QuestionModel("Do you want to live near a bus stop? (Microsoft will provide free bus passes to you)", this.masterbot); // 7
            QuestionModel CommuteMax = new QuestionModel("What is the maximum you're willing to commute?", this.masterbot); // 8
            QuestionModel CommuteOrRent = new QuestionModel("Which is more important to you? Rent or Commute?", this.masterbot); // 9 // This shsould be lobot as well
            QuestionModel RentRange = new QuestionModel("What is the range you're willing to pay for rent? (The average is about $2000)", this.masterbot); // 10

            /*
             * 
             * Theoretically, the branches could be generated on the go. Why not? So we take the existing state,
             * check it. Set it. Then proceed to the next branch
             */

            root.AddBranch(yes, ProvideHelp);
            //root.AddBranch(no, "idek");

            AskFamiliarity.AddBranch(yes, UserCityPreference);
            AskFamiliarity.AddBranch(no, CommuteOrRent);

            //UserCityPreference.AddBranch(yes, some logic here) to either 6 or 10
            UserCityPreference.AddBranch(no, CommuteOrRent);

            ProvideHelp.AddBranch(yes, AskFamiliarity);
            //ProvideHelp.AddBranch(no, "idek");

            CarAccess.AddBranch(yes, CommuteMax);
            CarAccess.AddBranch(no, BusAccess);

            BusAccess.AddBranch(yes, CommuteMax);
            BusAccess.AddBranch(no, CommuteMax);

            //CommuteMax.AddBranch("all"); // if rent was asked: goto 11, else goto RentRange

            //RentRange.AddBranch(); if commute was asked: goto CarAccess else goto 11

            return root;
        }

        private string intent;
        private Activity activity;
        /// <summary>
        /// Temporary variable
        /// </summary>
        private Dictionary<string, string> actions = new Dictionary<string, string>()
        {
            { "DetermineRelocationCost", "The average cost is ..." },
            { "DetermineMoveIsViable", "I think moving for you is a _ idea." },
            { "ElevatorPitch", "Here's the predicament you're in." },
            { "LeapLocationQuestion", "Leap is located in building 86" },
            { "LocationRecomendation", "I think you should move to ..." },
            { "LeapRelocationServices", "At the moment, LEAP will not compensate for any relocation expenses. I can try and help you determine costs though. Would you like to proceed?" },
        };

        private string[] QuestionArray = {
            "",
            "Are you interested in Relocating?",
            "Oh cool! Can I help you narrow down your choices?",
        };

        private LuisParser masterbot;

        /*
         * BotStates:
         * string ClientCurrentLocation: Where the client currently claims to live.
         * int ClientRentTarget: How much the client can currently afford.
         * int CommuteTimeRange: Maximum time the user is okay commuting;
         * bool AccessToCar: Does the user have access to a car;
         * string CityPreference: User's current city preference;
         * bool ClientInterestedInRelo: User wants to use relocation services;
         * bool AskedClientAboutRelo: User already Declined;
         * int LastQuestion: A key of the last question that was asked;
         */

        /// <summary>
        /// Where all the magic happens, where state will be kept and whatnot.
        /// </summary>
        /// <param name="intent"></param>
        /// <param name="activity"></param>
        /// <param name="masterbot"></param>
        public LodgingBotFunctionality(string intent, Activity activity, LuisParser masterbot)
        {
            this.intent = intent;
            this.activity = activity;
            this.masterbot = masterbot;
        }

        public string Reply
        {
            get
            {
                return this.GetReply();
            }
        }

        /// <summary>
        /// All these GET/SET stuff is for maintianing the State of the bot
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private string GetProperty(string key)
        {
            StateClient stateClient = this.activity.GetStateClient();
            BotData userData = stateClient.BotState.GetUserData(this.activity.ChannelId, this.activity.From.Id);
            return userData.GetProperty<string>(key);
        }

        private bool GetBoolProperty(string key)
        {
            StateClient stateClient = this.activity.GetStateClient();
            BotData userData = stateClient.BotState.GetUserData(this.activity.ChannelId, this.activity.From.Id);
            return userData.GetProperty<bool>(key);
        }

        private int GetIntProperty(string key)
        {
            StateClient stateClient = this.activity.GetStateClient();
            BotData userData = stateClient.BotState.GetUserData(this.activity.ChannelId, this.activity.From.Id);
            return userData.GetProperty<int>(key);
        }

        private void SetProperty(string key, string value)
        {
            StateClient stateClient = this.activity.GetStateClient();
            BotData userData = stateClient.BotState.GetUserData(this.activity.ChannelId, this.activity.From.Id);
            userData.SetProperty<string>(key, value);
            stateClient.BotState.SetUserData(this.activity.ChannelId, this.activity.From.Id, userData);
        }

        private void SetProperty(string key, bool value)
        {
            StateClient stateClient = this.activity.GetStateClient();
            BotData userData = stateClient.BotState.GetUserData(this.activity.ChannelId, this.activity.From.Id);
            userData.SetProperty<bool>(key, value);
            stateClient.BotState.SetUserData(this.activity.ChannelId, this.activity.From.Id, userData);
        }

        private void SetProperty(string key, int value)
        {
            StateClient stateClient = this.activity.GetStateClient();
            BotData userData = stateClient.BotState.GetUserData(this.activity.ChannelId, this.activity.From.Id);
            userData.SetProperty<int>(key, value);
            stateClient.BotState.SetUserData(this.activity.ChannelId, this.activity.From.Id, userData);
        }

        /// <summary>
        /// Generate a reply
        /// </summary>
        /// <returns>The string to be shipped to the client</returns>
        private string GetReply()
        {
            /*
             * key meanings:
             * 1: does user want to proceed?
             */

            //QuestionModel question = this.createQuestionBranching();

            //string yes = "PositiveConfirmation";
            //string no = "NegativeConfiramtion";
            //string wat = "I did not get that.";

            return "At the moment, Leap does not offer Relocation services. However, we can help narrow down your search for temporary lodging. " +
                   "Here's a link to [Redmond Apartments](https://www.forrent.com/find/WA/metro-Seattle/Redmond/price-Less+than+2000), "
                   + "[Seattle Apartments](https://www.forrent.com/find/WA/metro-Seattle/Seattle/price-Less+than+2000), " +
                   "[Bellevue Apartments](https://www.forrent.com/find/WA/metro-Seattle/Bellevue/price-Less+than+2000) and " +
                   "[Edmonds Apartments](https://www.forrent.com/find/WA/metro-Seattle/Edmonds/price-Less+than+2000).";

            //if (this.intent == "LeapRelocationServices")
            //{
               

            //    //this.SetProperty("LastQuestion", 1);
            //    //return question.question;
            //} else
            //{
            //    int lastquestion = this.GetIntProperty("LastQuestion");
            //    if (lastquestion == 1) 
            //    {
            //        // The last question was "Do you want help relocating?
            //        if (this.masterbot.Intent == yes)
            //        {
            //            question = question.GetBranch(yes);
            //            return question.question;
            //        } else if (this.masterbot.Intent == no)
            //        {
            //            //question = question.GetBranch(no);
            //            return "Alright, let me know if I can help later though.";
            //        } else
            //        {
            //            return wat;
            //        }
            //    }
            //    return "default";
            //}

            // Manipulate bot state and generate a reply

            //bool test = !this.GetBoolProperty("AskedClientAboutRelo");

            // If we haven't asked the client and relocation and the last question was not the relocation question
            //if (this.intent == "LeapRelocationServices")
            //{
            //    return this.actions[this.intent];
            //} else if (!this.GetBoolProperty("AskedClientAboutRelo") && this.GetIntProperty("LastQuestion") != 1)
            //{
            //    this.SetProperty("LastQuestion", 1);
            //    return this.QuestionArray[1];
            //// If the last question was the relocation question
            //} else if (this.GetIntProperty("LastQuestion") == 1)
            //{
            //    // and the user said yes...
            //    if (this.masterbot.Intent == "PositiveConfirmation")
            //    {
            //        this.SetProperty("LastQuestion", 2);
            //        this.SetProperty("AskedClientAboutRelo", true);
            //        this.SetProperty("ClientInterestedInRelo", true);
            //        return this.QuestionArray[1];
            //    // and if the user said no...
            //    } else if (this.masterbot.Intent == "NegativeConfirmation")
            //    {
            //        this.SetProperty("LastQuestion", 0);
            //        this.SetProperty("AskedClientAboutRelo", true);
            //        this.SetProperty("ClientInterestedInRelo", false);
            //        return "Aww. Well sorry about that.";
            //    // if the user doesn't make sense...
            //    } else
            //    {
            //        return "Come again?";
            //    }
            //// if the user is interested in relocation help...
            //} else if (this.GetBoolProperty("ClientInterestedInRelo")) {
            //    return "I am going to help you find a new place.";
            //// and if all else fails...
            //} else
            //{
            //    this.SetProperty("LastQuestion", -1);
            //    return "default";
            //}

        }
    }

}
