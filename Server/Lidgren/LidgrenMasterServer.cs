﻿using LmpCommon;
using LmpCommon.Message.Data.MasterServer;
using LmpCommon.Message.MasterServer;
using LmpCommon.Time;
using Server.Context;
using Server.Log;
using Server.Settings.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Server.Lidgren
{
    public class LidgrenMasterServer
    {
        private static List<IPEndPoint> MasterServerEndpoints { get; } = new List<IPEndPoint>();

        private static int MasterServerRegistrationMsInterval => MasterServerSettings.SettingsStore.MasterServerRegistrationMsInterval < 5000 ?
            5000 : MasterServerSettings.SettingsStore.MasterServerRegistrationMsInterval;

        public static async void RefreshMasterServersList()
        {
            if (!MasterServerSettings.SettingsStore.RegisterWithMasterServer) return;

            while (ServerContext.ServerRunning)
            {
                lock (MasterServerEndpoints)
                {
                    MasterServerEndpoints.Clear();
                    MasterServerEndpoints.AddRange(MasterServerRetriever.RetrieveWorkingMasterServersEndpoints()
                        .Select(Common.CreateEndpointFromString));
                }

                await Task.Delay((int)TimeSpan.FromMinutes(10).TotalMilliseconds);
            }
        }

        public static async void RegisterWithMasterServer()
        {
            if (!MasterServerSettings.SettingsStore.RegisterWithMasterServer) return;

            LunaLog.Normal("Registering with master servers...");

            var adr = LunaNetUtils.GetMyAddress();
            if (adr == null) return;

            var endpoint = new IPEndPoint(adr, ServerContext.Config.Port);
            while (ServerContext.ServerRunning)
            {
                var msgData = ServerContext.ServerMessageFactory.CreateNewMessageData<MsRegisterServerMsgData>();
                msgData.Id = LidgrenServer.Server.UniqueIdentifier;
                msgData.Password = !string.IsNullOrEmpty(GeneralSettings.SettingsStore.Password);
                msgData.Cheats = GeneralSettings.SettingsStore.Cheats;
                msgData.Description = GeneralSettings.SettingsStore.Description;
                msgData.CountryCode = GeneralSettings.SettingsStore.CountryCode;
                msgData.Website = GeneralSettings.SettingsStore.Website;
                msgData.WebsiteText = GeneralSettings.SettingsStore.WebsiteText;
                msgData.DropControlOnExit = GeneralSettings.SettingsStore.Cheats;
                msgData.DropControlOnExitFlight = GeneralSettings.SettingsStore.Cheats;
                msgData.DropControlOnVesselSwitching = GeneralSettings.SettingsStore.Cheats;
                msgData.GameMode = (int)GeneralSettings.SettingsStore.GameMode;
                msgData.InternalEndpoint = $"{endpoint.Address}:{endpoint.Port}";
                msgData.MaxPlayers = GeneralSettings.SettingsStore.MaxPlayers;
                msgData.ModControl = GeneralSettings.SettingsStore.ModControl;
                msgData.PlayerCount = ServerContext.Clients.Count;
                msgData.ServerName = GeneralSettings.SettingsStore.ServerName;
                msgData.ServerVersion = LmpVersioning.CurrentVersion.ToString(3);
                msgData.VesselPositionUpdatesMsInterval = IntervalSettings.SettingsStore.VesselUpdatesMsInterval;
                msgData.SecondaryVesselPositionUpdatesMsInterval = IntervalSettings.SettingsStore.SecondaryVesselUpdatesMsInterval;
                msgData.WarpMode = (int)WarpSettings.SettingsStore.WarpMode;
                msgData.TerrainQuality = (int)GeneralSettings.SettingsStore.TerrainQuality;

                msgData.Description = msgData.Description.Length > 200 ? msgData.Description.Substring(0, 200) : msgData.Description;
                msgData.CountryCode = msgData.CountryCode.Length > 2 ? msgData.CountryCode.Substring(0, 2) : msgData.CountryCode;
                msgData.Website = msgData.Website.Length > 60 ? msgData.Website.Substring(0, 60) : msgData.Website;
                msgData.WebsiteText = msgData.WebsiteText.Length > 15 ? msgData.WebsiteText.Substring(0, 15) : msgData.WebsiteText;
                msgData.ServerName = msgData.ServerName.Length > 30 ? msgData.ServerName.Substring(0, 30) : msgData.ServerName;

                lock (MasterServerEndpoints)
                {
                    foreach (var masterServer in MasterServerEndpoints)
                    {
                        RegisterWithMasterServer(msgData, masterServer);
                    }
                }

                await Task.Delay(MasterServerRegistrationMsInterval);
            }
        }

        private static void RegisterWithMasterServer(MsRegisterServerMsgData msgData, IPEndPoint masterServer)
        {
            Task.Run(() =>
            {
                var msg = ServerContext.MasterServerMessageFactory.CreateNew<MainMstSrvMsg>(msgData);
                msg.Data.SentTime = LunaNetworkTime.UtcNow.Ticks;

                try
                {
                    var outMsg = LidgrenServer.Server.CreateMessage(msg.GetMessageSize());
                    msg.Serialize(outMsg);
                    LidgrenServer.Server.SendUnconnectedMessage(outMsg, masterServer);

                    //Force send of packets
                    LidgrenServer.Server.FlushSendQueue();
                }
                catch (Exception)
                {
                    // ignored
                }
            });
        }
    }
}
