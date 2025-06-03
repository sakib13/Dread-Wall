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
        [SerializeField] float despawnDelay = 2.0f;

        [Header("UI")]
        [SerializeField] GameObject gameStartCanvas;
        [SerializeField] TextMeshProUGUI timerText;
        [SerializeField] GameObject winPanel;
        [SerializeField] GameObject losePanel;
        //[SerializeField] private Button startButton;

        [Header("Game Settings")]
        [SerializeField] float gameDuration = 300f; // Total length of game (seconds)

        [Networked] private TickTimer gameTimer { get; set; }
        [Networked] private int currentStage { get; set; } = 0;


        private PlayerRef[] players;

        // Store currently active squares and platforms
        private NetworkObject[] activeCubes = new NetworkObject[2];
        private NetworkObject[] activePlanes = new NetworkObject[2];

        private bool gameEnded = false;
        public bool IsMenuVisible { get; set; } = true;
        private bool isSinglePlayer = false;

        public WallMover wallMover; // Reference to the WallMover script-Sakib
        public DestroyObject destroyableWall; // Reference to destroy walls-Sakib


        public override void Spawned()
        {
            if (Object.HasStateAuthority)
            {
                players = Runner.ActivePlayers.ToArray();
                IsMenuVisible = true;
                isSinglePlayer = players.Length == 1;
            }

            else
            {
                players = Runner.ActivePlayers.ToArray();
                gameStartCanvas.SetActive(IsMenuVisible);
            }
            // Reset the UI state
            winPanel.SetActive(false);
            losePanel.SetActive(false);
            timerText.gameObject.SetActive(true);
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
                    currentStage = 3;
                    break;
                case 3: //
                    Debug.Log("---------All Puzzle Solved!------------");
                    EndGame(true); // Winning the game
                    break;
            }
        }

        // Destroy the current puzzle
        IEnumerator DespawnCurrentPuzzle()
        {
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

            // Get the position in front of the player's viewpoint
            if (Runner.TryGetPlayerObject(player, out var playerObj))
            {
                Vector3 spawnPos = playerObj.transform.position +
                                   playerObj.transform.forward * spawnDistance +
                                   Vector3.up * spawnHeight;

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
                    spawnPos - Vector3.up * spawnHeight + new Vector3(0, 1, 0),
                    Quaternion.identity,
                    player
                );
            }
            else // normal generation
            {
                activeCubes[playerIndex] = Runner.Spawn(
                    cubePrefab,
                    cubeSpawnPosition,
                    Quaternion.identity,
                    player
                );

                activePlanes[playerIndex] = Runner.Spawn(
                    planePrefab,
                    planeSpawnPosition,
                    Quaternion.identity,
                    player
                );
            }

            yield return null;
        }

        public override void FixedUpdateNetwork()
        {
            if (gameEnded || !Object.HasStateAuthority) return;

            // Update and check game timer
            if (gameTimer.Expired(Runner))
            {
                // Check if the game is finished
                if (currentStage < 3)
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
            timerText.text = $"{minutes:00}:{seconds:00}";

            // Turns red when the remaining time is less than 10 seconds.
            if (remainingTime <= 10f)
            {
                timerText.color = Color.red;
            }
        }

        // Game Over
        private void EndGame(bool isWin)
        {
            if (gameEnded) return;

            gameEnded = true;
            gameTimer = TickTimer.None;

            RPC_ShowGameResult(isWin);

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
            timerText.gameObject.SetActive(false);

            if (isWin)
            {
                winPanel.SetActive(true);
                Debug.Log("----------------YOU WIN!!!----------------------");
            }
            else
            {
                losePanel.SetActive(true);
                Debug.Log("----------------YOU LOSE!!!----------------------");
            }
        }

        [Rpc(RpcSources.All, RpcTargets.All)]
        public void RPC_HideMenu()
        {
            // Ensure that only the host actually modifies the network state
            if (Object.HasStateAuthority)
            {
                IsMenuVisible = false;
                gameStartCanvas.SetActive(IsMenuVisible);
            }
        }




        public void StartGame()
        {
            // Initialize the timer
            gameTimer = TickTimer.CreateFromSeconds(Runner, gameDuration);
            gameEnded = false;

            StartCoroutine(InitialSpawn());
            Debug.Log("----------------------GameStart------------------------");
            //gameStartCanvas.SetActive(false);

            // Reset timer color
            timerText.color = Color.white;

            if (Object.HasStateAuthority)
            {
                players = Runner.ActivePlayers.ToArray();
            }
        }
        //Sakib's code
        public void OnPuzzleComplete()
        {
            if (destroyableWall != null)
            {
                destroyableWall.DestroySelf();
            }
        }
        
    }
}