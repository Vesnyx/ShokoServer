﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using JMMServer.Entities;
using JMMServer.Providers.TraktTV;
using JMMServer.Repositories;

namespace JMMServer.Commands
{
    [Serializable]
    public class CommandRequest_TraktCollectionEpisode : CommandRequestImplementation, ICommandRequest
    {
        public int AnimeEpisodeID { get; set; }
        public int Action { get; set; }

        public TraktSyncAction ActionEnum
        {
            get
            {
                return (TraktSyncAction)Action;
            }
        }

		public CommandRequestPriority DefaultPriority
		{
			get { return CommandRequestPriority.Priority9; }
		}

		public string PrettyDescription
		{
			get
			{
                return string.Format("Sync episode to collection on Trakt: {0} - {1}", AnimeEpisodeID, Action);
			}
		}

		public CommandRequest_TraktCollectionEpisode()
		{
		}

        public CommandRequest_TraktCollectionEpisode(int animeEpisodeID, TraktSyncAction action)
		{
			this.AnimeEpisodeID = animeEpisodeID;
            this.Action = (int)action;
            this.CommandType = (int)CommandRequestType.Trakt_EpisodeCollection;
			this.Priority = (int)DefaultPriority;

			GenerateCommandID();
		}

		public override void ProcessCommand()
		{
            logger.Info("Processing CommandRequest_TraktCollectionEpisode: {0}-{1}", AnimeEpisodeID, Action);

			try
			{
                if (!ServerSettings.Trakt_IsEnabled || string.IsNullOrEmpty(ServerSettings.Trakt_AuthToken)) return;

				AnimeEpisodeRepository repEpisodes = new AnimeEpisodeRepository();
				AnimeEpisode ep = repEpisodes.GetByID(AnimeEpisodeID);
				if (ep != null)
				{
                    TraktSyncType syncType = TraktSyncType.CollectionAdd;
                    if (ActionEnum == TraktSyncAction.Remove) syncType = TraktSyncType.CollectionRemove;
                    TraktTVHelper.SyncEpisodeToTrakt(ep, syncType);
				}
			}
			catch (Exception ex)
			{
                logger.Error("Error processing CommandRequest_TraktCollectionEpisode: {0} - {1} - {2}", AnimeEpisodeID, Action, ex.ToString());
				return;
			}
		}

		/// <summary>
		/// This should generate a unique key for a command
		/// It will be used to check whether the command has already been queued before adding it
		/// </summary>
		public override void GenerateCommandID()
		{
            this.CommandID = string.Format("CommandRequest_TraktCollectionEpisode{0}-{1}", AnimeEpisodeID, Action);
		}

		public override bool LoadFromDBCommand(CommandRequest cq)
		{
			this.CommandID = cq.CommandID;
			this.CommandRequestID = cq.CommandRequestID;
			this.CommandType = cq.CommandType;
			this.Priority = cq.Priority;
			this.CommandDetails = cq.CommandDetails;
			this.DateTimeUpdated = cq.DateTimeUpdated;

			// read xml to get parameters
			if (this.CommandDetails.Trim().Length > 0)
			{
				XmlDocument docCreator = new XmlDocument();
				docCreator.LoadXml(this.CommandDetails);

				// populate the fields
                this.AnimeEpisodeID = int.Parse(TryGetProperty(docCreator, "CommandRequest_TraktCollectionEpisode", "AnimeEpisodeID"));
                this.Action = int.Parse(TryGetProperty(docCreator, "CommandRequest_TraktCollectionEpisode", "Action"));
			}

			return true;
		}

		public override CommandRequest ToDatabaseObject()
		{
			GenerateCommandID();

			CommandRequest cq = new CommandRequest();
			cq.CommandID = this.CommandID;
			cq.CommandType = this.CommandType;
			cq.Priority = this.Priority;
			cq.CommandDetails = this.ToXML();
			cq.DateTimeUpdated = DateTime.Now;

			return cq;
		}
    }
}
