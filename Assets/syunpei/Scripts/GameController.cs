using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class GameController : MonoBehaviour
{
    private const int StationCount = 30;
    private const int Infinity = 1_000_000_000;
    private const float IntermediateStopSeconds = 5f;
    private const float TerminalStopSeconds = 10f;

    public enum GameMode { Title, Game, Result }
    private enum PlayerState { AtStation, InTrain }
    private enum TrainMotionState { WaitingForInitialPlacement, Stopped, WaitingToDepart, Running }

    [Header("Screen State")]
    [SerializeField] private GameMode gameMode = GameMode.Title;

    [Header("Screens")]
    [SerializeField] private GameObject titleScreen;
    [SerializeField] private GameObject gameScreen;
    [SerializeField] private GameObject resultScreen;
    [SerializeField] private GameObject stationScreen;
    [SerializeField] private GameObject trainScreen;

    [Header("Buttons and Groups")]
    [SerializeField] private GameObject startGameButton;
    [SerializeField] private GameObject rankObject;
    [SerializeField] private GameObject scoreObject;
    [SerializeField] private GameObject resultStatusObject;
    [SerializeField] private GameObject returnToTitleButton;
    [SerializeField] private GameObject[] gameOnlyObjects;
    [SerializeField] private GameObject[] alwaysHiddenObjects;

    [Header("Background Images")]
    [SerializeField] private Image titleBackgroundImage;
    [SerializeField] private Sprite titleBackgroundSprite;
    [SerializeField] private Image stationBackgroundImage;
    [Tooltip("Home backgrounds. They are distributed among the 30 stations before each game.")]
    [SerializeField] private Sprite[] stationBackgroundSprites;
    [SerializeField] private Image trainBackgroundImage;
    [SerializeField] private Sprite trainBackgroundSprite;
    [SerializeField] private Image resultBackgroundImage;
    [SerializeField] private Sprite resultBackgroundSprite;
    [Tooltip("Transparent train image layered over the station background.")]
    [SerializeField] private Image stoppedTrainImage;
    [SerializeField] private Sprite stoppedTrainSprite;

    [Header("Fade")]
    [Tooltip("A black Image stretched over the entire Canvas and placed last in the hierarchy.")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeOutSeconds = 0.4f;
    [SerializeField] private float fadeInSeconds = 0.4f;

    [Header("Audio")]
    [SerializeField] private AudioSource bgmAudioSource;
    [SerializeField] private AudioClip bgmClip;
    [Range(0f, 1f)] [SerializeField] private float bgmVolume = 0.7f;

    [Header("Game Time")]
    [SerializeField] private float gameDurationSeconds = 600f;

    [Header("Rank Settings")]
    [SerializeField] private float rankSMaximum = 1.10f;
    [SerializeField] private float rankAMaximum = 1.25f;
    [SerializeField] private float rankBMaximum = 1.50f;

    [Header("Game UI")]
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text currentStationText;
    [SerializeField] private TMP_Text goalStationText;
    [SerializeField] private TMP_Text platformText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private TMP_Text travelledDistanceText;
    [SerializeField] private TMP_Text trainInformationText;

    [Header("Result UI")]
    [SerializeField] private TMP_Text resultStatusText;
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text scoreText;

    [Header("Train Start Settings")]
    [SerializeField] private float initialTrainIntervalSeconds = 0.35f;
    [SerializeField] private bool logTrainEvents;

    private PlayerState playerState;
    private int startStation;
    private int goalStation;
    private int currentStation;
    private int selectedPlatformNumber = 1;
    private int ridingTrainId = -1;
    private bool ridingTrainIsStopped;
    private int ridingTrainStation = -1;
    private int ridingTrainPlatform = -1;
    private int actualTravelDistance;
    private float elapsedRealSeconds;
    private bool gameFinished;
    private bool isTransitioning;
    private bool lastReachedGoal;
    private string lastRank = "-";
    private int lastScore;
    private long requestSequence;

    private int[,] directDistance;
    private int[,] shortestDistance;
    private readonly Sprite[] assignedStationBackgrounds = new Sprite[StationCount + 1];

    private readonly Dictionary<EdgeKey, int> edgeDistances = new Dictionary<EdgeKey, int>();
    private readonly Dictionary<int, int> stationPlatformCounts = new Dictionary<int, int>();
    private readonly Dictionary<PlatformKey, int> stoppedTrains = new Dictionary<PlatformKey, int>();
    private readonly Dictionary<int, PlatformKey> trainStopPositions = new Dictionary<int, PlatformKey>();
    private readonly Dictionary<int, TrainState> trains = new Dictionary<int, TrainState>();
    private readonly Dictionary<EdgeLaneKey, int> occupiedTracks = new Dictionary<EdgeLaneKey, int>();
    private readonly List<MoveRequest> moveRequests = new List<MoveRequest>();
    private readonly List<int> sortedTrainIds = new List<int>();

    private readonly int[][] routes =
    {
        new[] { 1, 2, 3, 4 }, new[] { 1, 30, 29 }, new[] { 4, 5, 6 },
        new[] { 4, 7, 8, 9, 10 }, new[] { 4, 11, 12, 13, 14 },
        new[] { 4, 27, 28, 29 }, new[] { 10, 9, 15, 14 },
        new[] { 14, 16, 17 }, new[] { 14, 18, 19, 20 },
        new[] { 14, 21, 22, 23 }, new[] { 14, 13, 12, 26 },
        new[] { 23, 24, 25, 29 }
    };

    private void Awake()
    {
        if (bgmAudioSource == null) bgmAudioSource = GetComponent<AudioSource>();
        PrepareFadeImage();
    }

    private void Start()
    {
        StartBgm();
        ApplyGameModeImmediately(GameMode.Title);
    }

    private void Update()
    {
        if (gameMode != GameMode.Game || gameFinished || isTransitioning) return;

        HandleKeyboardInput();
        elapsedRealSeconds += Time.deltaTime;
        UpdateTrains(Time.deltaTime);
        UpdateGameUI();

        if (elapsedRealSeconds >= gameDurationSeconds) FinishGame(false);
    }

    private void HandleKeyboardInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame) OnLeftButtonPressed();
        if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame) OnRightButtonPressed();
        if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame) OnUpButtonPressed();
    }

    public void StartGame()
    {
        if (!isTransitioning) StartCoroutine(ChangeGameModeWithFade(GameMode.Game));
    }

    public void ReturnToTitle()
    {
        if (!isTransitioning) StartCoroutine(ChangeGameModeWithFade(GameMode.Title));
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private IEnumerator ChangeGameModeWithFade(GameMode newMode)
    {
        isTransitioning = true;
        yield return FadeTo(1f, fadeOutSeconds);
        ApplyGameModeImmediately(newMode);
        yield return FadeTo(0f, fadeInSeconds);
        isTransitioning = false;
    }

    private IEnumerator ChangePlayerViewWithFade(PlayerState newState)
    {
        isTransitioning = true;
        yield return FadeTo(1f, fadeOutSeconds);
        playerState = newState;
        UpdateViewVisibility();
        UpdateGameUI();
        yield return FadeTo(0f, fadeInSeconds);
        isTransitioning = false;
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        if (fadeImage == null) yield break;
        fadeImage.gameObject.SetActive(true);
        fadeImage.raycastTarget = true;
        Color color = fadeImage.color;
        float startAlpha = color.a;
        float time = 0f;
        duration = Mathf.Max(0.01f, duration);
        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            color.a = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(time / duration));
            fadeImage.color = color;
            yield return null;
        }
        color.a = targetAlpha;
        fadeImage.color = color;
        fadeImage.raycastTarget = targetAlpha > 0.001f;
        if (targetAlpha <= 0.001f) fadeImage.gameObject.SetActive(false);
    }

    private void PrepareFadeImage()
    {
        if (fadeImage == null) return;
        fadeImage.transform.SetAsLastSibling();
        Color color = fadeImage.color;
        color.a = 0f;
        fadeImage.color = color;
        fadeImage.raycastTarget = false;
        fadeImage.gameObject.SetActive(false);
    }

    private void ApplyGameModeImmediately(GameMode newMode)
    {
        gameMode = newMode;
        SetObjectActive(titleScreen, newMode == GameMode.Title);
        SetObjectActive(gameScreen, newMode == GameMode.Game);
        SetObjectActive(resultScreen, newMode == GameMode.Result);
        SetObjectActive(startGameButton, newMode == GameMode.Title);
        SetObjectArrayActive(gameOnlyObjects, newMode == GameMode.Game);
        SetObjectActive(rankObject, newMode == GameMode.Result);
        SetObjectActive(scoreObject, newMode == GameMode.Result);
        SetObjectActive(resultStatusObject, newMode == GameMode.Result);
        SetObjectActive(returnToTitleButton, newMode == GameMode.Result);
        SetObjectArrayActive(alwaysHiddenObjects, false);

        if (newMode == GameMode.Title) InitializeTitleScreen();
        else if (newMode == GameMode.Game) InitializeGame();
        else InitializeResultScreen();
    }

    private void InitializeTitleScreen()
    {
        StopTrainSimulation();
        SetSprite(titleBackgroundImage, titleBackgroundSprite);
        SetObjectActive(stoppedTrainImage != null ? stoppedTrainImage.gameObject : null, false);
    }

    private void InitializeResultScreen()
    {
        StopTrainSimulation();
        SetSprite(resultBackgroundImage, resultBackgroundSprite);
        if (resultStatusText != null) resultStatusText.text = lastReachedGoal ? "DESTINATION REACHED" : "TIME UP";
        if (rankText != null) rankText.text = $"RANK: {lastRank}";
        if (scoreText != null) scoreText.text = $"SCORE: {lastScore}";
    }

    private void InitializeGame()
    {
        gameFinished = false;
        elapsedRealSeconds = 0f;
        actualTravelDistance = 0;
        ridingTrainId = -1;
        ridingTrainIsStopped = false;
        ridingTrainStation = -1;
        ridingTrainPlatform = -1;
        playerState = PlayerState.AtStation;

        InitializePlatformCounts();
        CreateGraph();
        CalculateAllPairsShortestPaths();
        SelectRandomStartAndGoal();
        AssignStationBackgrounds();

        currentStation = startStation;
        selectedPlatformNumber = 1;
        StartTrainSimulation();
        UpdateViewVisibility();
        SetMessage("Reach the destination station.");
        UpdateGameUI();
        UpdateSelectedPlatformMessage();
    }

    private void StartBgm()
    {
        if (bgmAudioSource == null || bgmClip == null) return;
        bgmAudioSource.clip = bgmClip;
        bgmAudioSource.loop = true;
        bgmAudioSource.volume = bgmVolume;
        if (!bgmAudioSource.isPlaying) bgmAudioSource.Play();
    }

    private void AssignStationBackgrounds()
    {
        Array.Clear(assignedStationBackgrounds, 0, assignedStationBackgrounds.Length);
        if (stationBackgroundSprites == null || stationBackgroundSprites.Length == 0) return;

        List<Sprite> valid = new List<Sprite>();
        foreach (Sprite sprite in stationBackgroundSprites) if (sprite != null) valid.Add(sprite);
        if (valid.Count == 0) return;

        List<Sprite> bag = new List<Sprite>();
        Sprite previous = null;
        for (int station = 1; station <= StationCount; station++)
        {
            if (bag.Count == 0)
            {
                bag.AddRange(valid);
                Shuffle(bag);
                if (bag.Count > 1 && bag[0] == previous)
                {
                    Sprite temp = bag[0]; bag[0] = bag[1]; bag[1] = temp;
                }
            }
            assignedStationBackgrounds[station] = bag[0];
            previous = bag[0];
            bag.RemoveAt(0);
        }
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            T temp = list[i]; list[i] = list[j]; list[j] = temp;
        }
    }

    private void UpdateViewVisibility()
    {
        bool atStation = playerState == PlayerState.AtStation;
        SetObjectActive(stationScreen, atStation);
        SetObjectActive(trainScreen, !atStation);
        if (atStation) UpdateStationVisuals();
        else SetSprite(trainBackgroundImage, trainBackgroundSprite);
    }

    private void UpdateStationVisuals()
    {
        if (IsValidStation(currentStation)) SetSprite(stationBackgroundImage, assignedStationBackgrounds[currentStation]);
        PlatformKey key = new PlatformKey(currentStation, selectedPlatformNumber);
        bool hasTrain = stoppedTrains.ContainsKey(key);
        if (stoppedTrainImage != null)
        {
            if (stoppedTrainSprite != null) stoppedTrainImage.sprite = stoppedTrainSprite;
            stoppedTrainImage.enabled = hasTrain && stoppedTrainImage.sprite != null;
            stoppedTrainImage.gameObject.SetActive(hasTrain);
        }
    }

    public void OnLeftButtonPressed()
    {
        if (!CanAcceptGameInput()) return;
        if (playerState != PlayerState.AtStation) { SetMessage("You cannot change platforms while riding a train."); return; }
        int count = GetPlatformCount(currentStation);
        selectedPlatformNumber = selectedPlatformNumber <= 1 ? count : selectedPlatformNumber - 1;
        UpdateGameUI();
        UpdateSelectedPlatformMessage();
    }

    public void OnRightButtonPressed()
    {
        if (!CanAcceptGameInput()) return;
        if (playerState != PlayerState.AtStation) { SetMessage("You cannot change platforms while riding a train."); return; }
        int count = GetPlatformCount(currentStation);
        selectedPlatformNumber = selectedPlatformNumber >= count ? 1 : selectedPlatformNumber + 1;
        UpdateGameUI();
        UpdateSelectedPlatformMessage();
    }

    public void OnUpButtonPressed()
    {
        if (!CanAcceptGameInput()) return;
        if (playerState == PlayerState.AtStation) TryBoardTrain();
        else TryLeaveTrain();
    }

    private bool CanAcceptGameInput() => gameMode == GameMode.Game && !gameFinished && !isTransitioning;

    private void TryBoardTrain()
    {
        PlatformKey key = new PlatformKey(currentStation, selectedPlatformNumber);
        if (!stoppedTrains.TryGetValue(key, out int trainId))
        {
            SetMessage($"No train is currently stopped at Platform {selectedPlatformNumber}.");
            return;
        }
        ridingTrainId = trainId;
        ridingTrainIsStopped = true;
        ridingTrainStation = currentStation;
        ridingTrainPlatform = selectedPlatformNumber;
        SetMessage($"Boarded Train {trainId}.");
        StartCoroutine(ChangePlayerViewWithFade(PlayerState.InTrain));
    }

    private void TryLeaveTrain()
    {
        if (ridingTrainId < 0) { SetMessage("You are not currently on a train."); return; }
        if (!ridingTrainIsStopped) { SetMessage("You cannot get off while the train is running."); return; }

        int previousTrainId = ridingTrainId;
        currentStation = ridingTrainStation;
        selectedPlatformNumber = ridingTrainPlatform;
        ridingTrainId = -1;
        ridingTrainIsStopped = false;
        ridingTrainStation = -1;
        ridingTrainPlatform = -1;
        SetMessage($"Got off Train {previousTrainId} at Station {currentStation}.");
        StartCoroutine(ChangePlayerViewWithFade(PlayerState.AtStation));
    }

    private void StartTrainSimulation()
    {
        StopTrainSimulation();
        requestSequence = 0;
        int trainId = 1;
        for (int routeId = 0; routeId < routes.Length; routeId++)
        {
            int trainCount = routeId == 4 ? 4 : 2;
            for (int index = 0; index < trainCount; index++)
            {
                bool first = index % 2 == 0;
                int routeIndex = first ? 0 : routes[routeId].Length - 1;
                TrainState train = new TrainState
                {
                    TrainId = trainId, RouteId = routeId, RouteIndex = routeIndex,
                    Direction = first ? 1 : -1, CurrentStation = routes[routeId][routeIndex],
                    CurrentPlatform = -1, ReservedPlatform = -1,
                    State = TrainMotionState.WaitingForInitialPlacement,
                    RemainingSeconds = trainId * Mathf.Max(0f, initialTrainIntervalSeconds)
                };
                trains.Add(trainId, train);
                sortedTrainIds.Add(trainId);
                trainId++;
            }
        }
    }

    private void StopTrainSimulation()
    {
        trains.Clear(); sortedTrainIds.Clear(); stoppedTrains.Clear(); trainStopPositions.Clear();
        occupiedTracks.Clear(); moveRequests.Clear();
    }

    private void UpdateTrains(float deltaTime)
    {
        foreach (int trainId in sortedTrainIds)
        {
            TrainState train = trains[trainId];
            train.RemainingSeconds -= deltaTime;
            if (train.State == TrainMotionState.WaitingForInitialPlacement && train.RemainingSeconds <= 0f) TryPlaceInitialTrain(train);
            else if (train.State == TrainMotionState.Stopped && train.RemainingSeconds <= 0f)
            {
                train.State = TrainMotionState.WaitingToDepart;
                AddMoveRequest(train);
            }
            else if (train.State == TrainMotionState.Running && train.RemainingSeconds <= 0f) CompleteArrival(train);
        }
        ProcessMoveRequests();
    }

    private void TryPlaceInitialTrain(TrainState train)
    {
        int platform = FindAvailablePlatform(train.CurrentStation, train.RouteId, train.Direction);
        if (platform < 1) { train.RemainingSeconds = 0.1f; return; }
        PlatformKey key = new PlatformKey(train.CurrentStation, platform);
        stoppedTrains[key] = train.TrainId;
        trainStopPositions[train.TrainId] = key;
        train.CurrentPlatform = platform;
        train.State = TrainMotionState.Stopped;
        train.RemainingSeconds = TerminalStopSeconds;
        if (logTrainEvents) Debug.Log($"Train {train.TrainId} initially placed at Station {train.CurrentStation}, Platform {platform}.");
        if (playerState == PlayerState.AtStation && currentStation == train.CurrentStation) UpdateStationVisuals();
    }

    private void AddMoveRequest(TrainState train)
    {
        foreach (MoveRequest request in moveRequests) if (request.TrainId == train.TrainId) return;
        int nextIndex = train.RouteIndex + train.Direction;
        if (nextIndex < 0 || nextIndex >= routes[train.RouteId].Length)
        {
            train.Direction *= -1;
            nextIndex = train.RouteIndex + train.Direction;
        }
        moveRequests.Add(new MoveRequest
        {
            TrainId = train.TrainId, Sequence = requestSequence++, FromStation = train.CurrentStation,
            ToStation = routes[train.RouteId][nextIndex], NextRouteIndex = nextIndex
        });
    }

    private void ProcessMoveRequests()
    {
        moveRequests.Sort((a, b) => a.Sequence.CompareTo(b.Sequence));
        bool moved;
        do
        {
            moved = false;
            for (int i = 0; i < moveRequests.Count; i++)
            {
                MoveRequest request = moveRequests[i];
                if (!trains.TryGetValue(request.TrainId, out TrainState train)) { moveRequests.RemoveAt(i--); continue; }
                int destinationPlatform = FindAvailablePlatform(request.ToStation, train.RouteId, train.Direction);
                EdgeLaneKey track = GetRequiredTrack(request.FromStation, request.ToStation);
                if (destinationPlatform < 1 || occupiedTracks.ContainsKey(track)) continue;

                occupiedTracks[track] = train.TrainId;
                PlatformKey destinationKey = new PlatformKey(request.ToStation, destinationPlatform);
                stoppedTrains[destinationKey] = train.TrainId;
                ReleaseCurrentPlatform(train);

                train.ReservedPlatform = destinationPlatform;
                train.RunningTrack = track;
                train.NextStation = request.ToStation;
                train.NextRouteIndex = request.NextRouteIndex;
                train.State = TrainMotionState.Running;
                train.RemainingSeconds = GetEdgeDistance(request.FromStation, request.ToStation);

                if (ridingTrainId == train.TrainId)
                {
                    ridingTrainIsStopped = false;
                    ridingTrainStation = -1;
                    ridingTrainPlatform = -1;
                    SetMessage("The train is running to the next station.");
                }
                if (logTrainEvents) Debug.Log($"Train {train.TrainId} departed Station {request.FromStation} for Station {request.ToStation}.");
                moveRequests.RemoveAt(i);
                moved = true;
                break;
            }
        } while (moved);
    }

    private void CompleteArrival(TrainState train)
    {
        int fromStation = train.CurrentStation;
        occupiedTracks.Remove(train.RunningTrack);
        train.CurrentStation = train.NextStation;
        train.RouteIndex = train.NextRouteIndex;
        train.CurrentPlatform = train.ReservedPlatform;
        train.ReservedPlatform = -1;
        train.State = TrainMotionState.Stopped;
        bool terminal = train.RouteIndex == 0 || train.RouteIndex == routes[train.RouteId].Length - 1;
        train.RemainingSeconds = terminal ? TerminalStopSeconds : IntermediateStopSeconds;
        PlatformKey key = new PlatformKey(train.CurrentStation, train.CurrentPlatform);
        trainStopPositions[train.TrainId] = key;

        if (ridingTrainId == train.TrainId)
        {
            ridingTrainIsStopped = true;
            ridingTrainStation = train.CurrentStation;
            ridingTrainPlatform = train.CurrentPlatform;
            AddRidingDistance(fromStation, train.CurrentStation);
            if (train.CurrentStation == goalStation) { FinishGame(true); return; }
            SetMessage($"Arrived at Station {train.CurrentStation}. Press Up or W to get off.");
        }
        if (playerState == PlayerState.AtStation && currentStation == train.CurrentStation) UpdateStationVisuals();
        if (logTrainEvents) Debug.Log($"Train {train.TrainId} arrived at Station {train.CurrentStation}, Platform {train.CurrentPlatform}.");
    }

    private void ReleaseCurrentPlatform(TrainState train)
    {
        train.PreviousPlatform = train.CurrentPlatform;
        if (train.CurrentPlatform < 1) return;
        PlatformKey key = new PlatformKey(train.CurrentStation, train.CurrentPlatform);
        if (stoppedTrains.TryGetValue(key, out int id) && id == train.TrainId) stoppedTrains.Remove(key);
        trainStopPositions.Remove(train.TrainId);
        train.CurrentPlatform = -1;
        if (playerState == PlayerState.AtStation && currentStation == train.CurrentStation) UpdateStationVisuals();
    }

    private int FindAvailablePlatform(int station, int routeId, int direction)
    {
        int count = GetPlatformCount(station);
        int preferred = GetPreferredPlatform(station, routeId, direction, count);
        for (int offset = 0; offset < count; offset++)
        {
            int platform = ((preferred - 1 + offset) % count) + 1;
            if (!stoppedTrains.ContainsKey(new PlatformKey(station, platform))) return platform;
        }
        return -1;
    }

    private int GetPreferredPlatform(int station, int routeId, int direction, int platformCount)
    {
        if (station == 9)
        {
            int index = Array.IndexOf(routes[routeId], 9);
            int next = index + direction;
            return next >= 0 && next < routes[routeId].Length && routes[routeId][next] == 10 ? 1 : 2;
        }
        if (station == 10 || platformCount == 1) return 1;
        bool terminal = routes[routeId][0] == station || routes[routeId][routes[routeId].Length - 1] == station;
        if (!terminal && platformCount == 2) return direction > 0 ? 1 : 2;
        return (routeId % platformCount) + 1;
    }

    private EdgeLaneKey GetRequiredTrack(int fromStation, int toStation)
    {
        EdgeKey edge = new EdgeKey(fromStation, toStation);
        bool doubleTrack = edge.Equals(new EdgeKey(4, 11)) || edge.Equals(new EdgeKey(11, 12))
            || edge.Equals(new EdgeKey(12, 13)) || edge.Equals(new EdgeKey(13, 14));
        int direction = doubleTrack ? (fromStation < toStation ? 1 : -1) : 0;
        return new EdgeLaneKey(edge, direction);
    }

    private void InitializePlatformCounts()
    {
        stationPlatformCounts.Clear();
        for (int station = 1; station <= StationCount; station++) stationPlatformCounts[station] = 1;
        int[] two = { 2, 3, 5, 7, 8, 9, 11, 12, 13, 15, 16, 18, 19, 21, 22, 23, 24, 25, 27, 28, 30 };
        foreach (int station in two) stationPlatformCounts[station] = 2;
        stationPlatformCounts[1] = 2; stationPlatformCounts[4] = 4; stationPlatformCounts[14] = 4; stationPlatformCounts[29] = 3;
    }

    private void CreateGraph()
    {
        directDistance = new int[StationCount, StationCount];
        shortestDistance = new int[StationCount, StationCount];
        edgeDistances.Clear();
        for (int i = 0; i < StationCount; i++) for (int j = 0; j < StationCount; j++) directDistance[i, j] = i == j ? 0 : Infinity;
        int[,] edges =
        {
            {1,2},{1,30},{2,3},{3,4},{4,5},{4,7},{4,11},{4,27},{5,6},{7,8},{8,9},{9,10},{9,15},
            {11,12},{12,13},{12,26},{13,14},{14,15},{14,16},{14,18},{14,21},{16,17},{18,19},{19,20},
            {21,22},{22,23},{23,24},{24,25},{25,29},{27,28},{28,29},{29,30}
        };
        for (int i = 0; i < edges.GetLength(0); i++) AddUndirectedEdge(edges[i, 0], edges[i, 1], UnityEngine.Random.Range(4, 7));
    }

    private void AddUndirectedEdge(int aStation, int bStation, int distance)
    {
        int a = aStation - 1, b = bStation - 1;
        directDistance[a, b] = directDistance[b, a] = distance;
        edgeDistances[new EdgeKey(aStation, bStation)] = distance;
    }

    private void CalculateAllPairsShortestPaths()
    {
        for (int i = 0; i < StationCount; i++) for (int j = 0; j < StationCount; j++) shortestDistance[i, j] = directDistance[i, j];
        for (int k = 0; k < StationCount; k++) for (int i = 0; i < StationCount; i++)
        {
            if (shortestDistance[i, k] >= Infinity) continue;
            for (int j = 0; j < StationCount; j++)
            {
                if (shortestDistance[k, j] >= Infinity) continue;
                int candidate = shortestDistance[i, k] + shortestDistance[k, j];
                if (candidate < shortestDistance[i, j]) shortestDistance[i, j] = candidate;
            }
        }
    }

    private void SelectRandomStartAndGoal()
    {
        List<StationPair> candidates = new List<StationPair>();
        for (int start = 1; start <= StationCount; start++) for (int goal = 1; goal <= StationCount; goal++)
        {
            if (start != goal && !AreOnSameRoute(start, goal) && shortestDistance[start - 1, goal - 1] < Infinity)
                candidates.Add(new StationPair(start, goal));
        }
        StationPair pair = candidates.Count > 0 ? candidates[UnityEngine.Random.Range(0, candidates.Count)] : new StationPair(1, 14);
        startStation = pair.Start; goalStation = pair.Goal;
    }

    private bool AreOnSameRoute(int a, int b)
    {
        foreach (int[] route in routes)
        {
            bool hasA = false, hasB = false;
            foreach (int station in route) { if (station == a) hasA = true; if (station == b) hasB = true; }
            if (hasA && hasB) return true;
        }
        return false;
    }

    private void AddRidingDistance(int from, int to)
    {
        int distance = GetEdgeDistance(from, to);
        if (distance >= 0) actualTravelDistance += distance;
    }

    private void FinishGame(bool reachedGoal)
    {
        if (gameFinished) return;
        gameFinished = true;
        lastReachedGoal = reachedGoal;
        int minimum = shortestDistance[startStation - 1, goalStation - 1];
        lastRank = reachedGoal && minimum > 0 ? CalculateRank((float)actualTravelDistance / minimum) : "C";
        lastScore = actualTravelDistance;
        StartCoroutine(ChangeGameModeWithFade(GameMode.Result));
    }

    private string CalculateRank(float ratio)
    {
        if (ratio <= rankSMaximum) return "S";
        if (ratio <= rankAMaximum) return "A";
        if (ratio <= rankBMaximum) return "B";
        return "C";
    }

    private void UpdateGameUI()
    {
        UpdateTimeText();
        if (goalStationText != null) goalStationText.text = $"Destination: Station {goalStation}";
        if (currentStationText != null)
            currentStationText.text = playerState == PlayerState.AtStation ? $"Current Station: {currentStation}"
                : ridingTrainIsStopped ? $"Current Station: {ridingTrainStation}" : "Current Position: On the train";
        if (platformText != null)
            platformText.text = playerState == PlayerState.AtStation ? $"Platform {selectedPlatformNumber} / {GetPlatformCount(currentStation)}"
                : ridingTrainIsStopped ? $"Current Platform: {ridingTrainPlatform}" : "Running";
        if (travelledDistanceText != null) travelledDistanceText.text = $"Travel Distance: {actualTravelDistance}";
        if (trainInformationText != null && playerState == PlayerState.InTrain && ridingTrainId >= 0)
        {
            TrainState train = trains.TryGetValue(ridingTrainId, out TrainState value) ? value : null;
            string destination = train != null ? $"Next: Station {GetNextDestination(train)}" : "Next: Unknown";
            trainInformationText.text = $"Train {ridingTrainId}\n{(ridingTrainIsStopped ? "Stopped" : "Running")}\n{destination}";
        }
        if (playerState == PlayerState.AtStation) UpdateStationVisuals();
    }

    private int GetNextDestination(TrainState train)
    {
        if (train.State == TrainMotionState.Running) return train.NextStation;
        int next = train.RouteIndex + train.Direction;
        if (next < 0 || next >= routes[train.RouteId].Length) next = train.RouteIndex - train.Direction;
        return routes[train.RouteId][next];
    }

    private void UpdateTimeText()
    {
        if (timeText == null) return;
        float duration = Mathf.Max(0.01f, gameDurationSeconds);
        float progress = Mathf.Clamp01(elapsedRealSeconds / duration);
        int totalMinutes = Mathf.FloorToInt(progress * 1200f);
        float remaining = Mathf.Max(0f, duration - elapsedRealSeconds);
        timeText.text = $"Game Time {totalMinutes / 60:00}:{totalMinutes % 60:00}\nTime Remaining {remaining:0.0} sec";
    }

    private void UpdateSelectedPlatformMessage()
    {
        UpdateStationVisuals();
        PlatformKey key = new PlatformKey(currentStation, selectedPlatformNumber);
        if (stoppedTrains.TryGetValue(key, out int trainId) && trains.TryGetValue(trainId, out TrainState train))
            SetMessage($"Train {trainId} bound for Station {GetNextDestination(train)} is stopped at Platform {selectedPlatformNumber}. Press Up or W to board.");
        else SetMessage($"No train is currently stopped at Platform {selectedPlatformNumber}.");
    }

    private void SetMessage(string message)
    {
        if (messageText != null) messageText.text = message;
        if (logTrainEvents) Debug.Log(message);
    }

    private static void SetObjectActive(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active) target.SetActive(active);
    }

    private static void SetObjectArrayActive(GameObject[] targets, bool active)
    {
        if (targets == null) return;
        foreach (GameObject target in targets) SetObjectActive(target, active);
    }

    private static void SetSprite(Image image, Sprite sprite)
    {
        if (image == null) return;
        if (sprite != null) image.sprite = sprite;
        image.enabled = image.sprite != null;
    }

    public int GetPlatformCount(int station) => stationPlatformCounts.TryGetValue(station, out int count) ? count : 1;
    public int GetEdgeDistance(int a, int b) => edgeDistances.TryGetValue(new EdgeKey(a, b), out int distance) ? distance : -1;
    public GameMode GetGameMode() => gameMode;
    public int GetStartStation() => startStation;
    public int GetGoalStation() => goalStation;
    public int GetCurrentStation() => currentStation;
    public int GetSelectedPlatformNumber() => selectedPlatformNumber;
    public int GetActualTravelDistance() => actualTravelDistance;
    public int GetShortestDistance() => shortestDistance == null ? Infinity : shortestDistance[startStation - 1, goalStation - 1];
    public bool IsPlayerRidingTrain() => playerState == PlayerState.InTrain;
    public int GetRidingTrainId() => ridingTrainId;
    private bool IsValidStation(int station) => station >= 1 && station <= StationCount;

    private sealed class TrainState
    {
        public int TrainId, RouteId, RouteIndex, Direction, CurrentStation, CurrentPlatform, PreviousPlatform;
        public int ReservedPlatform, NextStation, NextRouteIndex;
        public float RemainingSeconds;
        public TrainMotionState State;
        public EdgeLaneKey RunningTrack;
    }

    private sealed class MoveRequest
    {
        public int TrainId, FromStation, ToStation, NextRouteIndex;
        public long Sequence;
    }

    private readonly struct EdgeKey : IEquatable<EdgeKey>
    {
        private readonly int a, b;
        public EdgeKey(int x, int y) { if (x < y) { a = x; b = y; } else { a = y; b = x; } }
        public bool Equals(EdgeKey other) => a == other.a && b == other.b;
        public override bool Equals(object obj) => obj is EdgeKey other && Equals(other);
        public override int GetHashCode() { unchecked { return (a * 397) ^ b; } }
    }

    private readonly struct EdgeLaneKey : IEquatable<EdgeLaneKey>
    {
        private readonly EdgeKey edge;
        private readonly int direction;
        public EdgeLaneKey(EdgeKey edge, int direction) { this.edge = edge; this.direction = direction; }
        public bool Equals(EdgeLaneKey other) => edge.Equals(other.edge) && direction == other.direction;
        public override bool Equals(object obj) => obj is EdgeLaneKey other && Equals(other);
        public override int GetHashCode() { unchecked { return (edge.GetHashCode() * 397) ^ direction; } }
    }

    private readonly struct PlatformKey : IEquatable<PlatformKey>
    {
        private readonly int station, platform;
        public PlatformKey(int station, int platform) { this.station = station; this.platform = platform; }
        public bool Equals(PlatformKey other) => station == other.station && platform == other.platform;
        public override bool Equals(object obj) => obj is PlatformKey other && Equals(other);
        public override int GetHashCode() { unchecked { return (station * 397) ^ platform; } }
    }

    private readonly struct StationPair
    {
        public int Start { get; }
        public int Goal { get; }
        public StationPair(int start, int goal) { Start = start; Goal = goal; }
    }
}
