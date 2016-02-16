﻿/*
This file is part of Depressurizer.
Copyright 2011, 2012, 2013 Steve Labbe.

Depressurizer is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

Depressurizer is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Depressurizer.  If not, see <http://www.gnu.org/licenses/>.
*/
using Rallion;
using System;
using System.Collections.Generic;
using System.Xml;

namespace Depressurizer
{

    /// <summary>
    /// Autocategorization scheme that adds developer and publisher categories.
    /// </summary>
    public class AutoCatDevPub : AutoCat
    {

        public override AutoCatType AutoCatType
        {
            get { return AutoCatType.DevPub; }
        }

        // Autocat configuration
        public bool AllDevelopers { get; set; }
        public bool AllPublishers { get; set; }
        public string Prefix { get; set; }

        public List<string> Developers { get; set; }
        public List<string> Publishers { get; set; }

        // Serialization keys
        public const string TypeIdString = "AutoCatDevPub";
        private const string
            XmlName_Name = "Name",
            XmlName_Filter = "Filter",
            XmlName_AllDevelopers = "AllDevelopers",
            XmlName_AllPublishers = "AllPublishers",
            XmlName_Prefix = "Prefix",
            XmlName_Developers = "Developers",
            XmlName_Developer = "Developer",
            XmlName_Publishers = "Publishers",
            XmlName_Publisher = "Publisher";

        private GameList gamelist;

        /// <summary>
        /// Creates a new AutoCatManual object, which removes selected (or all) categories from one list and then, optionally, assigns categories from another list.
        /// </summary>
        public AutoCatDevPub(string name, string filter = null, string prefix = null, bool developersAll = false, bool publishersAll = false, List<string> developers = null, List<string> publishers = null)
            : base(name)
        {
            Filter = filter;
            Prefix = prefix;
            AllDevelopers = developersAll;
            AllPublishers = publishersAll;
            Developers = (developers == null) ? new List<string>() : developers;
            Publishers = (publishers == null) ? new List<string>() : publishers;
        }

        protected AutoCatDevPub(AutoCatDevPub other)
            : base(other)
        {
            Filter = other.Filter;
            Prefix = other.Prefix;
            AllDevelopers = other.AllDevelopers;
            AllPublishers = other.AllPublishers;
            Developers = new List<string>(other.Developers);
            Publishers = new List<string>(other.Publishers);
        }

        public override AutoCat Clone()
        {
            return new AutoCatDevPub(this);
        }

        /// <summary>
        /// Prepares to categorize games. Prepares a list of genre categories to remove. Does nothing if removeothergenres is false.
        /// </summary>
        public override void PreProcess(GameList games, GameDB db)
        {
            base.PreProcess(games, db);
            gamelist = games;
        }

        public override void DeProcess()
        {
            base.DeProcess();
            gamelist = null;
        }

        public override AutoCatResult CategorizeGame(GameInfo game, Filter filter)
        {
            if (games == null)
            {
                Program.Logger.Write(LoggerLevel.Error, GlobalStrings.Log_AutoCat_GamelistNull);
                throw new ApplicationException(GlobalStrings.AutoCatGenre_Exception_NoGameList);
            }
            if (db == null)
            {
                Program.Logger.Write(LoggerLevel.Error, GlobalStrings.Log_AutoCat_DBNull);
                throw new ApplicationException(GlobalStrings.AutoCatGenre_Exception_NoGameDB);
            }
            if (game == null)
            {
                Program.Logger.Write(LoggerLevel.Error, GlobalStrings.Log_AutoCat_GameNull);
                return AutoCatResult.Failure;
            }

            if (!db.Contains(game.Id) || db.Games[game.Id].LastStoreScrape == 0) return AutoCatResult.NotInDatabase;

            if (!game.IncludeGame(filter)) return AutoCatResult.Filtered;

            List<string> devs = db.GetDevelopers(game.Id);

            if (devs != null)
            {
                for (int index = 0; index < devs.Count; index++)
                {
                    if (Developers.Contains(devs[index]) || AllDevelopers)
                    {
                        game.AddCategory(games.GetCategory(GetProcessedString(devs[index])));
                    }
                }
            }

            List<string> pubs = db.GetPublishers(game.Id);

            if (pubs != null)
            {
                for (int index = 0; index < pubs.Count; index++)
                {
                    if (Publishers.Contains(pubs[index]) || AllPublishers)
                    {
                        game.AddCategory(games.GetCategory(GetProcessedString(pubs[index])));
                    }
                }
            }

            return AutoCatResult.Success;
        }

        private string GetProcessedString(string baseString)
        {
            if (string.IsNullOrEmpty(Prefix))
            {
                return baseString;
            }
            else {
                return Prefix + baseString;
            }
        }

        public override void WriteToXml(XmlWriter writer)
        {
            writer.WriteStartElement(TypeIdString);

            writer.WriteElementString(XmlName_Name, Name);
            if (Filter != null) writer.WriteElementString(XmlName_Filter, Filter);
            if (Prefix != null) writer.WriteElementString(XmlName_Prefix, Prefix);
            writer.WriteElementString(XmlName_AllDevelopers, AllDevelopers.ToString());
            writer.WriteElementString(XmlName_AllPublishers, AllPublishers.ToString());

            if (Developers.Count > 0)
            {
                writer.WriteStartElement(XmlName_Developers);
                foreach (string s in Developers)
                {
                    writer.WriteElementString(XmlName_Developer, s);
                }
                writer.WriteEndElement();
            }

            if (Publishers.Count > 0)
            {
                writer.WriteStartElement(XmlName_Publishers);
                foreach (string s in Publishers)
                {
                    writer.WriteElementString(XmlName_Publisher, s);
                }
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        public static AutoCatDevPub LoadFromXmlElement(XmlElement xElement)
        {
            string name = XmlUtil.GetStringFromNode(xElement[XmlName_Name], TypeIdString);
            string filter = XmlUtil.GetStringFromNode(xElement[XmlName_Filter], null);
            bool AllDevelopers = XmlUtil.GetBoolFromNode(xElement[XmlName_AllDevelopers], false);
            bool AllPublishers = XmlUtil.GetBoolFromNode(xElement[XmlName_AllPublishers], false);
            string prefix = XmlUtil.GetStringFromNode(xElement[XmlName_Prefix], null);

            List<string> devs = new List<string>();

            XmlElement devsListElement = xElement[XmlName_Developers];
            if (devsListElement != null)
            {
                XmlNodeList devNodes = devsListElement.SelectNodes(XmlName_Developer);
                foreach (XmlNode node in devNodes)
                {
                    string s;
                    if (XmlUtil.TryGetStringFromNode(node, out s))
                    {
                        devs.Add(s);
                    }
                }
            }

            List<string> pubs = new List<string>();

            XmlElement pubsListElement = xElement[XmlName_Publishers];
            if (pubsListElement != null)
            {
                XmlNodeList pubNodes = pubsListElement.SelectNodes(XmlName_Publisher);
                foreach (XmlNode node in pubNodes)
                {
                    string s;
                    if (XmlUtil.TryGetStringFromNode(node, out s))
                    {
                        pubs.Add(s);
                    }
                }
            }

            AutoCatDevPub result = new AutoCatDevPub(name, filter, prefix, AllDevelopers, AllPublishers, devs, pubs);
            return result;
        }
    }

}