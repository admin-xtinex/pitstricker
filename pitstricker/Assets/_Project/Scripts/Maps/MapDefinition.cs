using System;
using UnityEngine;

namespace PitStriker.Maps
{
    public enum MapStatus
    {
        Available,
        ComingSoon,
        Locked
    }

    [Serializable]
    public class MapDefinition
    {
        public string id;
        public string mapNumber;
        public string displayName;
        public string specifications;
        public string description;
        public MapStatus status;
        public string sceneName;
        public Color accentColor;

        public MapDefinition(string id, string mapNumber, string displayName, string specifications, string description, MapStatus status, string sceneName, Color accentColor)
        {
            this.id = id;
            this.mapNumber = mapNumber;
            this.displayName = displayName;
            this.specifications = specifications;
            this.description = description;
            this.status = status;
            this.sceneName = sceneName;
            this.accentColor = accentColor;
        }

        public bool IsPlayable => status == MapStatus.Available;
    }
}
