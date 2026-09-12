using System;
using System.Collections.Generic;
using UnityEngine;

namespace PitStriker.Maps
{
    public class MapManager : MonoBehaviour
    {
        private const string PREF_KEY_SELECTED_MAP = "Game.SelectedMapId";
        public const string DEFAULT_MAP_ID = "sunset_coastal";

        public static MapManager Instance { get; private set; }

        public event Action<MapDefinition> OnMapChanged;

        private readonly List<MapDefinition> _maps = new List<MapDefinition>();
        private string _selectedMapId = DEFAULT_MAP_ID;

        public IReadOnlyList<MapDefinition> GetAllMaps() => _maps;

        public MapDefinition SelectedMap
        {
            get
            {
                var map = GetMap(_selectedMapId);
                if (map != null && map.IsPlayable)
                    return map;

                return GetMap(DEFAULT_MAP_ID) ?? _maps[0];
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            InitializeCatalog();
            LoadSavedSelection();
        }

        private void InitializeCatalog()
        {
            _maps.Clear();

            _maps.Add(new MapDefinition(
                "sunset_coastal",
                "Map 01",
                "SUNSET COASTAL",
                "3 Pits • Par 6 • Sand Verge",
                "Sunset dunes and sea breeze fairways with sandy verges, a coastal boardwalk, and a lighthouse island backdrop.",
                MapStatus.Available,
                "SC_SunsetCoastal_Map02",
                new Color(0.35f, 0.75f, 0.95f, 1f)
            ));

            _maps.Add(new MapDefinition(
                "temple_courtyard",
                "Map 02",
                "TEMPLE COURTYARD",
                "3 Pits • Par 6 • Flagstone",
                "Ancient stone courtyards with stepped terraces and tight boundaries. Coming in future updates.",
                MapStatus.ComingSoon,
                string.Empty,
                new Color(0.85f, 0.45f, 0.85f, 1f)
            ));

            _maps.Add(new MapDefinition(
                "mountain_quarry",
                "Map 03",
                "MOUNTAIN QUARRY",
                "4 Pits • Par 8 • Quarry Stone",
                "Highland quarry with rugged elevation drops and rocky obstacles. Coming in future updates.",
                MapStatus.ComingSoon,
                string.Empty,
                new Color(0.95f, 0.55f, 0.35f, 1f)
            ));

            _maps.Add(new MapDefinition(
                "village",
                "Map 04",
                "VILLAGE",
                "3 Pits • Par 6 • Earth Track",
                "A rebuilt village fairway with earth embankments and stone boundary walls. Coming in a future update.",
                MapStatus.ComingSoon,
                string.Empty,
                new Color(0.95f, 0.77f, 0.25f, 1f)
            ));
        }

        private void LoadSavedSelection()
        {
            string savedId = PlayerPrefs.GetString(PREF_KEY_SELECTED_MAP, DEFAULT_MAP_ID);
            var map = GetMap(savedId);
            if (map != null && map.IsPlayable)
            {
                _selectedMapId = savedId;
            }
            else
            {
                _selectedMapId = DEFAULT_MAP_ID;
            }
        }

        public MapDefinition GetMap(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return _maps.Find(m => m.id.Equals(id, StringComparison.OrdinalIgnoreCase));
        }

        public bool SelectMap(string id)
        {
            var map = GetMap(id);
            if (map == null || !map.IsPlayable)
            {
                return false;
            }

            _selectedMapId = map.id;
            PlayerPrefs.SetString(PREF_KEY_SELECTED_MAP, _selectedMapId);
            PlayerPrefs.Save();

            OnMapChanged?.Invoke(map);
            return true;
        }
    }
}
