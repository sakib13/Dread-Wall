using UnityEngine;
using Fusion;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine.UI;

namespace Scripts
{
    public class GameManager : NetworkBehaviour
    {
        [Header("Cube Prefabs")]
        [SerializeField] NetworkObject redCube;
        [SerializeField] NetworkObject blueCube;
        [SerializeField] NetworkObject greenCube;

        [Header("Plane Prefabs")]
        [SerializeField] NetworkObject redPlane;
        [SerializeField] NetworkObject bluePlane;
        [SerializeField] NetworkObject greenPlane;

        [Header("Spawn Settings")]
        [SerializeField] float spawnDistance = 0.5f;
        [SerializeField] float spawnHeight = 1f;
        [SerializeField] Vector3 cubeSpawnPosition = new Vector3(0, 1, 1);
        [SerializeField] Vector3 planeSpawnPosition = new Vector3(-1, 0, 0);
        [SerializeField] float despawnDelay = 1.5f; // Destruction delay time

        [Header("3D Audio Settings")]
        [SerializeField] AudioClip cubeSpawnSound;
        [SerializeField] AudioClip puzzleSolvedSound;
        [SerializeField] GameObject soundEmitterPrefab; // Emitter prefabs for 3D sound effects

        [Header("Voice Audio Settings")]
        [SerializeField] AudioClip introAudio;       // Introductory voice at the start of the game
        [SerializeField] AudioClip urgencyPushAudio; // Urgent voice reminders when time is half over
        [SerializeField] AudioClip winAudio;         // Game Winning Voice
        [SerializeField] AudioClip failAudio;        // Game Failure Voice
        [SerializeField] float voiceVolume = 0.8f;   // Voice volume

        [Header("UI")]
        [SerializeField] GameObject gameStartCanvas;
        [SerializeField] TextMeshProUGUI[] timerTexts = new TextMeshProUGUI[4]; // Four timers
        [SerializeField] GameObject[] winPanels = new GameObject[4]; // Four victory panels
        [SerializeField] GameObject[] losePanels = new GameObject[4]; // Four Failure Panels
        [Networked] public bool IsMenuVisible { get; set; } = true; // Maintain network synchronisation properties

        [Header("Game Settings")]
        [SerializeField] float gameDuration = 300f; // Total length of game (seconds)

        [Networked] private TickTimer gameTimer { get; set; }
        [Networked] private int currentStage { get; set; } = 0;

        private ChangeDetector _changeDetector;// Change Detector for Fusion 2
        
        private PlayerRef[] players;

        // Store currently active squares and platforms
        private NetworkObject[] activeCubes = new NetworkObject[2];
        private NetworkObject[] activePlanes = new NetworkObject[2];

        private bool gameEnded = false;
        private bool isSinglePlayer = false;
        private bool hasPlayedUrgencyAudio = false; // Mark if an emergency voice has been played

        public WallMover wallMover; // Reference to the WallMover script-Sakib
        public DestroyObject destroyableWall; // Reference to destroy walls-Sakib


        public override void Spawned()
        {
            _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

            if (Object.HasStateAuthority)
            {
                players = Runner.ActivePlayers.ToArray();
                IsMenuVisible = true;   // Host sets initial state
                isSinglePlayer = players.Length == 1;
            }

            // Initial UI state for all clients
            UpdateMenuVisibility();

            // Reset all UI states
            SetAllUIElementsActive(false);
            SetAllTimersActive(false);

            // Ensure the availability of sound pre-fabricated parts
            if (soundEmitterPrefab == null)
            {
                Debug.LogWarning("---------Sound emitter prefab is not assigned in GameManager!-----------");
            }
        }

        // Set the activation status of all UI elements (victory/defeat panels)
        private void SetAllUIElementsActive(bool active)
        {
            foreach (var panel in winPanels)
            {
                if (panel != null) panel.SetActive(active);
            }
            foreach (var panel in losePanels)
            {
                if (panel != null) panel.SetActive(active);
            }
        }

        // Set the active state of all timers
        private void SetAllTimersActive(bool active)
        {
            foreach (var timer in timerTexts)
            {
                if (timer != null) timer.gameObject.SetActive(active);
            }
        }


        // Add render loop to handle change detection
        public override void Render()
        {
            base.Render();

            // Detect network state changes with ChangeDetector
            foreach (var change in _changeDetector.DetectChanges(this))
            {
                switch (change)
                {
                    case nameof(IsMenuVisible):
                        UpdateMenuVisibility();
                        break;
                        // You can add other properties that need to be detected
                }
            }
        }

        // Ways to update menu visibility
        private void UpdateMenuVisibility()
        {
            gameStartCanvas.SetActive(IsMenuVisible);
        }

        IEnumerator InitialSpawn()
        {
            // Initial wait 5 seconds
            yield return new WaitForSeconds(5f);

            // Phase 1: Generate red squares and red platforms for player 1
            yield return SpawnForPlayer(0, redCube, redPlane);
            currentStage = 1;
        }

