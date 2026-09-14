using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PitStriker.UI
{
    public partial class MenuManager
    {
        public const string PrefSelectedMap = "UI.SelectedMap";
        public const string PrefPendingStart = "UI.PendingStart";
        public const string VillageSceneName = "SC_Village_Graphics_Test";
        public const string BeachSceneName = "SC_Beach_Graphics_Test";

        GameObject _mapSelectPanel;
        Button _btnMapVillage;
        Button _btnMapBeach;
        Button _btnMapStart;
        Button _btnMapBack;
        Image _imgMapVillage;
        Image _imgMapBeach;
        string _selectedMap = "village";

        void BuildMapSelectUI()
        {
            if (_safeFrame == null) return;

            _mapSelectPanel = Box("MapSelect", _safeFrame, 0, 0, 1080, 640, Ink);
            Label(_mapSelectPanel.transform, "Choose a map", 0, 240, 850, 56, 34, Cream);
            Label(_mapSelectPanel.transform, "Same pits and marbles. Village or beach dress.", 0, 188, 900, 36, 20, Cream);

            var villageCard = Box("Map_Village", _mapSelectPanel.transform, -220, 20, 400, 280, Card);
            Label(villageCard.transform, "VILLAGE", 0, 70, 360, 48, 28, Cream);
            Label(villageCard.transform, "Paddy lane. Same 0 / 12 / 24 pits.", 0, 10, 360, 70, 18, Cream);
            _btnMapVillage = ActionButton("Btn_MapVillage", villageCard.transform, "Select village", 0, -90, 300, 56, Gold);
            _imgMapVillage = _btnMapVillage.image;

            var beachCard = Box("Map_Beach", _mapSelectPanel.transform, 220, 20, 400, 280, Card);
            Label(beachCard.transform, "BEACH", 0, 70, 360, 48, 28, Cream);
            Label(beachCard.transform, "Sand and ocean. Same 0 / 12 / 24 pits.", 0, 10, 360, 70, 18, Cream);
            _btnMapBeach = ActionButton("Btn_MapBeach", beachCard.transform, "Select beach", 0, -90, 300, 56, Gold);
            _imgMapBeach = _btnMapBeach.image;

            _btnMapBack = ActionButton("Btn_MapBack", _mapSelectPanel.transform, "Back", -220, -250, 230, 64, Card);
            _btnMapStart = ActionButton("Btn_MapStart", _mapSelectPanel.transform, "START MATCH", 130, -250, 476, 64, Gold);

            _btnMapVillage.onClick.AddListener(() => SetSelectedMap("village"));
            _btnMapBeach.onClick.AddListener(() => SetSelectedMap("beach"));
            _btnMapBack.onClick.AddListener(() => ShowScreen(ScreenType.ChoosePlayers));
            _btnMapStart.onClick.AddListener(LaunchSelectedMap);

            _selectedMap = PlayerPrefs.GetString(PrefSelectedMap, "village");
            if (_selectedMap != "beach") _selectedMap = "village";
            RefreshMapButtons();
            _mapSelectPanel.SetActive(false);
        }

        void SetSelectedMap(string map)
        {
            _selectedMap = map == "beach" ? "beach" : "village";
            PlayerPrefs.SetString(PrefSelectedMap, _selectedMap);
            PlayerPrefs.Save();
            RefreshMapButtons();
        }

        void RefreshMapButtons()
        {
            if (_imgMapVillage) _imgMapVillage.color = _selectedMap == "village" ? Gold : Card;
            if (_imgMapBeach) _imgMapBeach.color = _selectedMap == "beach" ? Gold : Card;
        }

        void ApplyMapScreen(ScreenType screen)
        {
            if (_mapSelectPanel != null)
                _mapSelectPanel.SetActive(screen == ScreenType.MapSelect);
        }

        public static string TargetSceneName()
        {
            string map = PlayerPrefs.GetString(PrefSelectedMap, "village");
            return map == "beach" ? BeachSceneName : VillageSceneName;
        }

        void LaunchSelectedMap()
        {
            SaveSetup();
            PlayerPrefs.SetString(PrefSelectedMap, _selectedMap);
            string target = TargetSceneName();
            string current = SceneManager.GetActiveScene().name;
            if (current != target)
            {
                PlayerPrefs.SetInt(PrefPendingStart, 1);
                PlayerPrefs.Save();
                SceneManager.LoadScene(target);
                return;
            }
            HandleStartMatchClicked();
        }

        void ConsumePendingStartIfNeeded()
        {
            if (PlayerPrefs.GetInt(PrefPendingStart, 0) != 1) return;
            PlayerPrefs.SetInt(PrefPendingStart, 0);
            PlayerPrefs.Save();
            HandleStartMatchClicked();
        }
    }
}
