﻿using Newtonsoft.Json;
using Services.Server.Interfaces;
using Services.Server.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Services.Server.Factories
{
    public class JsonFactory : IJsonFactory
    {  
        public dynamic GetJson(JsonTypes types)
        {
            //Path should come from configuration file.

            var json = File.ReadAllText(@"C:\Users\Feuse135\source\repos\Services.Server\Utills\Requests.json");
            var deserialziedJson = JsonConvert.DeserializeObject<dynamic>(json);

            switch (types)
            {
                case JsonTypes.Login:
                    return deserialziedJson.Login;
                case JsonTypes.LoginAM:
                    return deserialziedJson.LoginAM;
                case JsonTypes.LoginUS:
                    return deserialziedJson.LoginUS;
                case JsonTypes.SERVER_APP_STARTUP:
                    return deserialziedJson.AppStartup;
                case JsonTypes.GetSearchSettings:
                    return deserialziedJson.GetSearchSettings.data;
                case JsonTypes.GetEncounters:
                    return deserialziedJson.GetEncounters;
                case JsonTypes.Vote:
                    return deserialziedJson.Vote.data;
                case JsonTypes.SearchLocations:
                    return deserialziedJson.SearchLocations.data;
                case JsonTypes.UpdateAboutMe:
                    return deserialziedJson.UpdateAboutMe;
                case JsonTypes.SaveLocation:
                    return deserialziedJson.SaveLocation.data;
                case JsonTypes.UploadPhoto:
                    return deserialziedJson.UploadPhoto.data;
                case JsonTypes.RemoveImage:
                    return deserialziedJson.RemoveImage;
                case JsonTypes.MakeProfilePhoto:
                    return deserialziedJson.MakeProfilePhoto.data;
                case JsonTypes.GetImages:
                    return deserialziedJson.GetImages;
                case JsonTypes.Like:
                    return deserialziedJson.Like;
                default:
                    break;
            }
            throw new Exception("Service error");
        }
    }
}