        // RPC methods called by platform triggers
        [Rpc(sources: RpcSources.All, targets: RpcTargets.StateAuthority)]
        public void RPC_OnPuzzleSolved(int stage)
        {
            if (stage != currentStage || gameEnded) return;

            StartCoroutine(HandlePuzzleSolved());
        }

        IEnumerator HandlePuzzleSolved()
        {
            // Destroy the current stage's blocks and platforms
            yield return DespawnCurrentPuzzle();

            // Delay the generation of the next puzzle for a while
            yield return new WaitForSeconds(despawnDelay);

            switch (currentStage)
            {
                case 1: // Red puzzle complete
                    // Single-player mode: generate blue puzzles for the same player
                    // Multiplayer mode: generates blue puzzles for player 2
                    int nextPlayerIndex = isSinglePlayer ? 0 : 1;
                    yield return SpawnForPlayer(nextPlayerIndex, blueCube, bluePlane);
                    Debug.Log($"-----------for the player {(isSinglePlayer ? "1" : "2")} generate blue puzzle-----------");
                    currentStage = 2;
                    break;
                case 2: // Blue puzzle complete
                    // Generate green puzzles for player 1 (single/multiplayer both player 1)
                    yield return SpawnForPlayer(0, greenCube, greenPlane);
                    Debug.Log("--------Spawning GreenCubes for player1----------");

                    // Play the puzzle solving sound effect
                    PlaySound(puzzleSolvedSound, Vector3.zero);

                    currentStage = 3;
                    break;
                case 3: //
                    Debug.Log("---------All Puzzle Solved!------------");
                    currentStage = 4;

                    // Play the final puzzle solution sound
                    PlaySound(puzzleSolvedSound, Vector3.zero);

                    EndGame(true); // Winning the game
                    break;
            }
        }

        // Destroy the current puzzle
        IEnumerator DespawnCurrentPuzzle()
        {
            //Vector3 puzzlePosition = activePlanes[playerIndex].transform.position;

            int playerIndex = currentStage switch
            {
                1 => 0, // Red puzzles belong to player 0
                2 => isSinglePlayer ? 0 : 1, // Blue puzzles: single player mode player 0, multiplayer mode player 1
                _ => -1
            };

            if (playerIndex >= 0)
            {
                // Destroy the cube
                if (activeCubes[playerIndex] != null && activeCubes[playerIndex].IsValid)
                {
                    Runner.Despawn(activeCubes[playerIndex]);
                    activeCubes[playerIndex] = null;
                }

                // Destruction platform
                if (activePlanes[playerIndex] != null && activePlanes[playerIndex].IsValid)
                {
                    Runner.Despawn(activePlanes[playerIndex]);
                    activePlanes[playerIndex] = null;
                }

                // Wait one frame to ensure destruction is complete
                yield return null;
            }
        }


        IEnumerator SpawnForPlayer(int playerIndex, NetworkObject cubePrefab, NetworkObject planePrefab)
        {
            // Ensure that the player index is valid
            if (playerIndex >= players.Length)
            {
                Debug.LogError($"----------Invalid player index: {playerIndex}, total number of players: {players.Length}------------");
                yield break;
            }

            PlayerRef player = players[playerIndex];
            Vector3 spawnPos = Vector3.zero;
            Vector3 planePos = Vector3.zero;

            // Get the position in front of the player's viewpoint
            if (Runner.TryGetPlayerObject(player, out var playerObj))
            {
                spawnPos = playerObj.transform.position +
                                   playerObj.transform.forward * spawnDistance +
                                   Vector3.up * spawnHeight;

                planePos = spawnPos - Vector3.up * spawnHeight + new Vector3(0, 1, 0); 
            }
            else // normal generation
            {
                spawnPos = cubeSpawnPosition;
                planePos = planeSpawnPosition;
            }

            // Generate Cube
            activeCubes[playerIndex] = Runner.Spawn(
                cubePrefab,
                spawnPos,
                Quaternion.identity,
                player
            );

            // Spawn platforms (placed on the ground directly in front of the player)
            activePlanes[playerIndex] = Runner.Spawn(
                planePrefab,
                planePos,
                Quaternion.identity,
                player
            );

            // Play the cube spawning sound (at the spawning position)£©
            PlaySound(cubeSpawnSound, spawnPos);

            yield return null;
        }

        // Local method to play 3D sound (without RPC)
        private void PlaySound(AudioClip clip, Vector3 position)
        {
            if (clip == null || soundEmitterPrefab == null) return;

            // Create a sound emitter at the specified location
            GameObject soundEmitter = Instantiate(soundEmitterPrefab, position, Quaternion.identity);
            AudioSource audioSource = soundEmitter.GetComponent<AudioSource>();

            if (audioSource != null)
            {
                audioSource.clip = clip;
                audioSource.spatialBlend = 1.0f; // Set to 3D sound
                audioSource.Play();

                // Destroy the transmitter when the sound effect has finished playing
                Destroy(soundEmitter, clip.length + 0.1f);
            }
            else
            {
                Destroy(soundEmitter);
            }
        }

