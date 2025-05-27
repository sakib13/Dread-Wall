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

        private int currentStage = 0;
        private PlayerRef[] players;

        public override void Spawned()
        {
            if (Object.HasStateAuthority)
            {
                players = Runner.ActivePlayers.ToArray();
                StartCoroutine(InitialSpawn());
            }
        }

        IEnumerator InitialSpawn()
        {
            // ��ʼ�ȴ�5��
            yield return new WaitForSeconds(5f);

            // ��һ�׶Σ�Ϊ���1���ɺ췽��ͺ�ƽ̨
            SpawnForPlayer(players[0], redCube, redPlane);
            currentStage = 1;
        }

        // ��ƽ̨���������õ�RPC����
        [Rpc(sources: RpcSources.All, targets: RpcTargets.StateAuthority)]
        public void RPC_OnPuzzleSolved(int stage)
        {
            if (stage != currentStage) return;

            switch (currentStage)
            {
                case 1: // ���������
                    SpawnForPlayer(players[1], blueCube, bluePlane);
                    Debug.Log("Spawning Cubes");
                    currentStage = 2;
                    break;
                case 2: // ���������
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
            // ��ȡ����ӽ�ǰ��λ��
            if (Runner.TryGetPlayerObject(player, out var playerObj))
            {
                Vector3 spawnPos = playerObj.transform.position +
                                   playerObj.transform.forward * spawnDistance +
                                   Vector3.up * spawnHeight;

                // ����������
                NetworkObject cube = Runner.Spawn(
                    cubePrefab,
                    spawnPos,
                    Quaternion.identity,
                    player
                );

                // ����ƽ̨��ֱ�ӷ��������ǰ���棩
                Runner.Spawn(
                    planePrefab,
                    spawnPos - Vector3.up * spawnHeight,
                    Quaternion.identity,
                    player
                );
            }
            else
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
            Debug.Log("MQTT: ");
        }
    }
}