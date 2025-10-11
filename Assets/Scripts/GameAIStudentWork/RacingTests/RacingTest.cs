using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

using GameAI;

namespace Tests
{

    public class RacingTest
    {

        const int timeScale = 1; // how fast to run the game relative to frame rate. Running fast doesn't necessarily
                                 // give accurate results.

        const int PlayMatchTimeOutMS = int.MaxValue; // don't mess with this; add it to new tests
                                                     // as [Timeout(PlayMatchTimeOutMS)] (see below for
                                                     // example) It stops early default timeout

        public RacingTest()
        {

        }



        [Timeout(PlayMatchTimeOutMS)]
        private IEnumerator RunRaceFromConfig(string configName)
        {
            var config = Resources.Load<RaceTestConfig>($"RaceTrackTestConfigs/{configName}");
            Assert.IsNotNull(config, $"Missing grading config {configName}");

            return _TestFuzzyRace(
                config.trackPrefabName,
                config.duration_s,
                config.minAllowedSpeed,
                config.targetSpeed,
                config.extraCreditSpeed,
                config.maxAllowedWipeouts,
                config.maxPartialPenaltyWipeouts,
                config.speedScoreWeight,
                config.wipeoutScoreWeight,
                config.extraCreditWeight,
                config.gradeWeight
            );
        }


        [UnityTest]
        [Timeout(PlayMatchTimeOutMS)]
        public IEnumerator Race_Curvy_5m() => RunRaceFromConfig("Race_Curvy_5m");
 
        [UnityTest]
        [Timeout(PlayMatchTimeOutMS)]
        public IEnumerator Race_Winding_5m() => RunRaceFromConfig("Race_Winding_5m");


        [UnityTest]
        [Timeout(PlayMatchTimeOutMS)]
        public IEnumerator Race_FastSweepers_5m() => RunRaceFromConfig("Race_FastSweepers_5m");


        [UnityTest]
        [Timeout(PlayMatchTimeOutMS)]
        public IEnumerator Race_DragRace_1m() => RunRaceFromConfig("Race_Drag_1m");
 


        private IEnumerator LoadHarnessAndSpawnPrefab(string prefabName)
        {
            const string HarnessName = "HarnessRuntime";

            Scene harness;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var s = SceneManager.GetSceneAt(i);
                if (s.name == HarnessName)
                {
                    var op = SceneManager.UnloadSceneAsync(s);
                    if (op != null)
                        while (!op.isDone) yield return null;
                    break;
                }
            }

            harness = SceneManager.CreateScene(HarnessName);
            SceneManager.SetActiveScene(harness);

            GameObject prefab = Resources.Load<GameObject>($"Prefab/{prefabName}");

            Assert.IsNotNull(prefab, $"Could not find prefab '{prefabName}' in Resources/Prefab/.");

            Debug.Log($"[RacingTest] Spawning prefab '{prefab.name}' into scene '{harness.name}'.");
            var instance = Object.Instantiate(prefab);
            if (instance.scene != harness)
                SceneManager.MoveGameObjectToScene(instance, harness);

            yield return null;

            Assert.AreEqual(HarnessName, SceneManager.GetActiveScene().name, "Active scene should be the harness.");
            Assert.AreEqual(harness, instance.scene, "Prefab instance should be in the harness scene.");
        }

        [Timeout(PlayMatchTimeOutMS)]
        public IEnumerator _TestFuzzyRace(string raceTrackPrefabName, float duration_s,
            float minAllowedSpeed, float targetSpeed, float extraCreditSpeed,
            int maxAllowedWipeouts, int maxPartialPenaltyWipeouts,
            float speedScoreWeight, float wipeoutScoreWeight, float extraCreditWeight, float gradeWeight)
        {
            Time.timeScale = timeScale;

            GameManager.INTERNAL_overrideSimulationMode = GameManager.SimulationMode.FPS_60_1X_SimTime;

            yield return LoadHarnessAndSpawnPrefab(raceTrackPrefabName);

            yield return new WaitForSeconds(duration_s);

            var gm = GameManager.Instance;

            Debug.Log($"Km/H LTA: {gm.KpHLTA} Num wipeouts: {gm.Wipeouts} meters: {gm.MetersTravelled}");


            var speedPenalty = Mathf.Lerp(speedScoreWeight, 0f,

                Power(
                    Mathf.InverseLerp(minAllowedSpeed, targetSpeed, gm.KpHLTA)
                    , 0.5f)

                    );


            var wipeoutPenalty = Mathf.Lerp(wipeoutScoreWeight, 0f,
                    1f - Mathf.InverseLerp(maxAllowedWipeouts, maxPartialPenaltyWipeouts, gm.Wipeouts)
                );


            float extraCredit = 0f;

            if (gm.Wipeouts <= 0)
            {
                extraCredit = Mathf.Lerp(0f, extraCreditWeight,

                     Power(
                         Mathf.InverseLerp(targetSpeed, extraCreditSpeed, gm.KpHLTA)
                         , 0.5f)

                         );

                Debug.Log($"Extra credit earned: {extraCredit}");
            }
            else
            {
                Debug.Log($"Extra credit only earned if no wipeouts!");
            }

            var totalScore = (speedScoreWeight - speedPenalty) +
                (wipeoutScoreWeight - wipeoutPenalty) +
                extraCredit;

            Debug.Log($"Estimated Total Score: {totalScore * 100}%");
            Debug.Log($"Estimated Weighted Grade Contribution (wt: {gradeWeight}): {gradeWeight * totalScore * 100}");

        }

        float Power(float t, float strength)
        {
            return Mathf.Pow(t, strength);
        }


    }

}