        // Play voice (2D global sound)
        private void PlayVoice(AudioClip clip)
        {
            if (clip == null) return;

            // Create a temporary game object to play the voice
            GameObject voicePlayer = new GameObject("VoicePlayer");
            AudioSource audioSource = voicePlayer.AddComponent<AudioSource>();

            audioSource.clip = clip;
            audioSource.volume = voiceVolume;
            audioSource.spatialBlend = 0f; // Set to 2D sound (global)
            audioSource.Play();

            // Destroy the object when voice playback is complete
            Destroy(voicePlayer, clip.length + 0.1f);
        }

        public override void FixedUpdateNetwork()
        {
            if (gameEnded || !Object.HasStateAuthority) return;

            // Update and check game timer
            if (gameTimer.Expired(Runner))
            {
                // Check if the game is finished
                if (currentStage <= 3)
                {
                    EndGame(false); // Game Failure
                }
                else
                {
                    EndGame(true); // Winning the game
                }
                gameTimer = TickTimer.None;
            }
            else if (gameTimer.IsRunning)
            {
                // Update the UI of all clients
                UpdateTimerUI();
            }
        }

        // Update the timer UI
        private void UpdateTimerUI()
        {
            float remainingTime = gameTimer.RemainingTime(Runner) ?? 0;
            int minutes = Mathf.FloorToInt(remainingTime / 60);
            int seconds = Mathf.FloorToInt(remainingTime % 60);
            string timeText = $"{minutes:00}:{seconds:00}";

            // Update all timers
            foreach (var timer in timerTexts)
            {
                if (timer != null)
                {
                    timer.text = timeText;

                    // Turns red when the remaining time is less than 10 seconds.
                    if (remainingTime <= 10f)
                    {
                        timer.color = Color.red;
                    }
                    else
                    {
                        timer.color = Color.white;
                    }
                }
            }

            // Check if the emergency voice needs to be played (half way through the time)£©
            if (!hasPlayedUrgencyAudio && remainingTime <= gameDuration / 2f)
            {
                PlayVoice(urgencyPushAudio);
                hasPlayedUrgencyAudio = true;
            }
        }

        // Game Over
        private void EndGame(bool isWin)
        {
            if (gameEnded) return;

            
            gameEnded = true;
            gameTimer = TickTimer.None;

            RPC_ShowGameResult(isWin);

            // Win performance and fail performance
            if (isWin)
            {
                PlayVoice(winAudio);
                destroyableWall.DestroySelf();  // Destroy the final door when all puzzles solved in time
                wallMover.moving = false;
            }
            else
            {
                PlayVoice(failAudio);
            }

            // Stop all puzzle generation
            StopAllCoroutines();

            // Destroy all existing puzzles
            for (int i = 0; i < activeCubes.Length; i++)
            {
                if (activeCubes[i] != null && activeCubes[i].IsValid)
                {
                    Runner.Despawn(activeCubes[i]);
                }
                if (activePlanes[i] != null && activePlanes[i].IsValid)
                {
                    Runner.Despawn(activePlanes[i]);
                }
            }
        }

        [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
        private void RPC_ShowGameResult(bool isWin)
        {
            // Hide all timers
            SetAllTimersActive(false);

            // Show all win or lose panels
            SetAllUIElementsActive(false);

            if (isWin)
            {
                foreach (var panel in winPanels)
                {
                    if (panel != null) panel.SetActive(true);
                }
                Debug.Log("----------------YOU WIN!!!----------------------");
            }
            else
            {
                foreach (var panel in losePanels)
                {
                    if (panel != null) panel.SetActive(true);
                }
                Debug.Log("----------------YOU LOSE!!!----------------------");
            }
        }

        [Rpc(RpcSources.All, RpcTargets.All)]
        public void RPC_HideMenu()
        {
            // All clients hide the menu locally immediately
            if (gameStartCanvas != null) gameStartCanvas.SetActive(false);

            // Ensure that only the host actually modifies the network state
            if (Object.HasStateAuthority)
            {
                IsMenuVisible = false;
            }

        }


        public void StartGame()
        {
            // Local hidden menus for all clients
            if (gameStartCanvas != null) gameStartCanvas.SetActive(false);

            // Host sets network state
            if (Object.HasStateAuthority)
            {
                IsMenuVisible = false;
            }

            // Initialize the timer
            gameTimer = TickTimer.CreateFromSeconds(Runner, gameDuration);
            gameEnded = false;

            // Activate all timers
            SetAllTimersActive(true);

            // Reset all timer colors
            foreach (var timer in timerTexts)
            {
                if (timer != null) timer.color = Color.white;
            }

            // Play the game's introductory voice
            PlayVoice(introAudio);

            StartCoroutine(InitialSpawn());
            Debug.Log("----------------------GameStart------------------------");
            //gameStartCanvas.SetActive(false);


            // Start wall moving
            if (wallMover != null) wallMover.BeginMove();

            if (Object.HasStateAuthority)
            {
                players = Runner.ActivePlayers.ToArray();
            }
        }
        //Sakib's code
        //public void OnPuzzleComplete()
        //{
        //    if (destroyableWall != null)
        //    {
        //        destroyableWall.DestroySelf();
        //    }
        //}
        
    }
}