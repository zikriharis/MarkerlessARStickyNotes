
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System;
using Unity.VisualScripting;

/// <summary>
/// CloudPersistence
/// This class manages the UI states.
/// It maps the screens and buttons to actions for scanning maps, localising, creating / deleting objects
/// </summary>
public class CloudPersistence : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField]
    private Mapper _mapper;

    [SerializeField]
    private Tracker _tracker;

    [Header("UX - Status")]
    [SerializeField]
    private Text _statusText;

    //[Header("UX - Room Name Input")]
    //[SerializeField]
    //private InputField _roomNameInputField;

    [Header("UX - Create/Load")]
    [SerializeField]
    private GameObject _createLoadPanel;
    private Button _stopScanningButton;

    [SerializeField]
    private Button _createMapButton;

    [SerializeField]
    private Button _loadMapButton;

    [Header("UX - Scan Map")]
    [SerializeField]
    private GameObject _scanMapPanel;

    [SerializeField]
    private Button _startScanning;

    [SerializeField]
    private Button _exitScanMapButton;

    [Header("UX - Scanning Animation")]
    [SerializeField]
    private GameObject _scanningAnimationPanel;

    //recheck if this is needed
    [Header("UX - Localization")]
    [SerializeField]
    private GameObject _localizationPanel;

    //recheck if this is needed
    [SerializeField]
    private Button _exitLocalizeButton;

    [Header("UX - In Game")]
    [SerializeField]
    private GameObject _inGamePanel;

    [SerializeField]
    private Button _placeCubeButton;

    [SerializeField]
    private Button _deleteCubesButton;

    [SerializeField]
    private Button _exitInGameButton;

    private string GlobalMapName = "global_map";
    private bool _sendNewMap;
    private bool _waitForMap;
    private bool _clearOnLoad;

    //files to save to
    public static string k_mapFileName = "ADHocMapFile";
    public static string k_objectsFileName = "ADHocObjectsFile";

    /// <summary>
    /// Set up to main menu on start
    /// </summary>
    void Start()
    {
        //we want to use the local storage so turning on the local file save option
        //refer to tracker.cs since this is not included in on device persistent
        _tracker._loadFromFile = true;
        _mapper._saveToFile = true;

        //Load the previous map if possible
        if (PlayerPrefs.HasKey("LastMapSignature"))
        {
            GlobalMapName = PlayerPrefs.GetString("LastMapSignature");
        }
        else
        {
            // Initialize UI by hiding and disabling unrelated UI panel
            SetUp_CreateMenu();
        }
    }

    //private void Update()
    //{
    //    if (!_datastoreManager._networkRunning)
    //        return;

    //if we have a map waiting to send we can send it now.
    //     if (_sendNewMap)
    //     {
    //         _sendNewMap = false;
    //         _datastoreManager.SaveMapToDatastore();
    //     }
    //
    //     //if we are waiting to localise we can localise now
    //     if (_waitForMap)
    //     {
    //         _waitForMap = false;
    //         //go to tracking and localise to the map.
    //        _statusText.text = "Move Phone around to localize to map";
    //         _tracker._tracking += Localized;
    //         _tracker.StartTracking();
    //
    //         _scanningAnimationPanel.SetActive(true);
    //     }
    // }

    /// <summary>
    /// Exit to main menu
    /// </summary>
    private void Exit()
    {
        _statusText.text = "";

        //make sure all menu are destroyed
        Teardown_InGameMenu();
        Teardown_LocalizeMenu();
        Teardown_ScanningMenu();
        Teardown_CreateMenu();

        StartCoroutine(ClearTrackingAndMappingState());

        //check if this is needed
        _waitForMap = false;
        _sendNewMap = false;
        _clearOnLoad = false;

        //go back to the main menu
        SetUp_CreateMenu();
    }

    private IEnumerator ClearTrackingAndMappingState()
    {
        // Both ARPersistentAnchorManager and 
        // need to be diabled before calling ClearDeviceMap()

        _mapper.ClearAllState();
        yield return null;

        _tracker.ClearAllState();
        yield return null;
    }

    //create and load map functions
    private bool CheckForSavedMap(string MapFileName)
    {
        if (string.IsNullOrEmpty(GlobalMapName) || string.IsNullOrEmpty(MapFileName))
        {
            Debug.LogWarning("Location name or map file is null/empty");
            return false;
        }

        string path = Path.Combine(Application.persistentDataPath, $"{GlobalMapName}_{MapFileName}");
        return File.Exists(path);
    }

    private void SetUp_CreateMenu()
    {
        //hide other menus
        Teardown_InGameMenu();
        Teardown_ScanningMenu();
        Teardown_LocalizeMenu();

        _createLoadPanel.SetActive(true);

        _createMapButton.onClick.AddListener(SetUp_ScanMenu);
        _loadMapButton.onClick.AddListener(SetUp_LocalizeMenu);

        _createMapButton.interactable = true;

        //_roomNameInputField.gameObject.SetActive(true);

        //if there is a saved map enable the load button.
        if (CheckForSavedMap(k_mapFileName))
        {
            _loadMapButton.interactable = true;
        }
        else
        {
            _loadMapButton.interactable = false;
        }

    }

    private void Teardown_CreateMenu()
    {
        //_roomNameInputField.gameObject.SetActive(false);

        _createLoadPanel.gameObject.SetActive(false);
        _createMapButton.onClick.RemoveAllListeners();
        _loadMapButton.onClick.RemoveAllListeners();
    }

    bool ValidateRoomName(string roomName)
    {
        if ((roomName.Length == 0) || (!Regex.IsMatch(roomName, "^[a-zA-Z0-9]*$")))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Scan Map functions
    /// </summary>
    private void SetUp_ScanMenu()
    {
        //if (!ValidateRoomName(_roomNameInputField.text))
        //{
        //    _statusText.text = "Invalid name";
        //    return;
        //}

        //_mapSignature = _roomNameInputField.text;

        _statusText.text = "";

        Teardown_CreateMenu();
        _scanMapPanel.SetActive(true);
        _startScanning.onClick.AddListener(StartScanning);
        _exitScanMapButton.onClick.AddListener(Exit);
        _stopScanningButton.onClick.AddListener(StopHouseMapping);
        _stopScanningButton.gameObject.SetActive(true);

        _startScanning.interactable = true;
        _exitScanMapButton.interactable = true;
    }

    private void Teardown_ScanningMenu()
    {
        //disable and hide unrelated UI
        _startScanning.onClick.RemoveAllListeners();
        _exitScanMapButton.onClick.RemoveAllListeners();
        _scanMapPanel.gameObject.SetActive(false);
        _mapper._onMappingComplete -= MappingComplete;
        _mapper.StopMapping();
    }

    private void StartScanning()
    {
        _startScanning.interactable = false;
        _statusText.text = "Look Around to create map";
        _mapper._onMappingComplete += MappingComplete;
        float time = 5.0f;
        _mapper.RunMappingFor(time);

        _scanningAnimationPanel.SetActive(true);
    }

    //callback function when finish generating map
    private void MappingComplete(bool success)
    {
        if (success)
        {
            //clear out any cubers
            DeleteCubes();
            _scanningAnimationPanel.SetActive(false);

            //jump to localizing.
            SetUp_LocalizeMenu();
        }
        else
        {
            //failed to make a map try again.
            _startScanning.interactable = true;
            _statusText.text = "Map Creation Failed Try Again";
        }

        GlobalMapName = GenerateMapSignature();
        PlayerPrefs.SetString("LastMapSignature", GlobalMapName);
        PlayerPrefs.Save();

        //Removes this method from the onMappingComplete listener list to prevent being triggered in future and avoid double calls
        _mapper._onMappingComplete -= MappingComplete;
    }

    /// <summary>
    /// Localization to Map functions
    /// </summary>
    private void SetUp_LocalizeMenu()
    {
        //if (!ValidateRoomName(_roomNameInputField.text))
        //{
        //    _statusText.text = "Invalid name";
        //    return;
        //}

        //_mapSignature = _roomNameInputField.text;

        Teardown_CreateMenu();
        Teardown_ScanningMenu();

        //check if this is needed
        //_localizationPanel.SetActive(true);
        //_exitLocalizeButton.onClick.AddListener(Exit);

        //go to tracking and localise to the map.
        _statusText.text = "Move Phone around to localize to map";
        _tracker._tracking += Localized;
        _tracker.StartTracking();

        _scanningAnimationPanel.SetActive(true);

    }
    private void Teardown_LocalizeMenu()
    {
        _tracker._tracking -= Localized;
        _scanningAnimationPanel.SetActive(false);


        //recheck if this is needed
        //_localizationPanel.SetActive(false);
        //_exitLocalizeButton.onClick.RemoveAllListeners();
    }

    private void Localized(bool localized)
    {
        //once we are localised we can open the main menu.
        if (localized == true)
        {
            _statusText.text = "";
            _tracker._tracking -= Localized;
            SetUp_InGameMenu();
            LoadCubes();
            _scanningAnimationPanel.SetActive(false);

            //if (_clearOnLoad)
            //{
            //    _clearOnLoad = false;
            //    _datastoreManager.DeleteCubes();
            //}
        }
        else
        {
            //failed exit out.
            Exit();
        }
    }
    /// <summary>
    /// In game functions
    /// </summary>
    private void SetUp_InGameMenu()
    {
        Teardown_LocalizeMenu();
        Teardown_ScanningMenu();

        _inGamePanel.SetActive(true);
        _placeCubeButton.onClick.AddListener(PlaceCube);
        _deleteCubesButton.onClick.AddListener(DeleteCubes);
        _exitInGameButton.onClick.AddListener(Exit);

        _placeCubeButton.interactable = true;
        _exitInGameButton.interactable = true;
    }

    private void Teardown_InGameMenu()
    {
        //disable and hide unrelated UI
        _placeCubeButton.onClick.RemoveAllListeners();
        _exitInGameButton.onClick.RemoveAllListeners();
        _deleteCubesButton.onClick.RemoveAllListeners();
        _inGamePanel.gameObject.SetActive(false);
    }

    /// <summary>
    /// Manging the cude placement/storage and anchoring to map function
    /// </summary>
    private GameObject CreateAndPlaceCube(Vector3 localPos)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);

        //add it under the anchor on our map.
        _tracker.AddObjectToAnchor(go);
        go.transform.localPosition = localPos;
        //make it smaller.
        go.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
        return go;
    }

    private void PlaceCube()
    {
        //place a cube 2m in front of the camera.
        var pos = Camera.main.transform.position + (Camera.main.transform.forward * 2.0f);
        var go = CreateAndPlaceCube(_tracker.GetAnchorRelativePosition(pos));
        var fileName = GlobalMapName + "_" + CloudPersistence.k_objectsFileName;
        var path = Path.Combine(Application.persistentDataPath, fileName);

        using (StreamWriter sw = File.AppendText(path))
        {
            sw.WriteLine(go.transform.localPosition);
        }
    }

    private void LoadCubes()
    {
        var fileName = GlobalMapName + "_" + CloudPersistence.k_objectsFileName;
        var path = Path.Combine(Application.persistentDataPath, fileName);
        if (File.Exists(path))
        {
            using (StreamReader sr = new StreamReader(path))
            {
                while (sr.Peek() >= 0)
                {
                    var pos = sr.ReadLine();
                    var split1 = pos.Split("(");
                    var split2 = split1[1].Split(")");
                    var parts = split2[0].Split(",");
                    Vector3 localPos = new Vector3(
                        System.Convert.ToSingle(parts[0]),
                        System.Convert.ToSingle(parts[1]),
                        System.Convert.ToSingle(parts[2])
                    );

                    CreateAndPlaceCube(localPos);
                }
            }
        }
    }

    private void DeleteCubes()
    {
        //delete from the file.
        var fileName = GlobalMapName + "_" + CloudPersistence.k_objectsFileName;
        var path = Path.Combine(Application.persistentDataPath, fileName);
        File.Delete(path);

        //delete from in game.
        if (_tracker.Anchor)
        {
            for (int i = 0; i < _tracker.Anchor.transform.childCount; i++)
                Destroy(_tracker.Anchor.transform.GetChild(i).gameObject);
        }
    }
    private string GenerateMapSignature()
    {
        // In real scenario: hash of map point cloud or anchor positions
        // For now: fake it using timestamp
        return "room_" + DateTime.Now.Ticks.ToString();
    }

    private void StopHouseMapping()
    {
        _mapper.StopHouseMapping();
        _statusText.text = "Finished mapping entire house";
        // You might want to automatically proceed to localization here
    }
}