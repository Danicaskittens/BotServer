﻿using Microsoft.Bot.Connector;
using MovieBot.Contract;
using MovieBot.Utility;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;

namespace MovieBot.States
{
    /// <summary>
    /// <see cref="SearchState"/> designed for handling the "Search Cinema" request
    /// </summary>
    public class SearchCinemaState : SearchState
    {
        public List<Cinema> cinemaList { get; set; }
        public Point locationFound { get; set; }
        public DateTime dateChoosen { get; set; }
        public int StateNum { get; set; }
        public List<Location> locationList { get; set; }

        /// <inheritdoc />
        public override StateReply getReplay(string userInput)
        {
            switch (StateNum)
            {
                case 0:
                    StateReply reply = stateZero(userInput);
                    return reply;
                case 1:
                    StateReply reply1 = stateOne(userInput);
                    return reply1;
                case 2:
                    StateReply reply2 = stateTwo(userInput);
                    return reply2;
                case 3:
                    StateReply reply3 = stateThree(userInput);
                    return reply3;
                default:
                    return null;
            }
        }

        private StateReply stateZero(string userInput)
        {
            if (string.IsNullOrEmpty(userInput))
            {
                string replayMessage = "Fantastic! Tell me in which cinema you would like to go";
                StateReply replay = new StateReply(false, replayMessage);
                return replay;
            }
            else
            {
                if (!userInput.Contains("selectedcinema=")) {
                    string request = "v2/cinemas/name/" + userInput + "/";
                    string urlRequest = ConnectionUtility.CreateGetRequest(request);
                    WebResponse response = ConnectionUtility.MakeRequest(urlRequest);
                    CinemaList cinemaArray = ConnectionUtility.deserialise<CinemaList>(response);
                    this.cinemaList = cinemaArray.Data;

                    if (cinemaArray.Data.Count != 0)
                    {
                        if (cinemaArray.Data.Count > 1)
                        {
                            string replayMessage = "These are all the cinema that match your request. Please select your desired one";
                            StateReply replay = new StateReply(false, replayMessage, "herocard");
                            string heroCardTitle = "Select your cinema";

                            List<CardAction> cardButtons = new List<CardAction>();

                            foreach (Cinema Cinema in cinemaArray.Data)
                            {
                                string title = Cinema.Name + " City: " + Cinema.City;
                                string value = "selectedCinema=" + Cinema.CinemaID;
                                CardAction plButton = new CardAction()
                                {
                                    Value = value,
                                    Type = "imBack",
                                    Title = title
                                };
                                cardButtons.Add(plButton);
                            }

                            replay.HeroCard = ReplyUtility.generateHeroCardStateReply(cardButtons, heroCardTitle, "Please select one");
                            return replay;
                        }
                        else
                        {
                            Cinema selectedCinema = cinemaArray.Data.First();
                            this.SelectedCinema = selectedCinema;
                            string replyMessage = "Great ! I have found " + this.SelectedCinema.Name + ". When do you want to go?";
                            StateReply reply = ReplyUtility.generateWeekDayReply(replyMessage);
                            StateNum = 1;
                            return reply;
                        }
                    }
                    else
                    {
                        string replayMessage = "I did not find any cinema that matches your input. Please try with another name.";
                        StateReply replay = new StateReply(false, replayMessage);
                        return replay;
                    }
                }
                else
                {
                    string result = userInput.Replace("selectedcinema=", String.Empty);
                    foreach (Cinema cinema in cinemaList)
                    {
                        if (cinema.CinemaID == Int32.Parse(result))
                        {
                            this.SelectedCinema = cinema;
                        }
                    }

                    string replyMessage = "Perfect, thank you for your help. When do you want to go?";
                    StateReply reply = ReplyUtility.generateWeekDayReply(replyMessage);
                    StateNum = 1;
                    return reply;
                }
            }
        }

        private StateReply stateOne(string userInput)
        {
            if (userInput.Contains("selectedday="))
            {
                string selectedDay = userInput.Replace("selectedday=", String.Empty);
                NumberFormatInfo nfi = new NumberFormatInfo();
                this.dateChoosen = DateTime.ParseExact(selectedDay, "MM/dd/yyyy", CultureInfo.InvariantCulture);

                string request = "v2/cinemas/id/" + SelectedCinema.CinemaID + "/movies";
                string requestWithParameter = request + "/?StartDate=" + this.dateChoosen.ToString("yyyy-MM-dd") + "&EndDate=" + this.dateChoosen.AddDays(1).ToString("yyyy-MM-dd");
                string urlRequest = ConnectionUtility.CreateGetRequest(requestWithParameter);
                WebResponse response = ConnectionUtility.MakeRequest(urlRequest);
                MovieList movieArray = ConnectionUtility.deserialise<MovieList>(response);

                if (movieArray.Data.Count != 0)
                {
                    string replayMessage = "This is the list of all the Movies that are available in "+ SelectedCinema.Name;
                    StateReply replay = new StateReply(false, replayMessage, "herocard");
                    string heroCardTitle = "Please select only one movie";

                    List<CardAction> cardButtons = new List<CardAction>();

                    foreach (Movie movie in movieArray.Data)
                    {
                        string title = movie.Title;
                        string value = "movieSelected=" + movie.ImdbID + "&" + movie.Title;
                        CardAction plButton = new CardAction()
                        {
                            Value = value,
                            Type = "imBack",
                            Title = title
                        };
                        cardButtons.Add(plButton);
                    }

                    replay.HeroCard = ReplyUtility.generateHeroCardStateReply(cardButtons, heroCardTitle, "Please select one");
                    StateNum = 2;
                    return replay;
                }
                else
                {
                    string replyMessage = "I did not found any movies available in the day that you have selected. Please try with another day or looking for another cinema";
                    StateReply reply = ReplyUtility.generateWeekDayReply(replyMessage);
                    return reply;
                }
            }
            else
            {
                return null;
            }
        }

        private StateReply stateTwo(string userInput)
        {
            if (userInput.Contains("movieselected="))
            {
                string selectedMovieID = userInput.Replace("movieselected=", String.Empty);
                Char delimiter = '&';
                String[] substrings = selectedMovieID.Split(delimiter);
                this.saveMovie(substrings[0], substrings[1]);

                string request = "v2/projections/list/" + SelectedMovie.ImdbID + "/" + SelectedCinema.CinemaID;
                string requestWithParameter = request + "/?StartDate=" + this.dateChoosen.ToString("yyyy-MM-dd") + "&EndDate=" + this.dateChoosen.AddDays(1).ToString("yyyy-MM-dd");
                string urlRequest = ConnectionUtility.CreateGetRequest(requestWithParameter);
                WebResponse response = ConnectionUtility.MakeRequest(urlRequest);
                ProjectionsList projectionList = ConnectionUtility.deserialise<ProjectionsList>(response);
                this.sentProjections = projectionList.Data;

                if (projectionList.Data.Count != 0)
                {
                    StateReply reply = this.generateStateReplyForProjections(projectionList.Data);
                    StateNum = 3;
                    return reply;
                }
                else
                {
                    StateReply replay = this.stateOne("selectedday=" + dateChoosen.ToString("MM/dd/yyyy"));
                    return replay;
                }
            }
            else
            {
                return null;
            }
        }

        private StateReply stateThree(string userInput)
        {
            if (userInput.Equals("goback"))
            {
                StateReply replay = this.stateOne("selectedday=" + dateChoosen.ToString("MM/dd/yyyy"));
                return replay;
            }
            else
            {
                this.saveProjection(userInput);
                return this.reserveYourSeat();
            }
        }

    }
}