using UnityEngine;
using Fusion;
using System.Collections;
using System.Linq;

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
        [SerializeField] Vector3 cubeSpawnPosition =  new Vector3(0, 1, 1);
        [SerializeField] Vector3 planeSpawnPosition = new Vector3(-1, 0, 0);

        [Header("UI")]
        [SerializeField] GameObject canvas;


        private int currentStage = 0;
        private PlayerRef[] players;

        public override void Spawned()
        {
            if (Object.HasStateAuthority)
            {
                players = Runner.ActivePlayers.ToArray();
                //StartCoroutine(InitialSpawn());
            }
        }

        IEnumerator InitialSpawn()
        {
            // Initial wait 5 seconds
            yield return new WaitForSeconds(5f);

            // Phase 1: Generate red squares and red platforms for player 1
            SpawnForPlayer(players[0], redCube, redPlane);
            currentStage = 1;
        }

        // RPC methods called by platform triggers
        [Rpc(sources: RpcSources.All, targets: RpcTargets.StateAuthority)]
        public void RPC_OnPuzzleSolved(int stage)
        {
            if (stage != currentStage) return;

            switch (currentStage)
            {
                case 1: // Red puzzle complete
                    SpawnForPlayer(players[1], blueCube, bluePlane);
                    Debug.Log("Spawning Cubes");
                    currentStage = 2;
                    break;
                case 2: // Blue puzzle complete
                    SpawnForPlayer(players[0], greenCube, greenPlane);
                    currentStage = 3;
                    break;
                case 3: //
                    Debug.Log("Green Puzzle Solved!");
                    break;
            }
        }

        void SpawnForPlayer(PlayerRef player, NetworkObject cubePrefab, NetworkObject planePrefab)
        {
            // Get the position in front of the player's viewpoint
            if (Runner.TryGetPlayerObject(player, out var playerObj))
            {
                Vector3 spawnPos = playerObj.transform.position +
                                   playerObj.transform.forward * spawnDistance +
                                   Vector3.up * spawnHeight;

                // Generate Cube
                NetworkObject cube = Runner.Spawn(
                    cubePrefab,
                    spawnPos,
                    Quaternion.identity,
                    player
                );

                // Spawn platforms (placed on the ground directly in front of the player)
                Runner.Spawn(
                    planePrefab,
                    spawnPos - Vector3.up * spawnHeight,
                    Quaternion.identity,
                    player
                );
            }
            else // Spawn it normally
            {
                NetworkObject cube = Runner.Spawn(
                cubePrefab,
                cubeSpawnPosition,
                Quaternion.identity,
                Object.InputAuthority
            );

                Runner.Spawn(
                planePrefab,
                planeSpawnPosition,
                Quaternion.identity,
                Object.InputAuthority
            );
            }
        }

        public void StartUI()
        {
            StartCoroutine(InitialSpawn());
            Debug.Log("----------------------GameStart------------------------");
            canvas.SetActive(false);
        }
    }
}